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

namespace ICSharpCode.CodeConverter.VB
{
    public class CommentConvertingNodesVisitor : CSharpSyntaxVisitor<VisualBasicSyntaxNode>
    {
        public TriviaConverter TriviaConverter { get; }
        private readonly CSharpSyntaxVisitor<VisualBasicSyntaxNode> _wrappedVisitor;

        public CommentConvertingNodesVisitor(CSharpSyntaxVisitor<VisualBasicSyntaxNode> wrappedVisitor)
        {
            TriviaConverter = new TriviaConverter();
            this._wrappedVisitor = wrappedVisitor;
        }
        public override VisualBasicSyntaxNode DefaultVisit(SyntaxNode node)
        {
            try {
                return TriviaConverter.PortConvertedTrivia(node, _wrappedVisitor.Visit(node));
            } catch (Exception e) {
                var dummyStatement = SyntaxFactory.EmptyStatement();
                return dummyStatement.WithVbTrailingErrorComment<VbSyntax.StatementSyntax>((CSharpSyntaxNode) node, e);
            }

        }

        public override VisualBasicSyntaxNode VisitAttributeList(CsSyntax.AttributeListSyntax node)
        {
            var convertedNode = _wrappedVisitor.Visit(node)
                .WithPrependedLeadingTrivia(SyntaxFactory.EndOfLineTrivia(Environment.NewLine));
            return TriviaConverter.PortConvertedTrivia(node, convertedNode);
        }
    }
}