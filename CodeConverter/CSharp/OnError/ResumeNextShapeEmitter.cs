using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VBasicSyntaxKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// Emits README.md shapes 2 and 4: the method keeps its structure, and each statement that can fail is guarded so the next statement runs after a failure.
/// </summary>
/// <remarks>
/// Where the method knows statically that <c>On Error Resume Next</c> is active the guards catch everything.
/// Otherwise (<see cref="OnErrorModel.IsDynamic"/>) a <c>bool</c> local set where the <c>On Error</c> statements were filters the guards,
/// as VB's catch filters on its active handler local.
/// </remarks>
internal sealed class ResumeNextShapeEmitter
{
    private readonly OnErrorConversionContext _context;
    private readonly OnErrorModel _model;
    private readonly StatementGuards _guards;
    private readonly string _ignoringErrorsFlag;
    private readonly ErrStatements _err;

    public ResumeNextShapeEmitter(OnErrorConversionContext context, OnErrorModel model, ErrStatements err)
    {
        _context = context;
        _model = model;
        _err = err;
        var firstStatement = model.Statements.First();
        _ignoringErrorsFlag = model.IsDynamic ? context.Visitor.GetUniqueVariableNameInScope(firstStatement, "resumeNext") : null;
        GuardPolicy policy = model.IsDynamic ? new DynamicGuardPolicy(_ignoringErrorsFlag) : new ResumeNextGuardPolicy();
        var exceptionName = err.IsMaintained ? context.Visitor.GetUniqueVariableNameInScope(firstStatement, "ex") : null;
        _guards = new StatementGuards(CreateResumePoints(model, context.SemanticModel), policy, err, new DeclarationSplitter(context.SemanticModel, context.CommonConversions),
            context.SemanticModel, context.Visitor.GetUniqueVariableNameInScope, exceptionName, context.Visitor.TryHoistOutOfLoop);
    }

    /// <summary>
    /// Only when the model has nothing the guards can't express: handlers that are labels, <c>Resume</c> statements, and loop conditions that a local function can't capture.
    /// </summary>
    public static bool CanEmit(OnErrorModel model, SemanticModel semanticModel)
    {
        if (model.Handlers.Any() || model.ResumeStatements.Any() ||
            !model.OnErrorStatements.All(s => s is VBSyntax.OnErrorResumeNextStatementSyntax || s.IsKind(VBasicSyntaxKind.OnErrorGoToZeroStatement))) {
            return false;
        }
        var points = CreateResumePoints(model, semanticModel);
        return !model.Statements.SelectMany(s => s.DescendantNodesAndSelf(n => n is not VBSyntax.LambdaExpressionSyntax)).OfType<VBSyntax.StatementSyntax>()
            .Any(s => points.KindOf(s) is ResumePointKind.TopTestedLoop or ResumePointKind.BottomTestedLoop && points.HasLoopConditionCapturingARefParameter(s));
    }

    private static ResumePoints CreateResumePoints(OnErrorModel model, SemanticModel semanticModel) =>
        new(model, semanticModel, index => model.IsDynamic || model.ActiveHandlers[index].Kind == ActiveHandlerKind.ResumeNext);

    public async Task<IReadOnlyCollection<StatementSyntax>> EmitAsync()
    {
        var visitor = _context.Visitor;
        visitor.OnErrorStatementConverter = new ResumeNextStatementConverter(_err, _ignoringErrorsFlag);
        visitor.DecorateStatementVisitor(wrapped => new GuardingStatementVisitor(wrapped, _guards));
        var statements = await _context.ConvertAsync(_model.Statements);
        return DeclareFlag().Concat(MissingReturn.Add(_context, _model, _guards, statements)).ToList();
    }

    private IEnumerable<StatementSyntax> DeclareFlag()
    {
        if (_ignoringErrorsFlag == null) yield break;
        yield return SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)),
            SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(_ignoringErrorsFlag), null,
                SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))))));
    }
}
