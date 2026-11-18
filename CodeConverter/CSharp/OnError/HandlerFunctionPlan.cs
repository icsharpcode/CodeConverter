namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// How an <c>On Error GoTo</c> handler that resumes into the covered statements leaves, which decides what the local function returns.
/// </summary>
/// <remarks>
/// Resuming the statement after, or running the statement again, are both decided by the caller's <c>catch</c>, so the function says which with a <c>bool</c>.
/// The name of the function says what <c>true</c> means.
/// </remarks>
internal enum HandlerWays
{
    /// <summary>Only <c>Resume Next</c>: a <c>void</c> function.</summary>
    ResumeNext,
    /// <summary><c>Resume Next</c> or <c>Resume</c>: <c>true</c> runs the statement again.</summary>
    RetryOrResumeNext,
    /// <summary><c>Resume Next</c> or leaving the method: <c>true</c> leaves.</summary>
    ExitOrResumeNext,
    /// <summary><c>Resume</c> or leaving the method: <c>true</c> runs the statement again.</summary>
    RetryOrExit
}

/// <param name="FirstOnErrorIndex">Where the handler function is declared: right after the first <c>On Error GoTo</c>, so what it captures is declared before it.</param>
/// <param name="ReturnsValue">The handler has <c>Return expr</c>, whose value the function can only hand back through a variable.</param>
internal sealed record HandlerFunctionPlan(HandlerRegion Handler, int FirstOnErrorIndex, HandlerWays Ways, bool ReturnsValue)
{
    public bool CanRetry => Ways is HandlerWays.RetryOrResumeNext or HandlerWays.RetryOrExit;
    public bool CanExit => Ways is HandlerWays.ExitOrResumeNext or HandlerWays.RetryOrExit;

    /// <summary>The value the function returns for <c>Resume Next</c>, or null for a <c>void</c> function.</summary>
    public bool? ResumeNextValue => Ways switch {
        HandlerWays.ResumeNext => null,
        _ => false
    };

    public bool ResumeValue => true;

    /// <summary>What the function returns to say the method is left. For <see cref="HandlerWays.RetryOrExit"/> that's the opposite of retrying.</summary>
    public bool ExitValue => Ways == HandlerWays.ExitOrResumeNext;
}
