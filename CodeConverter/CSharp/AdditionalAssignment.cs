using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class AdditionalAssignment : IHoistedNode
    {
        public AdditionalAssignment(IdentifierNameSyntax identifierName, ExpressionSyntax expression)
        {
            IdentifierName = identifierName ?? throw new System.ArgumentNullException(nameof(identifierName));
            Expression = expression ?? throw new System.ArgumentNullException(nameof(expression));
        }

        public ExpressionSyntax Expression { get; set; }
        public IdentifierNameSyntax IdentifierName { get; }
    }
}