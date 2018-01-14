using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.VisualBasic;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ICSharpCode.CodeConverter.CSharp
{
    public class CommentConvertingMethodBodyVisitor : VisualBasicSyntaxVisitor<SyntaxList<StatementSyntax>>
    {
        private readonly VisualBasicSyntaxVisitor<SyntaxList<StatementSyntax>> wrappedVisitor;
        private readonly TriviaConverter triviaConverter;

        public CommentConvertingMethodBodyVisitor(VisualBasicSyntaxVisitor<SyntaxList<StatementSyntax>> wrappedVisitor, TriviaConverter triviaConverter)
        {
            this.wrappedVisitor = wrappedVisitor;
            this.triviaConverter = triviaConverter;
        }

        public override SyntaxList<StatementSyntax> DefaultVisit(SyntaxNode node)
        {
            var cSharpSyntaxNodes = wrappedVisitor.Visit(node);
            // Port trivia to the last statement in the list
            var lastWithConvertedTrivia = triviaConverter.PortConvertedTrivia(node, cSharpSyntaxNodes.LastOrDefault());
            return cSharpSyntaxNodes.Replace(cSharpSyntaxNodes.LastOrDefault(), lastWithConvertedTrivia);
        }
    }
}