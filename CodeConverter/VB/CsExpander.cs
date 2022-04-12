using ICSharpCode.CodeConverter.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace ICSharpCode.CodeConverter.VB;

internal class CsExpander : ISyntaxExpander
{
    public static ISyntaxExpander Instance { get; } = new CsExpander();

    public SyntaxNode ExpandNode(SyntaxNode node, SemanticModel semanticModel,
        Workspace workspace)
    {
        SyntaxNode expandedNode = Simplifier.Expand(node, semanticModel, workspace);
        return WithoutGlobalOverqualification(expandedNode);
    }

    /// <summary>
    /// The VB reduction step doesn't seem to reduce things qualified with global, so don't add it anywhere it isn't already
    /// </summary>
    private static SyntaxNode WithoutGlobalOverqualification(SyntaxNode expandedNode)
    {
        var aliasNodes = expandedNode.GetAnnotatedNodes(Simplifier.Annotation).Select(syntaxNode =>
            LeftMostDescendant(syntaxNode).Parent).OfType<AliasQualifiedNameSyntax>().Where(n => n.Alias.IsGlobalId()).ToArray();
        if (aliasNodes.Any()) {
            return expandedNode.ReplaceNodes(aliasNodes, (_, rewrite) => rewrite.Name.WithLeadingTrivia(rewrite.GetLeadingTrivia()));
        }
        return expandedNode;
    }

    public bool ShouldExpandWithinNode(SyntaxNode node, SemanticModel semanticModel)
    {
        return !ShouldExpandNode(node, semanticModel);
    }

    public bool ShouldExpandNode(SyntaxNode node, SemanticModel semanticModel)
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