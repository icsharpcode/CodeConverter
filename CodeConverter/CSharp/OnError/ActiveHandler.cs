namespace ICSharpCode.CodeConverter.CSharp.OnError;

internal enum ActiveHandlerKind
{
    None,
    ResumeNext,
    Label
}

/// <summary>
/// What VB does with an error raised by a statement: the value of the lowered method's "active handler" local, set by the last <c>On Error</c> statement executed.
/// </summary>
internal sealed record ActiveHandler(ActiveHandlerKind Kind, string Label = null)
{
    public static ActiveHandler None { get; } = new(ActiveHandlerKind.None);
    public static ActiveHandler ResumeNext { get; } = new(ActiveHandlerKind.ResumeNext);
    public static ActiveHandler ForLabel(string label) => new(ActiveHandlerKind.Label, LabelName.Normalize(label));
}
