using System.Linq;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    public class CommentConvertingMethodBodyVisitor : VisualBasicSyntaxVisitor<SyntaxList<CSSyntax.StatementSyntax>>
    {
        private readonly VisualBasicSyntaxVisitor<SyntaxList<CSSyntax.StatementSyntax>> wrappedVisitor;
        private readonly TriviaConverter triviaConverter;

        public CommentConvertingMethodBodyVisitor(VisualBasicSyntaxVisitor<SyntaxList<CSSyntax.StatementSyntax>> wrappedVisitor, TriviaConverter triviaConverter)
        {
            this.wrappedVisitor = wrappedVisitor;
            this.triviaConverter = triviaConverter;
        }

        public override SyntaxList<CSSyntax.StatementSyntax> DefaultVisit(SyntaxNode node)
        {
            var cSharpSyntaxNodes = wrappedVisitor.Visit(node);
            // Port trivia to the last statement in the list
            var lastWithConvertedTrivia = triviaConverter.PortConvertedTrivia(node, cSharpSyntaxNodes.LastOrDefault());
            return cSharpSyntaxNodes.Replace(cSharpSyntaxNodes.LastOrDefault(), lastWithConvertedTrivia);
        }

        public override SyntaxList<CSSyntax.StatementSyntax> VisitTryBlock(TryBlockSyntax node)
        {
            var cSharpSyntaxNodes = wrappedVisitor.Visit(node);
            var tryStatementCs = (CSSyntax.TryStatementSyntax)cSharpSyntaxNodes.Single();
            var tryTokenCs = tryStatementCs.TryKeyword;
            var tryStatementWithTryTrivia = tryStatementCs.ReplaceToken(tryTokenCs, tryTokenCs.WithConvertedTriviaFrom(node.TryStatement));
            var tryStatementWithAllTrivia = triviaConverter.PortConvertedTrivia(node, tryStatementWithTryTrivia);
            return cSharpSyntaxNodes.Replace(tryStatementCs, tryStatementWithAllTrivia);
        }
    }
}