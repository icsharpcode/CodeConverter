
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    public interface IOperatorConverter
    {
        Task<Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax> ConvertReferenceOrNothingComparisonOrNullAsync(Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax exprNode, bool negateExpression = false);
        Task<Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax> ConvertRewrittenBinaryOperatorOrNullAsync(Microsoft.CodeAnalysis.VisualBasic.Syntax.BinaryExpressionSyntax node, bool inExpressionLambda = false);
    }
}