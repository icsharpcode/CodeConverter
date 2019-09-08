using System;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxNodeExtensions = ICSharpCode.CodeConverter.Util.SyntaxNodeExtensions;

namespace ICSharpCode.CodeConverter.CSharp
{
    public class CommentConvertingMethodBodyVisitor : VisualBasicSyntaxVisitor<SyntaxList<CSSyntax.StatementSyntax>>
    {
        private readonly VisualBasicSyntaxVisitor<Task<SyntaxList<CSSyntax.StatementSyntax>>> _wrappedVisitor;
        private readonly TriviaConverter _triviaConverter;

        public CommentConvertingMethodBodyVisitor(VisualBasicSyntaxVisitor<Task<SyntaxList<CSSyntax.StatementSyntax>>> wrappedVisitor, TriviaConverter triviaConverter)
        {
            this._wrappedVisitor = wrappedVisitor;
            this._triviaConverter = triviaConverter;
        }

        public override SyntaxList<CSSyntax.StatementSyntax> DefaultVisit(SyntaxNode node)
        {
            try {
                return ConvertWithTrivia(node);
            } catch (Exception e) {
                var withTrailingErrorComment = SyntaxFactory.EmptyStatement()
                    .WithCsTrailingErrorComment<CSSyntax.StatementSyntax>((VisualBasicSyntaxNode) node, e);
                return SyntaxFactory.SingletonList(withTrailingErrorComment);
            }
        }

        private SyntaxList<CSSyntax.StatementSyntax> ConvertWithTrivia(SyntaxNode node)
        {
            var convertedNodes = await _wrappedVisitor.Visit(node);
            if (!convertedNodes.Any()) return convertedNodes;
            // Port trivia to the last statement in the list
            var lastWithConvertedTrivia = _triviaConverter.PortConvertedTrivia(node, convertedNodes.LastOrDefault());
            return convertedNodes.Replace(convertedNodes.LastOrDefault(), lastWithConvertedTrivia);
        }

        public override SyntaxList<CSSyntax.StatementSyntax> VisitTryBlock(TryBlockSyntax node)
        {
            var cSharpSyntaxNodes = await _wrappedVisitor.Visit(node);
            var tryStatementCs = (CSSyntax.TryStatementSyntax)cSharpSyntaxNodes.Single();
            var tryTokenCs = tryStatementCs.TryKeyword;
            var tryStatementWithTryTrivia = tryStatementCs.ReplaceToken(tryTokenCs, tryTokenCs.WithConvertedTriviaFrom(node.TryStatement));
            var tryStatementWithAllTrivia = _triviaConverter.PortConvertedTrivia(node, tryStatementWithTryTrivia);
            return cSharpSyntaxNodes.Replace(tryStatementCs, tryStatementWithAllTrivia);
        }
    }
}