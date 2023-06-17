using Microsoft.CodeAnalysis.CSharp.Syntax;
using BinaryExpressionSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.BinaryExpressionSyntax;

namespace ICSharpCode.CodeConverter.CSharp;

public interface IOperatorConverter
{
    Task<ExpressionSyntax> ConvertReferenceOrNothingComparisonOrNullAsync(VBSyntax.ExpressionSyntax exprNode, bool inExpressionLambda, bool negateExpression = false);
    Task<ExpressionSyntax> ConvertRewrittenBinaryOperatorOrNullAsync(BinaryExpressionSyntax node, bool inExpressionLambda);
}