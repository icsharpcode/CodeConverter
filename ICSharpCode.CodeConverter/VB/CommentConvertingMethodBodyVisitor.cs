using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.VB
{
    public class CommentConvertingMethodBodyVisitor : CSharpSyntaxVisitor<SyntaxList<VBSyntax.StatementSyntax>>
    {
        private readonly CSharpSyntaxVisitor<SyntaxList<VBSyntax.StatementSyntax>> wrappedVisitor;
        private readonly TriviaConverter triviaConverter;

        public CommentConvertingMethodBodyVisitor(CSharpSyntaxVisitor<SyntaxList<VBSyntax.StatementSyntax>> wrappedVisitor, TriviaConverter triviaConverter)
        {
            this.wrappedVisitor = wrappedVisitor;
            this.triviaConverter = triviaConverter;
        }

        public override SyntaxList<VBSyntax.StatementSyntax> DefaultVisit(SyntaxNode node)
        {
            var syntaxNodes = wrappedVisitor.Visit(node);
            // Port trivia to the last statement in the list
            if (!syntaxNodes.Any()) return syntaxNodes;
            var lastWithConvertedTrivia = triviaConverter.PortConvertedTrivia(node, syntaxNodes.LastOrDefault());
            return syntaxNodes.Replace(syntaxNodes.LastOrDefault(), lastWithConvertedTrivia);
        }
    }
}