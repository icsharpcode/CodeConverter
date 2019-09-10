using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class CommentConvertingVisitorWrapper<T> where T: SyntaxNode
    {
        private readonly VisualBasicSyntaxVisitor<Task<T>> _wrappedVisitor;

        public CommentConvertingVisitorWrapper(VisualBasicSyntaxVisitor<Task<T>> wrappedVisitor, TriviaConverter triviaConverter)
        {
            TriviaConverter = triviaConverter;
            _wrappedVisitor = wrappedVisitor;
        }

        public TriviaConverter TriviaConverter { get; }

        public async Task<T> Visit(SyntaxNode node)
        {
            return TriviaConverter.PortConvertedTrivia(node, await _wrappedVisitor.Visit(node));
        }
    }
}