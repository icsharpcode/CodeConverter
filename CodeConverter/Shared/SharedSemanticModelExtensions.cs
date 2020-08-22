using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Shared
{
    internal static class SharedSemanticModelExtensions
    {

        public static T GetAncestorOperationOrNull<T>(this SemanticModel semanticModel, SyntaxNode node) where T:IOperation
        {
            for (var currentNode = node;
                currentNode != null;
                currentNode = currentNode.Parent) {
                if (semanticModel.GetOperation(currentNode) is T tOp) return tOp;
            }

            return default;
        }
    }
}
