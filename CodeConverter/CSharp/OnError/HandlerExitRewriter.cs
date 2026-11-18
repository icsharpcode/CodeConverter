using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// Turns the method's own <c>return</c>s in the handler into the handler function's way of saying "leave the method".
/// </summary>
/// <remarks>
/// A <c>return</c> in a local function only leaves that function, so the value of <c>Return expr</c> goes into a variable that the caller returns,
/// and the function returns the value that means exit. VB clears <c>Err</c> on leaving the method while in a handler, after evaluating the returned value.
/// Returns inside lambdas and local functions are not the method's.
/// </remarks>
internal sealed class HandlerExitRewriter : CSharpSyntaxRewriter
{
    private static readonly SyntaxAnnotation SpliceAnnotation = new();
    private readonly ErrStatements _err;
    private readonly string _resultVariable;
    private readonly ExpressionSyntax _exitValue;

    /// <param name="resultVariable">The variable the caller returns, or null when the handler has no <c>Return expr</c>.</param>
    /// <param name="exitValue">What the function returns to leave the method.</param>
    private HandlerExitRewriter(ErrStatements err, string resultVariable, ExpressionSyntax exitValue)
    {
        _err = err;
        _resultVariable = resultVariable;
        _exitValue = exitValue;
    }

    public static IEnumerable<StatementSyntax> Rewrite(IEnumerable<StatementSyntax> statements, ErrStatements err, string resultVariable, ExpressionSyntax exitValue)
    {
        var rewriter = new HandlerExitRewriter(err, resultVariable, exitValue);
        return Splice(statements.Select(s => (StatementSyntax)rewriter.Visit(s)));
    }

    /// <summary>The statements that leave the method when the handler runs off its end.</summary>
    public static IEnumerable<StatementSyntax> Exit(ErrStatements err, ExpressionSyntax exitValue) =>
        err.Clear().Add(SyntaxFactory.ReturnStatement(exitValue));

    public override SyntaxNode VisitReturnStatement(ReturnStatementSyntax node)
    {
        if (node.HasAnnotation(HandlerFunctionStatementConverter.ResumeReturn)) return node;
        var statements = new List<StatementSyntax>();
        if (node.Expression != null && node.Expression is not DefaultExpressionSyntax && !IsDefaultLiteral(node.Expression) && !IsResultVariable(node.Expression)) {
            statements.Add(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(_resultVariable), node.Expression)));
        }
        statements.AddRange(Exit(_err, _exitValue));
        return SyntaxFactory.Block(statements).WithAdditionalAnnotations(SpliceAnnotation);
    }

    public override SyntaxNode VisitBlock(BlockSyntax node) =>
        node.WithStatements(SyntaxFactory.List(Splice(node.Statements.Select(s => (StatementSyntax)Visit(s)))));

    public override SyntaxNode VisitLocalFunctionStatement(LocalFunctionStatementSyntax node) => node;
    public override SyntaxNode VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) => node;
    public override SyntaxNode VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) => node;
    public override SyntaxNode VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node) => node;

    private static bool IsDefaultLiteral(ExpressionSyntax expression) => expression.IsKind(SyntaxKind.DefaultLiteralExpression);

    private bool IsResultVariable(ExpressionSyntax expression) => expression is IdentifierNameSyntax identifier && identifier.Identifier.ValueText == _resultVariable;

    private static IEnumerable<StatementSyntax> Splice(IEnumerable<StatementSyntax> statements) =>
        statements.SelectMany(s => s is BlockSyntax block && block.HasAnnotation(SpliceAnnotation) ? block.Statements : (IEnumerable<StatementSyntax>)new[] { s });
}
