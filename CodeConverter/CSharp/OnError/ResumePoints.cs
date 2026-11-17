using static Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

internal enum ResumePointKind
{
    /// <summary>Nothing to guard: the statement can't fail, isn't covered by a handler, or is inside a statement that is one resume point.</summary>
    None,
    /// <summary>A statement whose failure resumes after it.</summary>
    Statement,
    /// <summary>A <c>Dim</c>: each declarator is a resume point of its own.</summary>
    Declaration,
    /// <summary><c>If</c> or <c>ElseIf</c> conditions: a failure resumes in that branch.</summary>
    If,
    /// <summary><c>While</c> or <c>Do While/Until</c>: a failing condition enters the body.</summary>
    TopTestedLoop,
    /// <summary><c>Do</c> ... <c>Loop While/Until</c>: a failing condition leaves the loop.</summary>
    BottomTestedLoop,
    /// <summary>A statement with a header that can fail and statements inside that are resume points of their own.</summary>
    Block
}

/// <summary>
/// Which statements are resume points, from VB's lowering (the <c>RegisterUnstructuredExceptionHandlingResumeTarget</c> calls in the <c>LocalRewriter_*.vb</c> files)
/// and the table of failing expressions in README.md. Produces no C# syntax.
/// </summary>
/// <remarks>
/// <c>Using</c> and <c>SyncLock</c> are lowered to a compiler-generated <c>Try</c>, so the whole statement is the one resume point and nothing inside is.
/// </remarks>
internal sealed class ResumePoints
{
    private readonly OnErrorModel _model;
    private readonly SemanticModel _semanticModel;
    private readonly Func<int, bool> _isCoveredStatement;

    /// <param name="isCoveredStatement">Whether an error in the method-level statement at that index is handled by something that resumes.</param>
    public ResumePoints(OnErrorModel model, SemanticModel semanticModel, Func<int, bool> isCoveredStatement)
    {
        _model = model;
        _semanticModel = semanticModel;
        _isCoveredStatement = isCoveredStatement;
    }

    public bool IsCovered(VBSyntax.StatementSyntax statement) =>
        _isCoveredStatement(_model.MethodLevelIndexOf(statement)) &&
        !statement.Ancestors().Any(a => a is VBSyntax.UsingBlockSyntax or VBSyntax.SyncLockBlockSyntax);

    /// <summary>The locals declared by the declarators of a <c>Dim</c> that are resume points, so are split into a declaration and a guarded assignment.</summary>
    public IEnumerable<ILocalSymbol> GuardedLocals(VBSyntax.LocalDeclarationStatementSyntax declaration) =>
        !IsCovered(declaration)
            ? Enumerable.Empty<ILocalSymbol>()
            : declaration.Declarators.Where(d => ThrowAnalysis.CanThrow(d, _semanticModel))
                .SelectMany(d => d.Names).Select(n => _semanticModel.GetDeclaredSymbol(n) as ILocalSymbol).Where(l => l != null);

    /// <summary>Whether every local that would be split in these statements (and the statements inside them) can be (see <see cref="DeclarationSplitter.CanSplit"/>).</summary>
    public bool CanGuardAllDeclarations(IEnumerable<VBSyntax.StatementSyntax> statements) =>
        statements.SelectMany(s => s.DescendantNodesAndSelf(n => n is not VBSyntax.LambdaExpressionSyntax)).OfType<VBSyntax.LocalDeclarationStatementSyntax>()
            .SelectMany(GuardedLocals).All(DeclarationSplitter.CanSplit);

    public ResumePointKind KindOf(VBSyntax.StatementSyntax statement)
    {
        if (statement is VBSyntax.LocalDeclarationStatementSyntax) return ResumePointKind.Declaration;
        if (OnErrorModel.IsOnErrorStatement(statement) || statement is VBSyntax.ResumeStatementSyntax || !IsCovered(statement)) return ResumePointKind.None;
        return statement switch {
            VBSyntax.MultiLineIfBlockSyntax multiLine => AnyCanThrow(Conditions(multiLine)) ? ResumePointKind.If : ResumePointKind.None,
            VBSyntax.SingleLineIfStatementSyntax singleLine => CanThrow(singleLine.Condition) ? ResumePointKind.If : ResumePointKind.None,
            VBSyntax.WhileBlockSyntax whileBlock => CanThrow(whileBlock.WhileStatement.Condition) ? ResumePointKind.TopTestedLoop : ResumePointKind.None,
            VBSyntax.DoLoopBlockSyntax doLoop => KindOfDoLoop(doLoop),
            VBSyntax.ForBlockSyntax forBlock => ThrowAnalysis.CanThrow(forBlock.ForStatement, _semanticModel) ? ResumePointKind.Block : ResumePointKind.None,
            VBSyntax.ForEachBlockSyntax or VBSyntax.UsingBlockSyntax or VBSyntax.SyncLockBlockSyntax => ResumePointKind.Block,
            VBSyntax.SelectBlockSyntax select => CanSelectThrow(select) ? ResumePointKind.Block : ResumePointKind.None,
            VBSyntax.WithBlockSyntax with => CanThrow(with.WithStatement.Expression) ? ResumePointKind.Block : ResumePointKind.None,
            _ => ThrowAnalysis.CanThrow(statement, _semanticModel) ? ResumePointKind.Statement : ResumePointKind.None
        };
    }

    /// <summary>The conditions of an <c>If</c> and its <c>ElseIf</c>s in order, which is the order of the conditions in the converted <c>if</c>/<c>else if</c> chain.</summary>
    public static IEnumerable<VBSyntax.ExpressionSyntax> Conditions(VBSyntax.MultiLineIfBlockSyntax block) =>
        block.IfStatement.Condition.Yield().Concat(block.ElseIfBlocks.Select(e => e.ElseIfStatement.Condition));

    public bool CanThrow(VBSyntax.ExpressionSyntax expression) => ThrowAnalysis.CanThrow(expression, _semanticModel);

    /// <summary>The condition of a loop, which is null for a bare <c>Do</c> ... <c>Loop</c>.</summary>
    public static VBSyntax.ExpressionSyntax LoopCondition(VBSyntax.DoLoopBlockSyntax doLoop) =>
        (doLoop.DoStatement.WhileOrUntilClause ?? doLoop.LoopStatement.WhileOrUntilClause)?.Condition;

    /// <summary>
    /// Whether a loop condition would be captured by a local function from a <c>ref</c>/<c>out</c> parameter, which C# doesn't allow.
    /// </summary>
    public bool HasLoopConditionCapturingARefParameter(VBSyntax.StatementSyntax statement)
    {
        var condition = statement switch {
            VBSyntax.WhileBlockSyntax whileBlock => whileBlock.WhileStatement.Condition,
            VBSyntax.DoLoopBlockSyntax doLoop => LoopCondition(doLoop),
            _ => null
        };
        return condition != null && condition.DescendantNodesAndSelf().OfType<VBSyntax.IdentifierNameSyntax>()
            .Any(i => _semanticModel.GetSymbolInfo(i).Symbol is IParameterSymbol { RefKind: not RefKind.None });
    }

    private ResumePointKind KindOfDoLoop(VBSyntax.DoLoopBlockSyntax doLoop)
    {
        var condition = LoopCondition(doLoop);
        if (condition == null || !CanThrow(condition)) return ResumePointKind.None;
        return doLoop.DoStatement.WhileOrUntilClause != null ? ResumePointKind.TopTestedLoop : ResumePointKind.BottomTestedLoop;
    }

    private bool AnyCanThrow(IEnumerable<VBSyntax.ExpressionSyntax> expressions) => expressions.Any(CanThrow);

    /// <summary>Only constant case expressions are known not to fail; others become <c>when</c> clauses or an <c>if</c> chain.</summary>
    private bool CanSelectThrow(VBSyntax.SelectBlockSyntax select) =>
        CanThrow(select.SelectStatement.Expression) || select.CaseBlocks.SelectMany(c => c.CaseStatement.Cases).SelectMany(CaseExpressions)
            .Any(e => !_semanticModel.GetConstantValue(e).HasValue);

    private static IEnumerable<VBSyntax.ExpressionSyntax> CaseExpressions(VBSyntax.CaseClauseSyntax clause) => clause switch {
        VBSyntax.SimpleCaseClauseSyntax simple => simple.Value.Yield(),
        VBSyntax.RangeCaseClauseSyntax range => new[] { range.LowerBound, range.UpperBound },
        VBSyntax.RelationalCaseClauseSyntax relational => relational.Value.Yield(),
        _ => Enumerable.Empty<VBSyntax.ExpressionSyntax>()
    };
}
