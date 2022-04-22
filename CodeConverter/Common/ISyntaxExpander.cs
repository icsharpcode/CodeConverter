namespace ICSharpCode.CodeConverter.Common;

internal interface ISyntaxExpander
{
    bool ShouldExpandWithinNode(SyntaxNode node, SemanticModel semanticModel);
    bool ShouldExpandNode(SyntaxNode node, SemanticModel semanticModel);
    SyntaxNode ExpandNode(SyntaxNode node, SemanticModel semanticModel, Workspace workspace);
}