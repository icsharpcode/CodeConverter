using VBasicSyntaxKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// Recognises the structured shape: one method-level <c>On Error GoTo H</c> whose handler never resumes into the statements it covers,
/// so a <c>try</c> around them with the handler in the <c>catch</c> behaves the same.
/// </summary>
/// <remarks>
/// C# can't jump into a <c>try</c> or <c>catch</c>, so nothing outside a protected segment may jump into it, nor into the handler.
/// See README.md "Secondary errors" for when the statements the handler resumes to are protected too.
/// </remarks>
internal static class StructuredPlanner
{
    public static StructuredPlan TryPlan(OnErrorModel model, SemanticModel semanticModel)
    {
        var onErrorGoTos = model.OnErrorStatements.Where(s => s.IsKind(VBasicSyntaxKind.OnErrorGoToLabelStatement)).ToArray();
        if (onErrorGoTos.Length != 1) return null;
        var handler = model.Handlers.Single();
        var onErrorIndex = model.MethodLevelIndexOf(onErrorGoTos.Single());
        if (handler.LabelIndex < onErrorIndex) return null;

        var coverageEnd = model.OnErrorStatements.Select(model.MethodLevelIndexOf)
            .Where(i => onErrorIndex < i && i < handler.LabelIndex).DefaultIfEmpty(handler.LabelIndex).Min();
        var allExitLabelIndexes = handler.ExitLabels.Select(model.MethodLevelIndexOfLabel).ToArray();
        if (!handler.Exits.All(IsStructuredExit) || allExitLabelIndexes.Any(i => i <= onErrorIndex || handler.Contains(i))) return null;
        var hasTail = allExitLabelIndexes.Any(i => i >= handler.EndIndex);
        var exitLabelIndexes = allExitLabelIndexes.Where(i => i < handler.LabelIndex).ToArray();

        var handlerReachedWithoutError = OnErrorModel.EndPointIsReachable(model.Statements[handler.LabelIndex - 1], semanticModel);
        if (handlerReachedWithoutError && allExitLabelIndexes.Any()) return null;

        var coveredEnd = exitLabelIndexes.Where(i => i < coverageEnd).DefaultIfEmpty(coverageEnd).Min();
        var covered = new StatementRange(-1, onErrorIndex + 1, coveredEnd);
        if (!CanOnlyBeEnteredFromTheTop(model, covered) || IsJumpedIntoFromOutside(model, handler)) return null;

        var tail = new StatementRange(-1, handler.EndIndex, model.Statements.Count);
        if (hasTail && (IsJumpedIntoNestedBlockFromOutside(model, tail) || TailIsReEnteredByTheHandler(model, handler, tail, semanticModel))) return null;

        var segmented = TrySegment(model, handler, covered, coverageEnd, semanticModel);
        if (segmented == null && OnlyTheCodeAfterAResumeLabelCanFail(model, handler, covered, coverageEnd, semanticModel)) return null;
        return new StructuredPlan(onErrorIndex, handler, segmented ?? new[] { covered }, handlerReachedWithoutError, model.Statements.Count, hasTail);
    }

    private static bool IsStructuredExit(HandlerExit exit) =>
        exit.Kind is HandlerExitKind.ExitMethod or HandlerExitKind.FallsOffEnd or HandlerExitKind.ResumeLabel or HandlerExitKind.GoToLabel;

    /// <summary>
    /// Segments only when a secondary error is possible and the handler is written for re-entry; otherwise secondary errors propagate (README.md "Secondary errors").
    /// </summary>
    private static IReadOnlyList<StatementRange> TrySegment(OnErrorModel model, HandlerRegion handler, StatementRange covered, int coverageEnd, SemanticModel semanticModel)
    {
        if (handler.Exits.Any(e => e.Kind == HandlerExitKind.GoToLabel)) return null;
        var resumeLabelIndexes = handler.ExitLabels.Select(model.MethodLevelIndexOfLabel).Where(i => i < coverageEnd).OrderBy(i => i).ToArray();
        if (!resumeLabelIndexes.Any()) return null;

        var segments = resumeLabelIndexes
            .Select((labelIndex, n) => new StatementRange(labelIndex, labelIndex + 1, n + 1 < resumeLabelIndexes.Length ? resumeLabelIndexes[n + 1] : coverageEnd))
            .ToArray();
        var resumedToStatements = segments.SelectMany(s => StatementsIn(model, s)).ToArray();
        var handlerStatements = model.Statements.Skip(handler.LabelIndex + 1).Take(handler.EndIndex - handler.LabelIndex - 1);
        if (!segments.Any(s => SecondaryErrorIsPossible(StatementsIn(model, s), semanticModel)) ||
            !ReEntryGuard.IsPresent(handlerStatements, resumedToStatements, semanticModel) ||
            !segments.All(s => CanOnlyBeEnteredFromTheTop(model, s))) {
            return null;
        }
        return new[] { covered }.Concat(segments).ToArray();
    }

    /// <summary>
    /// The handler resumes to a label, nothing before the label can fail, and the code after it can. The plain form would leave the only code the handler can be for unprotected,
    /// so even the first error would escape. Unlike a cleanup after the label, this can't be a case of a handler that VB would loop in forever (README.md "Secondary errors").
    /// </summary>
    private static bool OnlyTheCodeAfterAResumeLabelCanFail(OnErrorModel model, HandlerRegion handler, StatementRange covered, int coverageEnd, SemanticModel semanticModel)
    {
        var resumesToCoveredLabel = handler.Exits.Any(e => e.Kind == HandlerExitKind.ResumeLabel && model.MethodLevelIndexOfLabel(e.TargetLabel) is var i && i >= covered.End && i < coverageEnd);
        return resumesToCoveredLabel &&
               !StatementsIn(model, covered).Any(s => ThrowAnalysis.CanThrow(s, semanticModel)) &&
               SecondaryErrorIsPossible(StatementsIn(model, new StatementRange(-1, covered.End, coverageEnd)), semanticModel);
    }

    /// <summary>
    /// Code that starts with its own <c>On Error</c> statement no longer has this handler active.
    /// </summary>
    private static bool SecondaryErrorIsPossible(IReadOnlyCollection<VBSyntax.StatementSyntax> statements, SemanticModel semanticModel) =>
        !OnErrorModel.IsOnErrorStatement(statements.FirstOrDefault(s => s is not VBSyntax.LabelStatementSyntax)) &&
        statements.Any(s => ThrowAnalysis.CanThrow(s, semanticModel));

    private static IReadOnlyCollection<VBSyntax.StatementSyntax> StatementsIn(OnErrorModel model, StatementRange segment) =>
        model.Statements.Skip(segment.Start).Take(segment.End - segment.Start).ToArray();

    private static bool CanOnlyBeEnteredFromTheTop(OnErrorModel model, StatementRange segment) =>
        model.Jumps.All(j => !segment.Contains(model.IndexContainingLabel(j.TargetLabel)) || segment.Contains(model.MethodLevelIndexOf(j.Statement)));

    /// <summary>The tail is entered by jumping to its method-level labels, which C# allows, but not into a nested block.</summary>
    private static bool IsJumpedIntoNestedBlockFromOutside(OnErrorModel model, StatementRange tail) =>
        model.Jumps.Any(j => tail.Contains(model.IndexContainingLabel(j.TargetLabel)) && !tail.Contains(model.MethodLevelIndexOf(j.Statement)) &&
                             model.MethodLevelIndexOfLabel(j.TargetLabel) < 0);

    /// <summary>
    /// In VB the tail still has the handler active, so an error there would enter the handler again. The tail is unprotected, which is only the same if that can't matter:
    /// the tail can't fail, or the handler isn't written for re-entry (README.md "Secondary errors").
    /// </summary>
    private static bool TailIsReEnteredByTheHandler(OnErrorModel model, HandlerRegion handler, StatementRange tail, SemanticModel semanticModel)
    {
        var tailStatements = StatementsIn(model, tail);
        var handlerStatements = model.Statements.Skip(handler.LabelIndex + 1).Take(handler.EndIndex - handler.LabelIndex - 1);
        return SecondaryErrorIsPossible(tailStatements, semanticModel) && ReEntryGuard.IsPresent(handlerStatements, tailStatements, semanticModel);
    }

    private static bool IsJumpedIntoFromOutside(OnErrorModel model, HandlerRegion handler) =>
        model.Jumps.Any(j => handler.Contains(model.IndexContainingLabel(j.TargetLabel)) && !handler.Contains(model.MethodLevelIndexOf(j.Statement)));
}
