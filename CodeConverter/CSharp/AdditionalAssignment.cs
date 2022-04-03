using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class AdditionalAssignment : IHoistedNode
    {
        public AdditionalAssignment(ExpressionSyntax lhs, ExpressionSyntax rhs)
        {
            RightHandSide = rhs ?? throw new ArgumentNullException(nameof(rhs));
            LeftHandSide = lhs ?? throw new ArgumentNullException(nameof(lhs));
        }

        public ExpressionSyntax LeftHandSide { get; set; }
        public ExpressionSyntax RightHandSide { get; }

        public static StatementSyntax CreateAssignment(AdditionalAssignment additionalAssignment)
        {
            var assign = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.AssignmentExpression(Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleAssignmentExpression, additionalAssignment.LeftHandSide, additionalAssignment.RightHandSide);
            return Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ExpressionStatement(assign);
        }
    }
}