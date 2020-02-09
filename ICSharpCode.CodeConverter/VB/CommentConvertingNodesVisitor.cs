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
        private readonly CSharpSyntaxVisitor<VisualBasicSyntaxNode> _wrappedVisitor;

        public CommentConvertingNodesVisitor(CSharpSyntaxVisitor<VisualBasicSyntaxNode> wrappedVisitor)
        {
            _wrappedVisitor = wrappedVisitor;
        }

        public override VisualBasicSyntaxNode DefaultVisit(SyntaxNode node)
        {
            return DefaultVisitInner(node);
        }

        private VisualBasicSyntaxNode DefaultVisitInner(SyntaxNode node)
        {
            return _wrappedVisitor.Visit(node);
        }

        public override VisualBasicSyntaxNode VisitAttributeList(CsSyntax.AttributeListSyntax node)
        {
            return DefaultVisitInner(node).WithPrependedLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed);
        }

        public override VisualBasicSyntaxNode VisitCompilationUnit(CsSyntax.CompilationUnitSyntax node)
        {
            var convertedNode = (VbSyntax.CompilationUnitSyntax)DefaultVisitInner(node);
            // Special case where we need to map manually because it's a special zero-width token that just has leading trivia that isn't at the start of the line necessarily
            return convertedNode.WithEndOfFileToken(
                convertedNode.EndOfFileToken.WithSourceMappingFrom(node.EndOfFileToken)
            );
        }
    }
}
