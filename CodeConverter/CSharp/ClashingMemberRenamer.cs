using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using ICSharpCode.CodeConverter.Util.FromRoslyn;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class ClashingMemberRenamer
    {
        /// <summary>
        /// Renames symbols in a VB project so that they don't clash with rules for C# member names, attempting to rename the least public ones first.
        /// See https://github.com/icsharpcode/CodeConverter/issues/420
        /// </summary>
        public static async Task<Project> RenameClashingSymbolsAsync(Project project)
        {
            var compilation = await project.GetCompilationAsync();
            var memberRenames = SymbolRenamer.GetNamespacesAndTypesInAssembly(project, compilation)
                .SelectMany(x => GetSymbolsWithNewNames(x, compilation));
            return await SymbolRenamer.PerformRenamesAsync(project, memberRenames.ToList());
        }

        private static IEnumerable<(ISymbol Original, string NewName)> GetSymbolsWithNewNames(INamespaceOrTypeSymbol containerSymbol, Compilation compilation)
        {
            if (containerSymbol.IsNamespace) return Enumerable.Empty<(ISymbol Original, string NewName)>();

            var members = containerSymbol.GetMembers()
                .Where(m => m.Locations.Any(loc => loc.SourceTree != null && compilation.ContainsSyntaxTree(loc.SourceTree)))
                .Where(s => containerSymbol.Name == s.Name || containerSymbol is INamedTypeSymbol nt && nt.IsEnumType() && SymbolRenamer.GetName(s).StartsWith(containerSymbol.Name));
            var symbolSet = containerSymbol.Yield().Concat(members).ToArray();
            return SymbolRenamer.GetSymbolsWithNewNames(symbolSet, new HashSet<string>(symbolSet.Select(SymbolRenamer.GetName)), true);
        }
    }
}