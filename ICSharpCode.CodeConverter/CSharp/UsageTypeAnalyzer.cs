using System;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Operations;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class UsageTypeAnalyzer
    {
        public static async Task<bool> IsNeverWritten(this Solution solution, ISymbol symbol, Location outsideLocation = null)
        {
            return symbol.AllWriteUsagesKnowable() && !await ContainsWriteUsagesFor(solution, symbol, outsideLocation);
        }

        public static async Task<bool> ContainsWriteUsagesFor(Solution solution, ISymbol symbol, Location outsideLocation = null)
        {
            var references = await SymbolFinder.FindReferencesAsync(symbol, solution);
            var operationsReferencing = references.SelectMany(r => r.Locations).GroupBy(l => (Doc: l.Document, Tree: l.Location.SourceTree)).Select(async g => {
                var document = g.Key.Doc;
                var locations = g.Where(l => l.Location.SourceTree != outsideLocation?.SourceTree || !l.Location.SourceSpan.OverlapsWith(outsideLocation.SourceSpan)).ToArray();
                if (locations.Length == 0) return Enumerable.Empty<IOperation>();

                var semanticModel = await document.GetSemanticModelAsync();
                var syntaxRoot = await document.GetSyntaxRootAsync();
                return g.Select(l => syntaxRoot.FindNode(l.Location.SourceSpan))
                        .Select(syntaxNode => semanticModel.GetOperation(syntaxNode));
            });
            foreach (var documentUsages in operationsReferencing) {
                var usages = await documentUsages;
                if (usages.Any(IsWriteUsage)) return true;
            }
            return false;
        }

        private static bool IsWriteUsage(IOperation operation)
        {
            return operation?.Parent is IAssignmentOperation a && a.Target == operation
                   || operation is IArgumentOperation ao && ao.Parameter.RefKind == RefKind.Ref;
        }
    }
}
