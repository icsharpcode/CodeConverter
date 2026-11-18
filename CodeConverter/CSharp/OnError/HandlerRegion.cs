namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// The statements of an <c>On Error GoTo</c> handler: from its label to the next method-level label or the end of the method.
/// </summary>
/// <param name="LabelIndex">Index of the handler's label among the method-level statements, or -1 when the label is inside a block.</param>
/// <param name="EndIndex">Exclusive end of the handler's statements.</param>
internal sealed record HandlerRegion(string Label, int LabelIndex, int EndIndex, IReadOnlyList<HandlerExit> Exits)
{
    public bool IsMethodLevel => LabelIndex >= 0;

    public bool Contains(int statementIndex) => IsMethodLevel && LabelIndex <= statementIndex && statementIndex < EndIndex;

    public IEnumerable<string> ExitLabels => Exits.Where(e => e.IsJumpToLabel).Select(e => e.TargetLabel).Distinct();
}
