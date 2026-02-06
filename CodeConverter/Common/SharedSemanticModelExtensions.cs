namespace ICSharpCode.CodeConverter.Common;

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

    public static ISymbol[] GetAllCandidateSymbols(this SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken = default(CancellationToken))
    {
        var symbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken);
        return [.. symbolInfo.Symbol.YieldNotNull(), .. symbolInfo.CandidateSymbols];
    }
}