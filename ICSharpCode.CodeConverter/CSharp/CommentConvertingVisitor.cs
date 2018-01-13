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
    public class CommentConvertingVisitor : VisualBasicSyntaxVisitor<CSharpSyntaxNode>
    {
        private readonly VisualBasicSyntaxVisitor<CSharpSyntaxNode> wrappedVisitor;
        private static readonly SyntaxTrivia EndOfLine = SyntaxFactory.SyntaxTrivia(SyntaxKind.EndOfLineTrivia, Environment.NewLine);

        public CommentConvertingVisitor(VisualBasicSyntaxVisitor<CSharpSyntaxNode> wrappedVisitor)
        {
            this.wrappedVisitor = wrappedVisitor;
        }

        private CSharpSyntaxNode VisitWithAppendedBlankLine(SyntaxNode node)
        {
            return DefaultVisit(node).WithAppendedTrailingTrivia(EndOfLine, EndOfLine);
        }
        private CSharpSyntaxNode VisitWithAppendedEndOfLine(SyntaxNode node)
        {
            return DefaultVisit(node).WithAppendedTrailingTrivia(EndOfLine);
        }

        private CSharpSyntaxNode VisitWithAppendedBlankLineIfEndOfSection<T>(T node) where T : VisualBasicSyntaxNode
        {
            var nextIsSame = GetNextNode(node) is T;
            return nextIsSame ? VisitWithAppendedEndOfLine(node) : VisitWithAppendedBlankLine(node);
        }
        private static SyntaxNode GetNextNode(VisualBasicSyntaxNode node)
        {
            return node.GetAncestorsOrThis(x => true).Last().FindNode(new TextSpan(node.FullSpan.End + 1, 0));
        }

        public override CSharpSyntaxNode DefaultVisit(SyntaxNode node)
        {
            return wrappedVisitor.Visit(node).WithConvertedTriviaFrom(node);
        }

        public override CSharpSyntaxNode VisitImportsStatement(ImportsStatementSyntax node)
        {
            return VisitWithAppendedBlankLine(node);
        }

        public override CSharpSyntaxNode VisitSimpleImportsClause(SimpleImportsClauseSyntax node)
        {
            return VisitWithAppendedBlankLine(node);
        }

        public override CSharpSyntaxNode VisitImportAliasClause(ImportAliasClauseSyntax node)
        {
            return VisitWithAppendedBlankLine(node);
        }

        public override CSharpSyntaxNode VisitClassBlock(ClassBlockSyntax node)
        {
            return VisitWithAppendedBlankLine(node);
        }

        public override CSharpSyntaxNode VisitModuleBlock(ModuleBlockSyntax node)
        {
            return VisitWithAppendedBlankLine(node);
        }

        public override CSharpSyntaxNode VisitStructureBlock(StructureBlockSyntax node)
        {
            return VisitWithAppendedBlankLine(node);
        }

        public override CSharpSyntaxNode VisitInterfaceBlock(InterfaceBlockSyntax node)
        {
            return VisitWithAppendedBlankLine(node);
        }

        public override CSharpSyntaxNode VisitMethodBlock(MethodBlockSyntax node)
        {
            return VisitWithAppendedBlankLine(node);
        }

        public override CSharpSyntaxNode VisitMethodStatement(MethodStatementSyntax node)
        {
            var hasBody = node.Parent is MethodBlockSyntax;
            return hasBody ? DefaultVisit(node) : VisitWithAppendedBlankLine(node);
        }

        public override CSharpSyntaxNode VisitPropertyBlock(PropertyBlockSyntax node)
        {
            return VisitWithAppendedBlankLine(node);
        }

        public override CSharpSyntaxNode VisitPropertyStatement(PropertyStatementSyntax node)
        {
            var nextNode = GetNextNode(node);
            var nextIsPropertyWithNoBody = nextNode is PropertyStatementSyntax && !PropertyHasBody(nextNode);
            return nextIsPropertyWithNoBody ? VisitWithAppendedEndOfLine(node) : VisitWithAppendedBlankLine(node);
        }

        private static bool PropertyHasBody(SyntaxNode nextNode)
        {
            return nextNode.Parent is PropertyBlockSyntax;
        }

        public override CSharpSyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            return VisitWithAppendedBlankLineIfEndOfSection(node);
        }

        public override CSharpSyntaxNode VisitEventStatement(EventStatementSyntax node)
        {
            return VisitWithAppendedBlankLine(node);
        }
    }
}