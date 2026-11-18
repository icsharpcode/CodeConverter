namespace ICSharpCode.CodeConverter.CSharp.OnError;

internal enum HandlerExitKind
{
    ResumeNext,
    Resume,
    ResumeLabel,
    GoToLabel,
    ExitMethod,
    FallsOffEnd,
    FallsIntoLabel
}

/// <summary>
/// A way control leaves a handler. <c>Resume*</c> also leaves VB's "in a handler" state and clears <c>Err</c>; <c>GoTo</c> and falling into a label do neither.
/// </summary>
internal sealed record HandlerExit(HandlerExitKind Kind, string TargetLabel = null)
{
    public bool IsJumpToLabel => Kind is HandlerExitKind.ResumeLabel or HandlerExitKind.GoToLabel or HandlerExitKind.FallsIntoLabel;
}
