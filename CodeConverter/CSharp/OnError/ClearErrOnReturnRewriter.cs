using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// Clears <c>Err</c> at each <c>return</c> from a handler, since VB clears it when the method is left while in a handler.
/// </summary>
/// <remarks>
/// VB evaluates the returned value first, which may read <c>Err</c>, so a returned value that isn't obviously independent of it
/// is returned from a <c>try</c> with the clear in its <c>finally</c>.
/// Returns inside lambdas and local functions leave those, not the method, so are left alone.
/// </remarks>
internal sealed class ClearErrOnReturnRewriter : CSharpSyntaxRewriter
{
    private static readonly SyntaxAnnotation SpliceAnnotation = new();
    private readonly ErrStatements _err;
    private readonly IdentifierNameSyntax _returnVariable;

    private ClearErrOnReturnRewriter(ErrStatements err, IdentifierNameSyntax returnVariable)
    {
        _err = err;
        _returnVariable = returnVariable;
    }

    public static IEnumerable<StatementSyntax> Rewrite(IEnumerable<StatementSyntax> statements, ErrStatements err, IdentifierNameSyntax returnVariable)
    {
        var rewriter = new ClearErrOnReturnRewriter(err, returnVariable);
        return Splice(statements.Select(s => (StatementSyntax)rewriter.Visit(s)));
    }

    public override SyntaxNode VisitReturnStatement(ReturnStatementSyntax node)
    {
        if (node.Expression == null || IsIndependentOfErr(node.Expression)) {
            return SyntaxFactory.Block(_err.Clear().Add(node)).WithAdditionalAnnotations(SpliceAnnotation);
        }
        return SyntaxFactory.TryStatement(SyntaxFactory.Block(node), default, SyntaxFactory.FinallyClause(SyntaxFactory.Block(_err.Clear())));
    }

    public override SyntaxNode VisitBlock(BlockSyntax node) =>
        node.WithStatements(SyntaxFactory.List(Splice(node.Statements.Select(s => (StatementSyntax)Visit(s)))));

    public override SyntaxNode VisitLocalFunctionStatement(LocalFunctionStatementSyntax node) => node;
    public override SyntaxNode VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) => node;
    public override SyntaxNode VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) => node;
    public override SyntaxNode VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node) => node;

    private bool IsIndependentOfErr(ExpressionSyntax expression) =>
        expression is LiteralExpressionSyntax or DefaultExpressionSyntax ||
        _returnVariable != null && expression is IdentifierNameSyntax identifier && identifier.Identifier.ValueText == _returnVariable.Identifier.ValueText;

    private static IEnumerable<StatementSyntax> Splice(IEnumerable<StatementSyntax> statements) =>
        statements.SelectMany(s => s is BlockSyntax block && block.HasAnnotation(SpliceAnnotation) ? block.Statements : (IEnumerable<StatementSyntax>)new[] { s });
}
