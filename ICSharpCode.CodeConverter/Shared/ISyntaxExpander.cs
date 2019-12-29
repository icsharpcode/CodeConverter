using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Shared
{
    internal interface ISyntaxExpander
    {
        Task<Document> WorkaroundBugsInExpandAsync(Document document);
        SyntaxNode TryExpandNode(SyntaxNode node, SyntaxNode root, SemanticModel semanticModel, Workspace workspace);
        bool ShouldExpandWithinNode(SemanticModel semanticModel, SyntaxNode node);
        bool ShouldExpandNode(SemanticModel semanticModel, SyntaxNode node);
    }
}