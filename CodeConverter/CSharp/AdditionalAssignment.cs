using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class AdditionalAssignment : IHoistedNode
    {
        public AdditionalAssignment(ExpressionSyntax lhs, ExpressionSyntax rhs)
        {
            RightHandSide = rhs ?? throw new System.ArgumentNullException(nameof(rhs));
            LeftHandSide = lhs ?? throw new System.ArgumentNullException(nameof(lhs));
        }

        public ExpressionSyntax LeftHandSide { get; set; }
        public ExpressionSyntax RightHandSide { get; }
    }
}