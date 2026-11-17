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
    Fallback
}

/// <param name="Structured">Set when <paramref name="Kind"/> is <see cref="OnErrorShapeKind.Structured"/>.</param>
internal sealed record OnErrorShape(OnErrorShapeKind Kind, StructuredPlan Structured = null)
{
    public static OnErrorShape Of(OnErrorShapeKind kind) => new(kind);
}
