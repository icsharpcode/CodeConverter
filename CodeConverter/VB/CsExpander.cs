using System.Linq;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace ICSharpCode.CodeConverter.VB
{
    internal class CsExpander : ISyntaxExpander
    {
        public static ISyntaxExpander Instance { get; } = new CsExpander();

        public SyntaxNode ExpandNode(SyntaxNode node, SyntaxNode root, SemanticModel semanticModel,
            Workspace workspace)
        {
            SyntaxNode expandedNode = Simplifier.Expand(node, semanticModel, workspace);
            return WithoutGlobalOverqualification(node, expandedNode);
        }

        /// <summary>
        /// The VB reduction step doesn't seem to reduce things qualified with global, so don't add it anywhere it isn't already
        /// </summary>
        private SyntaxNode WithoutGlobalOverqualification(SyntaxNode node, SyntaxNode expandedNode)
        {
            var aliasNodes = expandedNode.GetAnnotatedNodes(Simplifier.Annotation).Select(syntaxNode =>
                LeftMostDescendant(syntaxNode).Parent).OfType<AliasQualifiedNameSyntax>().Where(n => n.Alias.IsGlobalId()).ToArray();
            if (aliasNodes.Any()) {
                return expandedNode.ReplaceNodes(aliasNodes, (orig, rewrite) => rewrite.Name.WithLeadingTrivia(rewrite.GetLeadingTrivia()));
            }
            return expandedNode;
        }

        public bool ShouldExpandWithinNode(SyntaxNode node, SyntaxNode root, SemanticModel semanticModel)
        {
            return !ShouldExpandNode(node, root, semanticModel);
        }

        public bool ShouldExpandNode(SyntaxNode node, SyntaxNode root, SemanticModel semanticModel)
        {
            return (node is NameSyntax || node is MemberAccessExpressionSyntax) && !IsOriginalSymbolGenericMethod(semanticModel, node);
        }

        private static SyntaxNode LeftMostDescendant(SyntaxNode expandedNode)
        {
            var id = expandedNode.FollowProperty(n => n.ChildNodes().FirstOrDefault()).LastOrDefault();
            return id;
        }

        private static bool IsOriginalSymbolGenericMethod(SemanticModel semanticModel, SyntaxNode node)
        {
            return semanticModel.GetSymbolInfo(node).Symbol.IsGenericMethod();
        }
    }
}