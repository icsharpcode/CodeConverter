namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// The output shapes from README.md, simplest first.
/// </summary>
internal enum OnErrorShapeKind
{
    /// <summary>No <c>On Error</c> or <c>Resume</c>: convert statements as usual.</summary>
    None,
    Structured,
    ResumeNext,
    HandlerFunction,
    Dynamic,
    /// <summary>Only <c>On Error GoTo 0</c>: no handler is ever active, so it only clears <c>Err</c> and the statements are converted as usual.</summary>
    ClearErrOnly,
    Fallback
}

/// <param name="Structured">Set when <paramref name="Kind"/> is <see cref="OnErrorShapeKind.Structured"/>.</param>
internal sealed record OnErrorShape(OnErrorShapeKind Kind, StructuredPlan Structured = null)
{
    public static OnErrorShape Of(OnErrorShapeKind kind) => new(kind);
}
