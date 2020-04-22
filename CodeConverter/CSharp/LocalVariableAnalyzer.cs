using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using ICSharpCode.CodeConverter.Util.FromRoslyn;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class LocalVariableAnalyzer
    {
        public static async Task<HashSet<ILocalSymbol>> GetDescendantsToInlineInLoopAsync(this Solution solution, SemanticModel semanticModel, VisualBasicSyntaxNode methodNode)
        {
            var forEachControlVariables = await methodNode.DescendantNodes().OfType<VBSyntax.ForEachBlockSyntax>().SelectAsync(forEach => GetLoopVariablesToInlineAsync(solution, semanticModel, forEach));
            return new HashSet<ILocalSymbol>(forEachControlVariables.Where(f => f != null), SymbolEquivalenceComparer.Instance);
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
}
