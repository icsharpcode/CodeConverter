using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class VbSyntaxNodeExtensions
    {
        public static ExpressionSyntax ParenthesizeIfPrecedenceCouldChange(Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxNode node, ExpressionSyntax expression)
        {
            return PrecedenceCouldChange(node) ? SyntaxFactory.ParenthesizedExpression(expression) : expression;
        }

        public static bool PrecedenceCouldChange(Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxNode node)
        {
            bool parentIsSameBinaryKind = node is Microsoft.CodeAnalysis.VisualBasic.Syntax.BinaryExpressionSyntax && node.Parent is Microsoft.CodeAnalysis.VisualBasic.Syntax.BinaryExpressionSyntax parent && parent.Kind() == node.Kind();
            bool parentIsReturn = node.Parent is Microsoft.CodeAnalysis.VisualBasic.Syntax.ReturnStatementSyntax;
            bool parentIsLambda = node.Parent is Microsoft.CodeAnalysis.VisualBasic.Syntax.LambdaExpressionSyntax;
            bool parentIsNonArgumentExpression = node.Parent is Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax && !(node.Parent is Microsoft.CodeAnalysis.VisualBasic.Syntax.ArgumentSyntax);
            bool parentIsParenthesis = node.Parent is Microsoft.CodeAnalysis.VisualBasic.Syntax.ParenthesizedExpressionSyntax;

            // Could be a full C# precedence table - this is just a common case
            bool parentIsAndOr = node.Parent.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.AndAlsoExpression, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.OrElseExpression);
            bool nodeIsRelationalOrEqual = node.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.EqualsExpression, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.NotEqualsExpression,
                Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.LessThanExpression, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.LessThanOrEqualExpression,
                Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.GreaterThanExpression, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.GreaterThanOrEqualExpression);
            bool csharpPrecedenceSame = parentIsAndOr && nodeIsRelationalOrEqual;

            return parentIsNonArgumentExpression && !parentIsSameBinaryKind && !parentIsReturn && !parentIsLambda && !parentIsParenthesis && !csharpPrecedenceSame;
        }
    }
}
