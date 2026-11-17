using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// VB's flow analysis says a method ending in <c>Return</c> can't reach its end, so the caller adds no final return, but a guarded <c>return</c> can fall through.
/// </summary>
internal static class MissingReturn
{
    public static IEnumerable<StatementSyntax> Add(OnErrorConversionContext context, OnErrorModel model, StatementGuards guards, List<StatementSyntax> statements)
    {
        if (!guards.GuardedAReturn) return statements;
        var exit = context.Visitor.CreateMethodExitStatement(model.Statements.First());
        var endIsReachable = context.SemanticModel.AnalyzeControlFlow(model.Statements.First(), model.Statements.Last()).EndPointIsReachable;
        var endsInExit = statements.LastOrDefault() is ReturnStatementSyntax or ThrowStatementSyntax;
        return exit is ReturnStatementSyntax { Expression: not null } && !endIsReachable && !endsInExit ? statements.Append(exit) : statements;
    }
}
