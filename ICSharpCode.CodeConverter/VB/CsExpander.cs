using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Shared;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.VB
{
    internal class CsExpander : ISyntaxExpander
    {
        public static ISyntaxExpander Instance { get; } = new CsExpander();

        public SyntaxNode TryExpandNode(SyntaxNode node, SyntaxNode root, SemanticModel semanticModel,
            Workspace workspace)
        {
            return Expander.TryExpandNode(node, semanticModel, workspace);
        }

        public bool ShouldExpandWithinNode(SyntaxNode node, SyntaxNode root, SemanticModel semanticModel)
        {
#if ReductionCodeIsFixed // Currently everything gets globally qualified by this, but the reducer can't reverse that
            return !ShouldExpandNode(semanticModel, node);
#endif
            return false;
        }

        public bool ShouldExpandNode(SyntaxNode node, SyntaxNode root, SemanticModel semanticModel)
        {
            return node is Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax;
        }
    }
}