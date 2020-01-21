using System;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using CS = Microsoft.CodeAnalysis.CSharp;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxNodeExtensions = ICSharpCode.CodeConverter.Util.SyntaxNodeExtensions;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System.Linq;
using System.Collections;

namespace ICSharpCode.CodeConverter.VB
{
    public class CommentConvertingMethodBodyVisitor : CS.CSharpSyntaxVisitor<SyntaxList<VBSyntax.StatementSyntax>>
    {
        private readonly CS.CSharpSyntaxVisitor<SyntaxList<VBSyntax.StatementSyntax>> _wrappedVisitor;
        private readonly TriviaConverter _triviaConverter;
        private readonly BitArray _lineTriviaMapped;

        public CommentConvertingMethodBodyVisitor(CS.CSharpSyntaxVisitor<SyntaxList<VBSyntax.StatementSyntax>> wrappedVisitor, TriviaConverter triviaConverter, BitArray lineTriviaMapped)
        {
            this._wrappedVisitor = wrappedVisitor;
            this._triviaConverter = triviaConverter;
            _lineTriviaMapped = lineTriviaMapped;
        }

        public override SyntaxList<VBSyntax.StatementSyntax> DefaultVisit(SyntaxNode node)
        {
            try {
                var converted = _wrappedVisitor.Visit(node);
                if (!converted.Any()) return converted;
                converted = converted.Replace(converted.First(), node.CopyAnnotationsTo(converted.First()));
                var origLinespan = node.SyntaxTree.GetLineSpan(node.Span);
                if (origLinespan.StartLinePosition.Line != origLinespan.EndLinePosition.Line) {
                    return converted;
                }
                var statementsWithAnnotations = converted.Select(s => s.WithAdditionalAnnotations(new SyntaxAnnotation(AnnotationConstants.WithinOriginalLineAnnotationKind, origLinespan.StartLinePosition.Line.ToString())));
                return VBasic.SyntaxFactory.List(statementsWithAnnotations);
            } catch (Exception e) {
                var dummyStatement = VBasic.SyntaxFactory.EmptyStatement();
                var withVbTrailingErrorComment = dummyStatement.WithVbTrailingErrorComment<VBSyntax.StatementSyntax>((CS.CSharpSyntaxNode) node, e);
                return VBasic.SyntaxFactory.SingletonList(withVbTrailingErrorComment);
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
    }
}
