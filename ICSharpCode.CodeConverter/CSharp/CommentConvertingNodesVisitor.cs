using System;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using VbSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using CsSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using SyntaxNodeExtensions = ICSharpCode.CodeConverter.Util.SyntaxNodeExtensions;

namespace ICSharpCode.CodeConverter.CSharp
{
    public class CommentConvertingNodesVisitor : VisualBasicSyntaxVisitor<Task<CSharpSyntaxNode>>
    {
        public TriviaConverter TriviaConverter { get; }
        private readonly VisualBasicSyntaxVisitor<Task<CSharpSyntaxNode>> _wrappedVisitor;

        public CommentConvertingNodesVisitor(VisualBasicSyntaxVisitor<Task<CSharpSyntaxNode>> wrappedVisitor, TriviaConverter triviaConverter)
        {
            TriviaConverter = triviaConverter;
            this._wrappedVisitor = wrappedVisitor;
        }
        public override async Task<CSharpSyntaxNode> DefaultVisit(SyntaxNode node)
        {
            return TriviaConverter.PortConvertedTrivia(node, await _wrappedVisitor.Visit(node));
        }

        public override async Task<CSharpSyntaxNode> VisitModuleBlock(VbSyntax.ModuleBlockSyntax node)
        {
            return await WithPortedTrivia<VbSyntax.TypeBlockSyntax, CsSyntax.BaseTypeDeclarationSyntax>(node, WithTypeBlockTrivia);
        }

        public override async Task<CSharpSyntaxNode> VisitStructureBlock(VbSyntax.StructureBlockSyntax node)
        {
            return await WithPortedTrivia<VbSyntax.TypeBlockSyntax, CsSyntax.BaseTypeDeclarationSyntax>(node, WithTypeBlockTrivia);
        }

        public override async Task<CSharpSyntaxNode> VisitInterfaceBlock(VbSyntax.InterfaceBlockSyntax node)
        {
            return await WithPortedTrivia<VbSyntax.TypeBlockSyntax, CsSyntax.BaseTypeDeclarationSyntax>(node, WithTypeBlockTrivia);
        }

        public override async Task<CSharpSyntaxNode> VisitClassBlock(VbSyntax.ClassBlockSyntax node)
        {
            return await WithPortedTrivia<VbSyntax.TypeBlockSyntax, CsSyntax.BaseTypeDeclarationSyntax>(node, WithTypeBlockTrivia);
        }

        public override async Task<CSharpSyntaxNode> VisitCompilationUnit(VbSyntax.CompilationUnitSyntax node)
        {
            var cSharpSyntaxNode = (CsSyntax.CompilationUnitSyntax) await base.VisitCompilationUnit(node);
            cSharpSyntaxNode = cSharpSyntaxNode.WithEndOfFileToken(
                cSharpSyntaxNode.EndOfFileToken.WithConvertedLeadingTriviaFrom(node.EndOfFileToken));

            return TriviaConverter.IsAllTriviaConverted()
                ? cSharpSyntaxNode
                : cSharpSyntaxNode.WithAppendedTrailingTrivia(SyntaxFactory.Comment("/* Some trivia (e.g. comments) could not be converted */"));
        }

        private async Task<TDest> WithPortedTrivia<TSource, TDest>(SyntaxNode node, Func<TSource, TDest, TDest> portExtraTrivia) where TSource : SyntaxNode where TDest : CSharpSyntaxNode
        {
            var cSharpSyntaxNode = portExtraTrivia((TSource)node, (TDest) await _wrappedVisitor.Visit(node));
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