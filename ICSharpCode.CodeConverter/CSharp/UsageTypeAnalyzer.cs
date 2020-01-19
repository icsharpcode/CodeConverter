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
            var operationsReferencing = await references.SelectMany(r => r.Locations).SelectAsync(async l => {
                if (l.Location.SourceTree == outsideLocation?.SourceTree && l.Location.SourceSpan.OverlapsWith(outsideLocation.SourceSpan)) return null;
                var semanticModel = await l.Document.GetSemanticModelAsync();
                var syntaxRoot = await l.Document.GetSyntaxRootAsync();
                var syntaxNode = syntaxRoot.FindNode(l.Location.SourceSpan);
                return semanticModel.GetOperation(syntaxNode);
            });
            if (operationsReferencing.Any(IsWriteUsage)) return true;
            return false;
        }

        private static bool IsWriteUsage(IOperation operation)
        {
            return operation?.Parent is IAssignmentOperation a && a.Target == operation
                   || operation is IArgumentOperation ao && ao.Parameter.RefKind == RefKind.Ref;
        }
    }
}
