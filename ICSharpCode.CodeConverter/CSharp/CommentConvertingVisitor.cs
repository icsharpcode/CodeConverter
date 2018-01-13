using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;

namespace ICSharpCode.CodeConverter.CSharp
{
    public class CommentConvertingVisitor : VisualBasicSyntaxVisitor<CSharpSyntaxNode>
    {
        private readonly VisualBasicSyntaxVisitor<CSharpSyntaxNode> wrappedVisitor;

        public CommentConvertingVisitor(VisualBasicSyntaxVisitor<CSharpSyntaxNode> wrappedVisitor)
        {
            this.wrappedVisitor = wrappedVisitor;
        }

        public override CSharpSyntaxNode DefaultVisit(SyntaxNode node)
        {
            return wrappedVisitor.Visit(node).WithConvertedTriviaFrom(node);
        }
    }
}