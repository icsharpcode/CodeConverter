using System;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace ICSharpCode.CodeConverter.CSharp
{
    public class CommentConvertingNodesVisitor : VisualBasicSyntaxVisitor<CSharpSyntaxNode>
    {
        public TriviaConverter TriviaConverter { get; }
        private readonly VisualBasicSyntaxVisitor<CSharpSyntaxNode> wrappedVisitor;

        public CommentConvertingNodesVisitor(VisualBasicSyntaxVisitor<CSharpSyntaxNode> wrappedVisitor)
        {
            TriviaConverter = new TriviaConverter();
            this.wrappedVisitor = wrappedVisitor;
        }
        public override CSharpSyntaxNode DefaultVisit(SyntaxNode node)
        {
            var cSharpSyntaxNode = wrappedVisitor.Visit(node);
            if (node is TypeStatementSyntax) {
                return cSharpSyntaxNode;
            }

            if (node is TypeBlockSyntax typeBlockVbNode && cSharpSyntaxNode is BaseTypeDeclarationSyntax btCsNode) {
                var beforeOpenBrace = btCsNode.OpenBraceToken.GetPreviousToken();
                cSharpSyntaxNode = cSharpSyntaxNode.ReplaceToken(beforeOpenBrace,
                    beforeOpenBrace.WithConvertedTriviaFrom(typeBlockVbNode.BlockStatement));
            }
            return TriviaConverter.PortConvertedTrivia(node, cSharpSyntaxNode);
        }
    }
}