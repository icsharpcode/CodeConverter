using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Simplification;

namespace ICSharpCode.CodeConverter.Shared
{
    internal class Expander
    {
        public static SyntaxNode TryExpandNode(SyntaxNode node, SemanticModel semanticModel, Workspace workspace)
        {
            try {
                return Simplifier.Expand(node, semanticModel, workspace);
            } catch (Exception) {
                return node;
            }
        }
    }
}