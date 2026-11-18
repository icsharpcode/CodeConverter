using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// Every guard calls the handler function from its <c>catch</c>, then does what it says: carry on, run the statement again (<c>goto</c> the label before its <c>try</c>) or leave.
/// </summary>
internal sealed class HandlerFunctionGuardPolicy : GuardPolicy
{
    private readonly HandlerFunctionPlan _plan;
    private readonly string _functionName;
    private readonly StatementSyntax _exitStatement;

    public HandlerFunctionGuardPolicy(HandlerFunctionPlan plan, string functionName, StatementSyntax exitStatement)
    {
        _plan = plan;
        _functionName = functionName;
        _exitStatement = exitStatement;
    }

    public override bool CanRetry => _plan.CanRetry;

    public override IEnumerable<StatementSyntax> OnError(string retryLabel)
    {
        var call = SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(_functionName));
        var retry = SyntaxFactory.GotoStatement(SyntaxKind.GotoStatement, SyntaxFactory.IdentifierName(retryLabel ?? "unused"));
        switch (_plan.Ways) {
            case HandlerWays.ResumeNext:
                return new StatementSyntax[] { SyntaxFactory.ExpressionStatement(call) };
            case HandlerWays.RetryOrResumeNext:
                return new StatementSyntax[] { SyntaxFactory.IfStatement(call, retry) };
            case HandlerWays.ExitOrResumeNext:
                return new StatementSyntax[] { SyntaxFactory.IfStatement(call, _exitStatement) };
            default:
                return new StatementSyntax[] { SyntaxFactory.IfStatement(call, retry), _exitStatement };
        }
    }
}
