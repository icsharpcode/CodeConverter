using VBasicSyntaxKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// Chooses the simplest output shape (README.md "Output shapes") that is right for the method.
/// </summary>
internal static class OnErrorShapeSelector
{
    public static OnErrorShape Select(OnErrorModel model, SemanticModel semanticModel)
    {
        if (!model.HasOnErrorOrResume) return OnErrorShape.Of(OnErrorShapeKind.None);
        if (model.OnErrorStatements.Any(s => s.IsKind(VBasicSyntaxKind.OnErrorGoToMinusOneStatement))) return OnErrorShape.Of(OnErrorShapeKind.Fallback);
        if (model.OnErrorStatements.All(s => s.IsKind(VBasicSyntaxKind.OnErrorGoToZeroStatement)) && !model.ResumeStatements.Any()) return OnErrorShape.Of(OnErrorShapeKind.ClearErrOnly);
        if (model.IsDynamic) return OnErrorShape.Of(OnErrorShapeKind.Dynamic);
        if (model.Handlers.Any(h => !h.IsMethodLevel) || model.ResumeStatements.Any(r => model.HandlerContaining(model.MethodLevelIndexOf(r)) == null)) {
            return OnErrorShape.Of(OnErrorShapeKind.Fallback);
        }
        if (model.ActiveHandlers.Contains(ActiveHandler.ResumeNext) || model.OnErrorStatements.Any(s => s is VBSyntax.OnErrorResumeNextStatementSyntax)) {
            return OnErrorShape.Of(OnErrorShapeKind.ResumeNext);
        }
        if (StructuredPlanner.TryPlan(model, semanticModel) is { } plan) return new OnErrorShape(OnErrorShapeKind.Structured, plan);
        return model.Handlers.Any(h => ResumesIntoCoveredStatements(model, h)) ? OnErrorShape.Of(OnErrorShapeKind.HandlerFunction) : OnErrorShape.Of(OnErrorShapeKind.Fallback);
    }

    /// <summary>
    /// Resuming into the statements a handler covers needs a way back into the middle of them, which only a handler called from each statement's catch gives.
    /// Several handlers or <c>On Error GoTo</c> statements are also left to that shape.
    /// </summary>
    private static bool ResumesIntoCoveredStatements(OnErrorModel model, HandlerRegion handler)
    {
        var onErrorIndexes = model.OnErrorStatements.Select(model.MethodLevelIndexOf).ToArray();
        return model.Handlers.Count > 1 || handler.Exits.Any(e =>
            e.Kind is HandlerExitKind.Resume or HandlerExitKind.ResumeNext ||
            e.Kind is HandlerExitKind.ResumeLabel or HandlerExitKind.GoToLabel && model.MethodLevelIndexOfLabel(e.TargetLabel) is var target && target >= 0 &&
            target <= onErrorIndexes.Max());
    }
}
