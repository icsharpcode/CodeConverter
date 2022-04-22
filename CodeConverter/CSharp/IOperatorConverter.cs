using Microsoft.CodeAnalysis.CSharp.Syntax;
using BinaryExpressionSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.BinaryExpressionSyntax;

namespace ICSharpCode.CodeConverter.CSharp;

public interface IOperatorConverter
{
    Task<ExpressionSyntax> ConvertReferenceOrNothingComparisonOrNullAsync(Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax exprNode, bool negateExpression = false);
    Task<ExpressionSyntax> ConvertRewrittenBinaryOperatorOrNullAsync(BinaryExpressionSyntax node, bool inExpressionLambda = false);
}