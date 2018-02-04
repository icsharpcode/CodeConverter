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

        public override CSharpSyntaxNode VisitCompilationUnit(VbSyntax.CompilationUnitSyntax node)
        {
            var cSharpSyntaxNode = base.VisitCompilationUnit(node);
            TriviaConverter.ThrowIfPortsMissed(node, cSharpSyntaxNode);
            return cSharpSyntaxNode;
        }

        private TDest WithPortedTrivia<TSource, TDest>(SyntaxNode node, Func<TSource, TDest, TDest> portExtraTrivia) where TSource : SyntaxNode where TDest : CSharpSyntaxNode
        {
            var cSharpSyntaxNode = portExtraTrivia((TSource)node, (TDest)wrappedVisitor.Visit(node));
            return TriviaConverter.PortConvertedTrivia(node, cSharpSyntaxNode);
        }

        private CsSyntax.BaseTypeDeclarationSyntax WithTypeBlockTrivia(VbSyntax.TypeBlockSyntax sourceNode, CsSyntax.BaseTypeDeclarationSyntax destNode)
        {
            var beforeOpenBrace = destNode.OpenBraceToken.GetPreviousToken();
            var withAnnotation = TriviaConverter.WithDelegateToParentAnnotation(sourceNode.BlockStatement, beforeOpenBrace);
            withAnnotation = TriviaConverter.WithDelegateToParentAnnotation(sourceNode.Inherits, withAnnotation);
            withAnnotation = TriviaConverter.WithDelegateToParentAnnotation(sourceNode.Implements, withAnnotation);
            return destNode.ReplaceToken(beforeOpenBrace, withAnnotation);
        }
    }
}