using static Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// Recognises README.md shape 3: one method-level handler that resumes into the statements it covers, which each guard calls from its <c>catch</c>.
/// </summary>
/// <remarks>
/// Anything the emitter can't express soundly returns null so the method keeps the fallback:
/// * more than one handler, or <c>Resume L</c> / <c>GoTo L</c> out of the handler, or more than two ways out;
/// * the handler is also run into without an error, or jumped into: a local function can't be entered by a jump;
/// * the function would capture a <c>ref</c>/<c>out</c> parameter, which C# doesn't allow;
/// * a guard inside a guarded block: a secondary error, thrown by the handler inside the inner <c>catch</c>, would be caught again by the outer one,
///   but VB's filter lets it propagate;
/// * a failing loop condition that must leave the method, which a local function can't do;
/// * a local that must be declared before its guard, or at the top for the handler, but whose type can't be written (anonymous type).
/// </remarks>
internal static class HandlerFunctionPlanner
{
    public static HandlerFunctionPlan TryPlan(OnErrorModel model, SemanticModel semanticModel)
    {
        if (model.IsDynamic || model.Handlers.Count != 1 || model.Handlers[0] is not { IsMethodLevel: true, LabelIndex: > 0 } handler) return null;
        if (!model.OnErrorStatements.All(s => s is VBSyntax.OnErrorGoToStatementSyntax) || model.ResumeStatements.Any(r => model.HandlerContaining(model.MethodLevelIndexOf(r)) == null)) return null;
        var ways = WaysOf(handler.Exits);
        if (ways == null || OnErrorModel.EndPointIsReachable(model.Statements[handler.LabelIndex - 1], semanticModel)) return null;
        if (model.Jumps.Any(j => handler.Contains(model.IndexContainingLabel(j.TargetLabel)) && !handler.Contains(model.MethodLevelIndexOf(j.Statement)))) return null;

        var handlerStatements = model.Statements.Skip(handler.LabelIndex).Take(handler.EndIndex - handler.LabelIndex).ToArray();
        var others = model.Statements.Where((_, i) => !handler.Contains(i)).ToArray();
        if (CapturesRefParameter(handlerStatements, semanticModel) || OutsideUsesLocalsOfHandler(handlerStatements, others, semanticModel) ||
            HandlerUsesLocalsThatCantBeDeclaredEarly(handlerStatements, others, semanticModel)) {
            return null;
        }

        var firstOnErrorIndex = model.OnErrorStatements.Select(model.MethodLevelIndexOf).Min();
        var plan = new HandlerFunctionPlan(handler, firstOnErrorIndex, ways.Value, ReturnsValue(handlerStatements));
        return GuardsAreSupported(model, plan, others, semanticModel) ? plan : null;
    }

    public static ResumePoints CreateResumePoints(OnErrorModel model, HandlerRegion handler, SemanticModel semanticModel) =>
        new(model, semanticModel, index => model.ActiveHandlers[index].Kind == ActiveHandlerKind.Label && !handler.Contains(index));

    private static HandlerWays? WaysOf(IReadOnlyCollection<HandlerExit> exits)
    {
        if (exits.Any(e => e.Kind is not (HandlerExitKind.ResumeNext or HandlerExitKind.Resume or HandlerExitKind.ExitMethod or HandlerExitKind.FallsOffEnd))) return null;
        var resumesNext = exits.Any(e => e.Kind == HandlerExitKind.ResumeNext);
        var retries = exits.Any(e => e.Kind == HandlerExitKind.Resume);
        var exitsMethod = exits.Any(e => e.Kind is HandlerExitKind.ExitMethod or HandlerExitKind.FallsOffEnd);
        return (resumesNext, retries, exitsMethod) switch {
            (true, false, false) => HandlerWays.ResumeNext,
            (true, true, false) => HandlerWays.RetryOrResumeNext,
            (true, false, true) => HandlerWays.ExitOrResumeNext,
            (false, true, true) => HandlerWays.RetryOrExit,
            _ => null
        };
    }

    private static bool ReturnsValue(IEnumerable<VBSyntax.StatementSyntax> handlerStatements) =>
        handlerStatements.SelectMany(s => s.DescendantNodesAndSelf(n => n is not VBSyntax.LambdaExpressionSyntax))
            .OfType<VBSyntax.ReturnStatementSyntax>().Any(r => r.Expression != null);

    private static bool CapturesRefParameter(IEnumerable<VBSyntax.StatementSyntax> statements, SemanticModel semanticModel) =>
        statements.SelectMany(s => s.DescendantNodesAndSelf()).OfType<VBSyntax.IdentifierNameSyntax>()
            .Any(i => semanticModel.GetSymbolInfo(i).Symbol is IParameterSymbol { RefKind: not RefKind.None });

    private static bool OutsideUsesLocalsOfHandler(IEnumerable<VBSyntax.StatementSyntax> handlerStatements, IEnumerable<VBSyntax.StatementSyntax> others, SemanticModel semanticModel)
    {
        var declared = handlerStatements.SelectMany(s => s.DescendantNodesAndSelf()).OfType<VBSyntax.ModifiedIdentifierSyntax>()
            .Select(n => semanticModel.GetDeclaredSymbol(n)).Where(l => l != null).ToArray();
        return declared.Any() && others.SelectMany(s => s.DescendantNodesAndSelf()).OfType<VBSyntax.IdentifierNameSyntax>()
            .Any(i => declared.Contains(semanticModel.GetSymbolInfo(i).Symbol, SymbolEqualityComparer.Default));
    }

    /// <summary>The handler is declared before the guards, so a local it uses that is declared after it has to be declared at the top.</summary>
    private static bool HandlerUsesLocalsThatCantBeDeclaredEarly(IEnumerable<VBSyntax.StatementSyntax> handlerStatements, IEnumerable<VBSyntax.StatementSyntax> others, SemanticModel semanticModel)
    {
        var declaredElsewhere = others.SelectMany(s => s.DescendantNodesAndSelf()).OfType<VBSyntax.ModifiedIdentifierSyntax>()
            .Select(n => semanticModel.GetDeclaredSymbol(n)).OfType<ILocalSymbol>().ToArray();
        return handlerStatements.SelectMany(s => s.DescendantNodesAndSelf()).OfType<VBSyntax.IdentifierNameSyntax>()
            .Select(i => semanticModel.GetSymbolInfo(i).Symbol).OfType<ILocalSymbol>()
            .Any(l => !DeclarationSplitter.CanSplit(l) && declaredElsewhere.Contains(l, SymbolEqualityComparer.Default));
    }

    private static bool GuardsAreSupported(OnErrorModel model, HandlerFunctionPlan plan, IEnumerable<VBSyntax.StatementSyntax> others, SemanticModel semanticModel)
    {
        var points = CreateResumePoints(model, plan.Handler, semanticModel);
        if (!points.CanGuardAllDeclarations(others)) return false;
        var all = others.SelectMany(s => s.DescendantNodesAndSelf(n => n is not VBSyntax.LambdaExpressionSyntax)).OfType<VBSyntax.StatementSyntax>().ToArray();
        var guarded = all.Where(s => IsGuarded(points, s, semanticModel)).ToArray();
        var loops = guarded.Where(s => points.KindOf(s) is ResumePointKind.TopTestedLoop or ResumePointKind.BottomTestedLoop).ToArray();
        if (loops.Any(points.HasLoopConditionCapturingARefParameter) || loops.Any() && plan.CanExit) return false;
        return !guarded.Any(s => s.Ancestors().OfType<VBSyntax.StatementSyntax>().Any(a => model.MethodLevelIndexOf(a) >= 0 && points.KindOf(a) == ResumePointKind.Block));
    }

    private static bool IsGuarded(ResumePoints points, VBSyntax.StatementSyntax statement, SemanticModel semanticModel) => points.KindOf(statement) switch {
        ResumePointKind.None => false,
        ResumePointKind.Declaration => points.IsCovered(statement) && ((VBSyntax.LocalDeclarationStatementSyntax)statement).Declarators.Any(d => ThrowAnalysis.CanThrow(d, semanticModel)),
        _ => true
    };
}
