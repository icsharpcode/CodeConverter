namespace ICSharpCode.CodeConverter.Util;

internal static class SyntaxExtensions
{
    public static CS.Syntax.ExpressionSyntax SkipIntoParens(this CS.Syntax.ExpressionSyntax expression)
    {
        if (expression == null)
            return null;
        while (expression is CS.Syntax.ParenthesizedExpressionSyntax pes) {
            expression = pes.Expression;
        }
        return expression;
    }

    public static VBSyntax.ExpressionSyntax SkipIntoParens(this VBSyntax.ExpressionSyntax expression)
    {
        if (expression == null)
            return null;
        while (expression is VBSyntax.ParenthesizedExpressionSyntax pes) {
            expression = pes.Expression;
        }
        return expression;
    }

    public static CS.Syntax.ExpressionSyntax SkipOutOfParens(this CS.Syntax.ExpressionSyntax expression)
    {
        if (expression == null)
            return null;
        while (expression is CS.Syntax.ParenthesizedExpressionSyntax pes) {
            expression = pes.Parent as CS.Syntax.ExpressionSyntax;
        }
        return expression;
    }

    public static VBSyntax.ExpressionSyntax SkipOutOfParens(this VBSyntax.ExpressionSyntax expression)
    {
        if (expression == null)
            return null;
        while (expression is VBSyntax.ParenthesizedExpressionSyntax pes) {
            expression = pes.Parent as VBSyntax.ExpressionSyntax;
        }
        return expression;
    }

    public static bool IsParentKind(this SyntaxNode node, CS.SyntaxKind kind)
    {
        return node != null && node.Parent.IsKind(kind);
    }

    public static bool IsParentKind(this SyntaxNode node, VBasic.SyntaxKind kind)
    {
        return node?.Parent.IsKind(kind) == true;
    }

    public static bool IsParentKind(this SyntaxToken node, CS.SyntaxKind kind)
    {
        return node.Parent?.IsKind(kind) == true;
    }

    public static TSymbol GetEnclosingSymbol<TSymbol>(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        where TSymbol : ISymbol
    {
        for (var symbol = semanticModel.GetEnclosingSymbol(position, cancellationToken);
             symbol != null;
             symbol = symbol.ContainingSymbol) {
            if (symbol is TSymbol) {
                return (TSymbol)symbol;
            }
        }

        return default(TSymbol);
    }
}