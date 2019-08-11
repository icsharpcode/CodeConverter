using ICSharpCode.CodeConverter.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class CommentConvertingVisitorWrapper<T> where T: SyntaxNode
    {
        private readonly VisualBasicSyntaxVisitor<T> _wrappedVisitor;

        public CommentConvertingVisitorWrapper(VisualBasicSyntaxVisitor<T> wrappedVisitor, TriviaConverter triviaConverter)
        {
            TriviaConverter = triviaConverter;
            _wrappedVisitor = wrappedVisitor;
        }

        public TriviaConverter TriviaConverter { get; }

        public T Visit(SyntaxNode node)
        {
            return TriviaConverter.PortConvertedTrivia(node, _wrappedVisitor.Visit(node));
        }
    }
}