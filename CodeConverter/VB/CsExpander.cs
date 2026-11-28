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
        return WithNativeIntegerKeywords(WithoutGlobalOverqualification(expandedNode));
    }

    /// <summary>
    /// Roslyn displays System.IntPtr/UIntPtr with the C# keywords nint/nuint (the same types since C# 11), so the expansion
    /// qualifies them as System.nint/System.nuint, which don't exist. Put the keyword back so the name still binds.
    /// </summary>
    private static SyntaxNode WithNativeIntegerKeywords(SyntaxNode expandedNode)
    {
        var qualifiedNativeIntegers = expandedNode.DescendantNodesAndSelf()
            .Where(n => n is QualifiedNameSyntax { Left: var left, Right: var right } && IsSystemNamespace(left) && IsNativeIntegerKeyword(right)
                        || n is MemberAccessExpressionSyntax { Expression: var expression, Name: var name } && IsSystemNamespace(expression) && IsNativeIntegerKeyword(name))
            .ToArray();
        if (!qualifiedNativeIntegers.Any()) return expandedNode;
        return expandedNode.ReplaceNodes(qualifiedNativeIntegers, (original, _) => {
            var keyword = original is QualifiedNameSyntax q ? q.Right : ((MemberAccessExpressionSyntax)original).Name;
            return keyword.WithTriviaFrom(original);
        });
    }

    private static bool IsNativeIntegerKeyword(SimpleNameSyntax name) => name is IdentifierNameSyntax { IsNint: true } or IdentifierNameSyntax { IsNuint: true };

    private static bool IsSystemNamespace(ExpressionSyntax expression) => expression switch {
        IdentifierNameSyntax id => id.Identifier.ValueText == nameof(System),
        AliasQualifiedNameSyntax alias => alias.Alias.IsGlobalId() && alias.Name.Identifier.ValueText == nameof(System),
        _ => false
    };

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