using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// What a guard's <c>catch</c> does with an error, apart from recording it in <c>Err</c>: the part of README.md "Output shapes" that differs between shapes 2 to 4.
/// </summary>
internal abstract class GuardPolicy
{
    /// <summary>Null for a <c>catch</c> that always catches.</summary>
    public virtual ExpressionSyntax Filter => null;

    /// <summary>Whether <see cref="OnError"/> may jump back to the label placed before the guarded statement, which VB's <c>Resume</c> does.</summary>
    public virtual bool CanRetry => false;

    /// <summary>Whether <see cref="OnError"/> may leave the method, which can't be done from a local function.</summary>
    public virtual bool CanExitMethod => false;

    /// <param name="retryLabel">Null unless <see cref="CanRetry"/>.</param>
    public virtual IEnumerable<StatementSyntax> OnError(string retryLabel) => Enumerable.Empty<StatementSyntax>();
}

/// <summary>
/// <c>On Error Resume Next</c> is active: the error is recorded and the next statement runs.
/// </summary>
internal sealed class ResumeNextGuardPolicy : GuardPolicy
{
}

/// <summary>
/// Whether <c>On Error Resume Next</c> is active is only known when the method runs, so a flag set where VB's <c>On Error</c> statements were decides whether to catch.
/// </summary>
internal sealed class DynamicGuardPolicy : GuardPolicy
{
    public DynamicGuardPolicy(string flagName) => Filter = SyntaxFactory.IdentifierName(flagName);

    public override ExpressionSyntax Filter { get; }
}
