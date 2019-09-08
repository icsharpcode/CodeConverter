using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Operations;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class UsageTypeAnalyzer
    {
        public static async Task<bool?> HasWriteUsagesAsync(this Solution solution, ISymbol symbol)
        {
            var references = await SymbolFinder.FindReferencesAsync(symbol, solution);
            var operationsReferencingAsync = references.SelectMany(r => r.Locations).Select(async l => {
                var semanticModel = await l.Document.GetSemanticModelAsync();
                var syntaxRoot = await l.Document.GetSyntaxRootAsync();
                var syntaxNode = syntaxRoot.FindNode(l.Location.SourceSpan);
                return semanticModel.GetOperation(syntaxNode);
            });
            var operationsReferencing = await Task.WhenAll(operationsReferencingAsync);
            if (operationsReferencing.Any(IsWriteUsage)) return true;
            if (symbol.GetResultantVisibility() == SymbolVisibility.Public) return null;
            return false;
        }

        private static bool IsWriteUsage(IOperation operation)
        {
            return operation.Parent is IAssignmentOperation a && a.Target == operation
                   || operation is IParameterReferenceOperation pro && pro.Parameter.RefKind == RefKind.Ref;
        }
    }
}