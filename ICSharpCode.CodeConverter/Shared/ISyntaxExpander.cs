using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Shared
{
    internal interface ISyntaxExpander
    {
        SyntaxNode TryExpandNode(SyntaxNode node, SyntaxNode root, SemanticModel semanticModel, Workspace workspace);
        bool ShouldExpandWithinNode(SemanticModel semanticModel, SyntaxNode node);
        bool ShouldExpandNode(SemanticModel semanticModel, SyntaxNode node);
    }
}