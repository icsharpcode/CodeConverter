using System;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using VbSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using CsSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;

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
            return TriviaConverter.PortConvertedTrivia(node, wrappedVisitor.Visit(node));
        }

        public override CSharpSyntaxNode VisitModuleBlock(VbSyntax.ModuleBlockSyntax node)
        {
            return WithPortedTrivia<VbSyntax.TypeBlockSyntax, CsSyntax.BaseTypeDeclarationSyntax>(node, WithTypeBlockTrivia);
        }

        public override CSharpSyntaxNode VisitStructureBlock(VbSyntax.StructureBlockSyntax node)
        {
            return WithPortedTrivia<VbSyntax.TypeBlockSyntax, CsSyntax.BaseTypeDeclarationSyntax>(node, WithTypeBlockTrivia);
        }

        public override CSharpSyntaxNode VisitInterfaceBlock(VbSyntax.InterfaceBlockSyntax node)
        {
            return WithPortedTrivia<VbSyntax.TypeBlockSyntax, CsSyntax.BaseTypeDeclarationSyntax>(node, WithTypeBlockTrivia);
        }

        public override CSharpSyntaxNode VisitClassBlock(VbSyntax.ClassBlockSyntax node)
        {
            return WithPortedTrivia<VbSyntax.TypeBlockSyntax, CsSyntax.BaseTypeDeclarationSyntax>(node, WithTypeBlockTrivia);
        }

        public override CSharpSyntaxNode VisitSingleLineLambdaExpression(VbSyntax.SingleLineLambdaExpressionSyntax node)
        {
            return WithPortedTrivia<VbSyntax.LambdaExpressionSyntax, CsSyntax.LambdaExpressionSyntax>(node, PortSubOrFunctionHeaderTrailingTrivia);
        }

        public override CSharpSyntaxNode VisitMultiLineLambdaExpression(VbSyntax.MultiLineLambdaExpressionSyntax node)
        {
            return WithPortedTrivia<VbSyntax.LambdaExpressionSyntax, CsSyntax.LambdaExpressionSyntax>(node, PortSubOrFunctionHeaderTrailingTrivia);
        }

        private TDest WithPortedTrivia<TSource, TDest>(SyntaxNode node, Func<TSource, TDest, TDest> portExtraTrivia) where TSource : SyntaxNode where TDest : CSharpSyntaxNode
        {
            var cSharpSyntaxNode = portExtraTrivia((TSource)node, (TDest)wrappedVisitor.Visit(node));
            return TriviaConverter.PortConvertedTrivia(node, cSharpSyntaxNode);
        }

        private static CsSyntax.BaseTypeDeclarationSyntax WithTypeBlockTrivia(VbSyntax.TypeBlockSyntax typeBlockVbNode, CsSyntax.BaseTypeDeclarationSyntax btCsNode)
        {
                var beforeOpenBrace = btCsNode.OpenBraceToken.GetPreviousToken();
                return btCsNode.ReplaceToken(beforeOpenBrace,
                    beforeOpenBrace
                        .WithConvertedTrailingTriviaFrom(typeBlockVbNode.BlockStatement)
                        .WithConvertedTrailingTriviaFrom(typeBlockVbNode.Inherits.LastOrDefault())
                        .WithConvertedTrailingTriviaFrom(typeBlockVbNode.Implements.LastOrDefault()));
        }

        private static CsSyntax.LambdaExpressionSyntax PortSubOrFunctionHeaderTrailingTrivia(VbSyntax.LambdaExpressionSyntax node, CsSyntax.LambdaExpressionSyntax csLambda)
        {
            var csLambdaArrowToken = csLambda.ArrowToken;
            var withConvertedTrailingTriviaFrom = csLambdaArrowToken.WithConvertedTrailingTriviaFrom(node.SubOrFunctionHeader.ParameterList.CloseParenToken);
            return csLambda.ReplaceToken(csLambdaArrowToken, withConvertedTrailingTriviaFrom);
        }
    }
}