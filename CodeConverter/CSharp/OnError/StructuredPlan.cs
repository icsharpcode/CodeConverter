namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// Where the parts of a structured method are, as indexes into its method-level statements.
/// </summary>
/// <remarks>
/// Layout: statements before <see cref="OnErrorIndex"/>; the protected <see cref="Segments"/>; statements up to the handler's label that no segment protects; the handler.
/// Plain form: a single segment, the covered statements from the <c>On Error GoTo</c> to the first label the handler leaves to.
/// Segmented form: also one segment per label the handler resumes to, so an error after resuming enters the handler again as in VB.
/// Tail: when the handler leaves to a label after itself, the statements from the end of the handler's region come last, after the <c>try</c>, unprotected.
/// </remarks>
/// <param name="HandlerReachedWithoutError">The statement before the handler's label can run into it, making it ordinary code too.</param>
/// <param name="Segments">The ranges that each go in their own <c>try</c>.</param>
/// <param name="HasTail">The handler leaves to a label after its own statements, so the statements from there on are emitted after the <c>try</c>.</param>
internal sealed record StructuredPlan(int OnErrorIndex, HandlerRegion Handler, IReadOnlyList<StatementRange> Segments, bool HandlerReachedWithoutError, int StatementCount, bool HasTail = false)
{
    public bool IsSegmented => Segments.Count > 1;
    public StatementRange Covered => Segments[0];

    /// <summary>The statements between the last segment and the handler's label.</summary>
    public int UnprotectedStart => Segments.Last().End;

    /// <summary>The statements after the handler's region, which the handler leaves to. Empty when there is no tail.</summary>
    public StatementRange Tail => HasTail ? new StatementRange(-1, Handler.EndIndex, StatementCount) : new StatementRange(-1, StatementCount, StatementCount);
}

/// <summary>Method-level statements [<paramref name="Start"/>, <paramref name="End"/>), preceded by the label at <paramref name="LabelIndex"/> (-1 for none).</summary>
internal sealed record StatementRange(int LabelIndex, int Start, int End)
{
    public bool Contains(int statementIndex) => Start <= statementIndex && statementIndex < End;
}
