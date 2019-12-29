using System.Threading.Tasks;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Shared;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.VB
{
    internal class CsExpander : ISyntaxExpander
    {
        public static ISyntaxExpander Instance { get; } = new CsExpander();

        public async Task<Document> WorkaroundBugsInExpandAsync(Document document)
        {
            return document;
        }

        public SyntaxNode TryExpandNode(SyntaxNode node, SemanticModel semanticModel, Workspace workspace)
        {
            return Expander.TryExpandNode(node, semanticModel, workspace);
        }

        public bool ShouldExpandWithinNode(SemanticModel semanticModel, SyntaxNode node)
        {
#if ReductionCodeIsFixed // Currently everything gets globally qualified by this, but the reducer can't reverse that
            return !ShouldExpandNode(semanticModel, node);
#endif
            return false;
        }

        public bool ShouldExpandNode(SemanticModel semanticModel, SyntaxNode node)
        {
            return node is Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax;
        }
    }
}