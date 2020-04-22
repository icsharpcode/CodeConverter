using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.VisualBasic;
using ArgumentSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.ArgumentSyntax;
using BinaryExpressionSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.BinaryExpressionSyntax;
using LambdaExpressionSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.LambdaExpressionSyntax;
using ParenthesizedExpressionSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.ParenthesizedExpressionSyntax;
using ReturnStatementSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.ReturnStatementSyntax;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class VbSyntaxNodeExtensions
    {
        public static ExpressionSyntax ParenthesizeIfPrecedenceCouldChange(this VisualBasicSyntaxNode node, ExpressionSyntax expression)
        {
            return PrecedenceCouldChange(node) ? SyntaxFactory.ParenthesizedExpression(expression) : expression;
        }

        public static bool PrecedenceCouldChange(this VisualBasicSyntaxNode node)
        {
            bool parentIsBinaryExpression = node is BinaryExpressionSyntax;
            bool parentIsReturn = node.Parent is ReturnStatementSyntax;
            bool parentIsLambda = node.Parent is LambdaExpressionSyntax;
            bool parentIsNonArgumentExpression = node.Parent is Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax && !(node.Parent is ArgumentSyntax);
            bool parentIsParenthesis = node.Parent is ParenthesizedExpressionSyntax;

            return parentIsNonArgumentExpression && !parentIsBinaryExpression && !parentIsReturn && !parentIsLambda && !parentIsParenthesis;
        }
    }
}
