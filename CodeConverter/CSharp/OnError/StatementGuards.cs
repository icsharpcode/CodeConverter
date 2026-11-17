using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// Wraps what the converter produced for a statement in the <c>try</c>/<c>catch</c> that VB's per-statement resume point is, as README.md "Resume next" describes.
/// </summary>
/// <remarks>
/// Catching at the statement that fails means nothing has to jump back into a block, so loops stay loops and the method keeps its structure.
/// Statements inside a guarded block are guarded by their own visit, so a failing header skips the block rather than resuming inside it (an agreed deviation).
/// </remarks>
internal sealed class StatementGuards
{
    private readonly ResumePoints _points;
    private readonly GuardPolicy _policy;
    private readonly ErrStatements _err;
    private readonly DeclarationSplitter _splitter;
    private readonly SemanticModel _semanticModel;
    private readonly Func<VBasic.VisualBasicSyntaxNode, string, string> _uniqueName;
    private readonly string _exceptionName;
    private readonly Func<ILocalSymbol, bool> _isDeclaredAtTop;
    private readonly Func<string, TypeSyntax, bool> _hoistOutOfLoop;

    /// <param name="hoistOutOfLoop">Declares a local before the loop that the statement being guarded is in, returning false if it isn't in one.</param>
    /// <param name="isDeclaredAtTop">Locals whose declaration must come before the guards, because something declared earlier (a handler function) uses them.</param>
    public StatementGuards(ResumePoints points, GuardPolicy policy, ErrStatements err, DeclarationSplitter splitter, SemanticModel semanticModel,
        Func<VBasic.VisualBasicSyntaxNode, string, string> uniqueName, string exceptionName,
        Func<string, TypeSyntax, bool> hoistOutOfLoop, Func<ILocalSymbol, bool> isDeclaredAtTop = null)
    {
        _hoistOutOfLoop = hoistOutOfLoop;
        _isDeclaredAtTop = isDeclaredAtTop ?? (_ => false);
        _points = points;
        _policy = policy;
        _err = err;
        _splitter = splitter;
        _semanticModel = semanticModel;
        _uniqueName = uniqueName;
        _exceptionName = exceptionName;
    }

    /// <summary>The declarations to place before the statements that were guarded, for the locals selected by <c>isDeclaredAtTop</c>.</summary>
    public List<StatementSyntax> TopDeclarations { get; } = new();

    /// <summary>Whether a <c>return</c> was put in a guard. It then doesn't end the method, so a function may need a <c>return</c> after the guard.</summary>
    public bool GuardedAReturn { get; private set; }

    public SyntaxList<StatementSyntax> Guard(VBSyntax.StatementSyntax statement, SyntaxList<StatementSyntax> converted)
    {
        if (!converted.Any()) return converted;
        return _points.KindOf(statement) switch {
            ResumePointKind.Statement or ResumePointKind.Block => SyntaxFactory.SingletonList(Try(statement, converted)),
            ResumePointKind.Declaration => GuardDeclaration((VBSyntax.LocalDeclarationStatementSyntax)statement, converted),
            ResumePointKind.If => GuardConditions(statement, converted),
            ResumePointKind.TopTestedLoop => GuardLoopCondition(statement, converted, valueWhenFailed: true),
            ResumePointKind.BottomTestedLoop => GuardLoopCondition(statement, converted, valueWhenFailed: false),
            _ => converted
        };
    }

    private StatementSyntax Try(VBasic.VisualBasicSyntaxNode node, IEnumerable<StatementSyntax> body, IEnumerable<StatementSyntax> afterHandler = null)
    {
        var bodyStatements = body.ToList();
        GuardedAReturn |= bodyStatements.Any(ContainsReturn);
        var retryLabel = _policy.CanRetry ? _uniqueName(node, "retry") : null;
        var catchStatements = _policy.OnError(retryLabel).Concat(afterHandler ?? Enumerable.Empty<StatementSyntax>());
        var tryStatement = SyntaxFactory.TryStatement(SyntaxFactory.Block(bodyStatements),
            SyntaxFactory.SingletonList(_err.CatchClause(_exceptionName, catchStatements, _policy.Filter)), null);
        return retryLabel == null ? tryStatement : SyntaxFactory.LabeledStatement(retryLabel, tryStatement);
    }

    private static bool ContainsReturn(StatementSyntax statement) =>
        statement.DescendantNodesAndSelf(n => n is not (LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax)).OfType<ReturnStatementSyntax>().Any();

    /// <summary>
    /// Each declarator is a resume point, and a local declared in a guard would be out of scope after it, so a guarded local is declared before as <c>= default</c>.
    /// Every local gets an initializer: an error can skip the assignment VB's flow analysis relied on.
    /// In a loop the declaration goes before the loop: VB locals keep their value across iterations, so when the initializer fails the previous iteration's value is still there.
    /// </summary>
    private SyntaxList<StatementSyntax> GuardDeclaration(VBSyntax.LocalDeclarationStatementSyntax declaration, SyntaxList<StatementSyntax> converted)
    {
        var guardedLocals = new HashSet<ILocalSymbol>(_points.GuardedLocals(declaration), SymbolEqualityComparer.Default);
        var topLocals = declaration.Declarators.SelectMany(d => d.Names).Select(n => _semanticModel.GetDeclaredSymbol(n) as ILocalSymbol)
            .Where(l => l != null && _isDeclaredAtTop(l)).ToArray();
        var declarations = new List<StatementSyntax>();
        var inPlace = _splitter.Split(declaration, converted, l => guardedLocals.Contains(l) || topLocals.Contains(l, SymbolEqualityComparer.Default), declarations,
            (local, assignment) => guardedLocals.Contains(local) ? Try(declaration, new[] { assignment }) : assignment);
        var topNames = new HashSet<string>(topLocals.Select(l => l.Name), StringComparer.OrdinalIgnoreCase);
        var isAtTop = declarations.Select(d => d is LocalDeclarationStatementSyntax l && topNames.Contains(l.Declaration.Variables[0].Identifier.ValueText)).ToArray();
        TopDeclarations.AddRange(declarations.Where((_, i) => isAtTop[i]));
        return SyntaxFactory.List(declarations.Where((d, i) => !isAtTop[i] && !HoistedOutOfLoop(d)).Concat(DefaultInitializers.Add(inPlace)));
    }

    private bool HoistedOutOfLoop(StatementSyntax declaration) =>
        declaration is LocalDeclarationStatementSyntax { IsConst: false } local && local.Declaration.Variables.Count == 1 &&
        _hoistOutOfLoop(local.Declaration.Variables[0].Identifier.ValueText, local.Declaration.Type);

    /// <summary>
    /// A failing condition resumes in the branch: it's evaluated into a <c>bool</c> that is true if evaluating it fails.
    /// Each <c>ElseIf</c> condition needs somewhere to go, so the chain becomes nested <c>if</c>/<c>else</c> where one has a guard.
    /// </summary>
    private SyntaxList<StatementSyntax> GuardConditions(VBSyntax.StatementSyntax statement, SyntaxList<StatementSyntax> converted)
    {
        var vbConditions = statement is VBSyntax.MultiLineIfBlockSyntax multiLine
            ? ResumePoints.Conditions(multiLine).ToArray()
            : new[] { ((VBSyntax.SingleLineIfStatementSyntax)statement).Condition };
        if (converted.Count != 1 || converted[0] is not IfStatementSyntax csIf) {
            throw new InvalidOperationException("An If statement is expected to convert to a single if statement");
        }
        return SyntaxFactory.List(GuardConditionChain(csIf, vbConditions, 0));
    }

    private IEnumerable<StatementSyntax> GuardConditionChain(IfStatementSyntax csIf, IReadOnlyList<VBSyntax.ExpressionSyntax> vbConditions, int index)
    {
        var vbCondition = vbConditions[index];
        var name = _points.CanThrow(vbCondition) ? _uniqueName(vbCondition, "condition") : null;
        if (csIf.Else?.Statement is IfStatementSyntax elseIf && index + 1 < vbConditions.Count) {
            var rewritten = GuardConditionChain(elseIf, vbConditions, index + 1).ToList();
            csIf = csIf.WithElse(csIf.Else.WithStatement(rewritten.Count == 1 ? rewritten[0] : SyntaxFactory.Block(rewritten)));
        }
        if (name == null) return new[] { csIf };

        var declaration = SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)),
            SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(name),
                null, SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression))))));
        var evaluation = SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName(name), csIf.Condition));
        return new StatementSyntax[] { declaration, Try(vbCondition, new[] { evaluation }), csIf.WithCondition(SyntaxFactory.IdentifierName(name)) };
    }

    /// <summary>
    /// A loop must stay a loop, so the condition moves into a local function that a failure leaves with the value VB resumes with:
    /// a condition tested at the top resumes in the body, one tested at the bottom resumes after the loop.
    /// </summary>
    private SyntaxList<StatementSyntax> GuardLoopCondition(VBSyntax.StatementSyntax statement, SyntaxList<StatementSyntax> converted, bool valueWhenFailed)
    {
        var vbCondition = statement is VBSyntax.WhileBlockSyntax whileBlock ? whileBlock.WhileStatement.Condition : ResumePoints.LoopCondition((VBSyntax.DoLoopBlockSyntax)statement);
        var functionName = _uniqueName(vbCondition, "LoopCondition");
        var call = SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(functionName));
        var (condition, loop) = converted.Last() switch {
            WhileStatementSyntax csWhile => (csWhile.Condition, (StatementSyntax)csWhile.WithCondition(call)),
            DoStatementSyntax csDo => (csDo.Condition, csDo.WithCondition(call)),
            _ => throw new InvalidOperationException("A loop is expected to convert to a while or do statement")
        };
        var body = Try(vbCondition, new[] { SyntaxFactory.ReturnStatement(condition) },
            new[] { SyntaxFactory.ReturnStatement(SyntaxFactory.LiteralExpression(valueWhenFailed ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression)) });
        var function = SyntaxFactory.LocalFunctionStatement(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)), functionName)
            .WithBody(SyntaxFactory.Block(body));
        return SyntaxFactory.List(converted.Take(converted.Count - 1).Append(function).Append(loop));
    }
}
