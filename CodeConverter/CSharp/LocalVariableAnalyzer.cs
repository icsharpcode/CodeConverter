using ICSharpCode.CodeConverter.Util.FromRoslyn;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.CSharp;

internal static class LocalVariableAnalyzer
{
    public static async Task<HashSet<ILocalSymbol>> GetDescendantsToInlineInLoopAsync(this Solution solution, SemanticModel semanticModel, VisualBasicSyntaxNode methodNode)
    {
        var forEachControlVariables = await methodNode.DescendantNodes().OfType<ForEachBlockSyntax>().SelectAsync(forEach => GetLoopVariablesToInlineAsync(solution, semanticModel, forEach));
#pragma warning disable RS1024 // Compare symbols correctly - analyzer bug, this is the comparer the docs recommend
        return new HashSet<ILocalSymbol>(forEachControlVariables.Where(f => f != null), SymbolEqualityComparer.IncludeNullability);
#pragma warning restore RS1024 // Compare symbols correctly
    }

    private static async Task<ILocalSymbol> GetLoopVariablesToInlineAsync(Solution solution, SemanticModel semanticModel, ForEachBlockSyntax block)
    {
        if (semanticModel.GetSymbolInfo(block.ForEachStatement.ControlVariable).Symbol is ILocalSymbol varSymbol) {
            var usagesOutsideLoop = await solution.GetUsagesAsync(varSymbol, block.GetLocation());
            if (!usagesOutsideLoop.Any()) return varSymbol;
        }
        return null;
    }
}