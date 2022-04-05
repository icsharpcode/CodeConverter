﻿namespace ICSharpCode.CodeConverter.Common;

internal interface ISyntaxExpander
{
    bool ShouldExpandWithinNode(SyntaxNode node, SyntaxNode root, SemanticModel semanticModel);
    bool ShouldExpandNode(SyntaxNode node, SyntaxNode root, SemanticModel semanticModel);
    SyntaxNode ExpandNode(SyntaxNode node, SyntaxNode root, SemanticModel semanticModel, Workspace workspace);
}