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

        public CommentConvertingNodesVisitor(CSharpSyntaxVisitor<VisualBasicSyntaxNode> wrappedVisitor, BitArray lineTriviaMapped, SemanticModel semanticModel)
        {
            TriviaConverter = new TriviaConverter();
            _wrappedVisitor = wrappedVisitor;
            _lineTriviaMapped = lineTriviaMapped;
            _semanticModel = semanticModel;
        }

        public override VisualBasicSyntaxNode DefaultVisit(SyntaxNode node)
        {
            return DefaultVisitInner(node);
        }

        private VisualBasicSyntaxNode DefaultVisitInner(SyntaxNode node)
        {
            try {
                var converted = _wrappedVisitor.Visit(node);
                return converted.WithSourceMappingFrom(node);
            } catch (Exception e) {
                var dummyStatement = SyntaxFactory.EmptyStatement();
                return dummyStatement.WithVbTrailingErrorComment<VbSyntax.StatementSyntax>((CSharpSyntaxNode)node, e);
            }
        }

        public override VisualBasicSyntaxNode VisitAttributeList(CsSyntax.AttributeListSyntax node)
        {
            var convertedNode = DefaultVisitInner(node)
                .WithPrependedLeadingTrivia(SyntaxFactory.EndOfLineTrivia(Environment.NewLine));
            return convertedNode;
        }

        public override VisualBasicSyntaxNode VisitCompilationUnit(CsSyntax.CompilationUnitSyntax node)
        {
            var convertedNode = (VbSyntax.CompilationUnitSyntax)DefaultVisitInner(node);
            return convertedNode.WithEndOfFileToken(convertedNode.EndOfFileToken.WithSourceMappingFrom(node.EndOfFileToken));
        }

        public override VisualBasicSyntaxNode VisitNamespaceDeclaration(CsSyntax.NamespaceDeclarationSyntax node)
        {
            var convertedNode = (VbSyntax.NamespaceBlockSyntax)DefaultVisitInner(node);
            var blockStart = convertedNode.NamespaceStatement.GetLastToken();
            return convertedNode.ReplaceToken(blockStart, blockStart.WithSourceMappingFrom(node.OpenBraceToken));
        }

        public override VisualBasicSyntaxNode VisitClassDeclaration(CsSyntax.ClassDeclarationSyntax node)
        {
            var convertedNode = DefaultVisitInner(node);
            VbSyntax.StatementSyntax blockStmt = convertedNode is VbSyntax.ClassBlockSyntax cbs ? (VbSyntax.StatementSyntax) cbs.ClassStatement
                : convertedNode is VbSyntax.ModuleBlockSyntax mbs ? (VbSyntax.StatementSyntax) mbs.ModuleStatement
                : null;
            var endOfBlock = blockStmt?.GetLastToken();
            if (endOfBlock != null) {
                return convertedNode.ReplaceToken(endOfBlock.Value, endOfBlock.Value.WithSourceMappingFrom(node.OpenBraceToken));
            }
            return convertedNode;
        }

        public override VisualBasicSyntaxNode VisitStructDeclaration(CsSyntax.StructDeclarationSyntax node)
        {
            var convertedNode = (VbSyntax.StructureBlockSyntax)DefaultVisitInner(node);
            var blockStart = convertedNode.StructureStatement.GetLastToken();
            return convertedNode.ReplaceToken(blockStart, blockStart.WithSourceMappingFrom(node.OpenBraceToken));
        }

        public override VisualBasicSyntaxNode VisitEnumDeclaration(CsSyntax.EnumDeclarationSyntax node)
        {
            var convertedNode = (VbSyntax.EnumBlockSyntax)DefaultVisitInner(node);
            var blockStart = convertedNode.EnumStatement.GetLastToken();
            return convertedNode.ReplaceToken(blockStart, blockStart.WithSourceMappingFrom(node.OpenBraceToken));
        }
    }
}
