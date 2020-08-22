using System.Collections.Generic;
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
        public static async Task<bool> IsNeverWrittenAsync(this Solution solution, ISymbol symbol, Location outsideLocation = null)
        {
            return symbol.AllWriteUsagesKnowable() && !await ContainsWriteUsagesForAsync(solution, symbol, outsideLocation);
        }

        public static async Task<bool> ContainsWriteUsagesForAsync(Solution solution, ISymbol symbol, Location outsideLocation = null)
        {
            var references = await GetUsagesAsync(solution, symbol, outsideLocation);
            var operationsReferencing = references.Select(async g => {
                var semanticModel = await g.Doc.GetSemanticModelAsync();
                var syntaxRoot = await g.Doc.GetSyntaxRootAsync();
                return g.Usages.Select(l => syntaxRoot.FindNode(l.Location.SourceSpan))
                        .Select(syntaxNode => semanticModel.GetOperation(syntaxNode));
            });
            foreach (var documentUsages in operationsReferencing) {
                var usages = await documentUsages;
                if (usages.Any(IsWriteUsage)) return true;
            }
            return false;
        }

        public static async Task<IEnumerable<(Document Doc, ReferenceLocation[] Usages)>> GetUsagesAsync(this Solution solution, ISymbol symbol, Location outsideLocation = null)
        {
            var references = await SymbolFinder.FindReferencesAsync(symbol, solution);
            return references.SelectMany(r => r.Locations).GroupBy(l => (Doc: l.Document, Tree: l.Location.SourceTree))
                .Select(g => (Doc: g.Key.Doc, Usages: g.Where(l => l.Location.SourceTree != outsideLocation?.SourceTree || !l.Location.SourceSpan.OverlapsWith(outsideLocation.SourceSpan)).ToArray()))
                .Where(g => g.Usages.Any());
        }

        private static bool IsWriteUsage(IOperation operation)
        {
            return operation?.Parent is IAssignmentOperation a && a.Target == operation
                   || operation is IArgumentOperation ao && ao.Parameter.RefKind == RefKind.Ref;
        }
    }
}
