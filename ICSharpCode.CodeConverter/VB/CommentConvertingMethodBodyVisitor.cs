using System;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using CS = Microsoft.CodeAnalysis.CSharp;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.VB
{
    public class CommentConvertingMethodBodyVisitor : CS.CSharpSyntaxVisitor<SyntaxList<VBSyntax.StatementSyntax>>
    {
        private readonly CS.CSharpSyntaxVisitor<SyntaxList<VBSyntax.StatementSyntax>> _wrappedVisitor;
        private readonly TriviaConverter _triviaConverter;

        public CommentConvertingMethodBodyVisitor(CS.CSharpSyntaxVisitor<SyntaxList<VBSyntax.StatementSyntax>> wrappedVisitor, TriviaConverter triviaConverter)
        {
            this._wrappedVisitor = wrappedVisitor;
            this._triviaConverter = triviaConverter;
        }

        public override SyntaxList<VBSyntax.StatementSyntax> DefaultVisit(SyntaxNode node)
        {
            try {
                return ConvertWithTrivia(node);
            } catch (Exception e) {
                return VBasic.SyntaxFactory.SingletonList(CreateErrorCommentStatement(node, e));
            }
        }

        private SyntaxList<VBSyntax.StatementSyntax> ConvertWithTrivia(SyntaxNode node)
        {
            var convertedNodes = _wrappedVisitor.Visit(node);
            if (!convertedNodes.Any()) return convertedNodes;
            // Port trivia to the last statement in the list
            var lastWithConvertedTrivia = _triviaConverter.PortConvertedTrivia(node, convertedNodes.LastOrDefault());
            return convertedNodes.Replace(convertedNodes.LastOrDefault(), lastWithConvertedTrivia);
        }

        private VBSyntax.StatementSyntax CreateErrorCommentStatement(SyntaxNode node, Exception exception)
        {
            var errorDescription = node.DescribeConversionError(exception);
            var commentedText = "''' " + errorDescription.Replace("\r\n", "\r\n''' ");
            return VBasic.SyntaxFactory.EmptyStatement()
                .WithTrailingTrivia(VBasic.SyntaxFactory.CommentTrivia(commentedText))
                .WithAdditionalAnnotations(new SyntaxAnnotation(AnnotationConstants.ConversionErrorAnnotationKind,
                    exception.ToString()));
        }
    }
}