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
            return TriviaConverter.PortConvertedTrivia(node, _wrappedVisitor.Visit(node));
        }
    }
}