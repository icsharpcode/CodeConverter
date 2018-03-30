using System;
using System.Linq;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

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
            try {
                return ConvertWithTrivia(node);
            } catch (Exception e) {
                return SyntaxFactory.SingletonList(CreateErrorCommentStatement(node, e));
            }
        }

        private SyntaxList<CSSyntax.StatementSyntax> ConvertWithTrivia(SyntaxNode node)
        {
            var convertedNodes = wrappedVisitor.Visit(node);
            if (!convertedNodes.Any()) return convertedNodes;
            // Port trivia to the last statement in the list
            var lastWithConvertedTrivia = triviaConverter.PortConvertedTrivia(node, convertedNodes.LastOrDefault());
            return convertedNodes.Replace(convertedNodes.LastOrDefault(), lastWithConvertedTrivia);
        }

        private CSSyntax.StatementSyntax CreateErrorCommentStatement(SyntaxNode node, Exception exception)
        {
            var errorDescription = node.DescribeConversionError(exception);
            var commentedText = "/* " + errorDescription + " */";
            return SyntaxFactory.EmptyStatement()
                .WithTrailingTrivia(SyntaxFactory.Comment(commentedText))
                .WithAdditionalAnnotations(new SyntaxAnnotation(AnnotationConstants.ConversionErrorAnnotationKind, exception.ToString()));
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