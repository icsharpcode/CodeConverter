using System;
using System.Linq;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using VbSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using CsSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory;
using System.Collections;

namespace ICSharpCode.CodeConverter.VB
{
    internal class CommentConvertingNodesVisitor : CSharpSyntaxVisitor<VisualBasicSyntaxNode>
    {
        public TriviaConverter TriviaConverter { get; }
        private readonly CSharpSyntaxVisitor<VisualBasicSyntaxNode> _wrappedVisitor;
        private BitArray _lineTriviaMapped;
        private SemanticModel _semanticModel;
        private TextLineCollection _lines;

        public CommentConvertingNodesVisitor(CSharpSyntaxVisitor<VisualBasicSyntaxNode> wrappedVisitor, BitArray lineTriviaMapped, SemanticModel semanticModel)
        {
            TriviaConverter = new TriviaConverter();
            _wrappedVisitor = wrappedVisitor;
            _lineTriviaMapped = lineTriviaMapped;
            _semanticModel = semanticModel;
            _lines = semanticModel.SyntaxTree.GetText().Lines; //TODO: Consider using GetTextAsync
        }

        public override VisualBasicSyntaxNode DefaultVisit(SyntaxNode node)
        {
            try {
                var converted = _wrappedVisitor.Visit(node);
                if (converted == null) return converted;
                converted = node.CopyAnnotationsTo(converted);
                var origLinespan = node.SyntaxTree.GetLineSpan(node.Span);
                if (origLinespan.StartLinePosition.Line != origLinespan.EndLinePosition.Line) {
                    return converted;
                }
                return converted.WithAdditionalAnnotations(new SyntaxAnnotation(AnnotationConstants.WithinOriginalLineAnnotationKind, origLinespan.StartLinePosition.Line.ToString()));
            } catch (Exception e) {
                var dummyStatement = SyntaxFactory.EmptyStatement();
                return dummyStatement.WithVbTrailingErrorComment<VbSyntax.StatementSyntax>((CSharpSyntaxNode) node, e);
            }
        }

        public override VisualBasicSyntaxNode VisitAttributeList(CsSyntax.AttributeListSyntax node)
        {
            var convertedNode = _wrappedVisitor.Visit(node)
                .WithPrependedLeadingTrivia(SyntaxFactory.EndOfLineTrivia(Environment.NewLine));
            return convertedNode;
        }
    }
}
