using System;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace ICSharpCode.CodeConverter.CSharp
{
    public class CommentConvertingNodesVisitor : VisualBasicSyntaxVisitor<CSharpSyntaxNode>
    {
        private readonly VisualBasicSyntaxVisitor<CSharpSyntaxNode> wrappedVisitor;
        private static readonly SyntaxTrivia EndOfLine = SyntaxFactory.SyntaxTrivia(SyntaxKind.EndOfLineTrivia, Environment.NewLine);

        public CommentConvertingNodesVisitor(VisualBasicSyntaxVisitor<CSharpSyntaxNode> wrappedVisitor)
        {
            this.wrappedVisitor = wrappedVisitor;
        }
        public override CSharpSyntaxNode DefaultVisit(SyntaxNode node)
        {
            var cSharpSyntaxNode = wrappedVisitor.Visit(node);
            return cSharpSyntaxNode.WithConvertedTriviaFrom(node);
        }
    }
}