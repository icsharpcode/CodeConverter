using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.VB
{
    internal static class ClashingMemberRenamer
    {
        /// <summary>
        /// Renames symbols in a CSharp project so that they don't clash on case within the same named scope, attempting to rename the least public ones first.
        /// This is because C# is case sensitive but VB is case insensitive.
        /// </summary>
        /// <remarks>
        /// Cases in different named scopes should be dealt with by <seealso cref="DocumentExtensions.ExpandAsync"/>.
        /// For names scoped within a type member, see <seealso cref="SemanticModelSymbolSetExtensions.GetCsLocalSymbolsPerScope"/>.
        /// </remarks>
        public static async Task<Project> RenameClashingSymbolsAsync(Project project)
        {
            var compilation = await project.GetCompilationAsync();
            var memberRenames = SymbolRenamer.GetNamespacesAndTypesInAssembly(project, compilation)
                .SelectMany(x => GetSymbolsWithNewNames(x, compilation));
            return await SymbolRenamer.PerformRenamesAsync(project, memberRenames.ToList());
        }

        private static IEnumerable<(ISymbol Original, string NewName)> GetSymbolsWithNewNames(INamespaceOrTypeSymbol containerSymbol, Compilation compilation)
        {
            var members = containerSymbol.GetMembers()
                .Where(m => m.Locations.Any(loc => loc.SourceTree != null && compilation.ContainsSyntaxTree(loc.SourceTree))).ToArray();
            var symbolSets = GetLocalSymbolSets(containerSymbol, compilation, members).Concat(members.AsEnumerable().Yield());
            return symbolSets.SelectMany(GetUniqueNamesForSymbolSet);
        }

        public static IEnumerable<IEnumerable<ISymbol>> GetLocalSymbolSets(INamespaceOrTypeSymbol containerSymbol, Compilation compilation, IReadOnlyCollection<ISymbol> members)
        {
            if (!(containerSymbol is ITypeSymbol)) return Enumerable.Empty<IEnumerable<ISymbol>>();

            var semanticModels = containerSymbol.Locations.Select(loc => loc.SourceTree).Distinct()
                .Where(compilation.ContainsSyntaxTree)
                .Select(sourceTree => compilation.GetSemanticModel(sourceTree, true));
            return semanticModels.SelectMany(semanticModel => members.SelectMany(semanticModel.GetCsSymbolsPerScope));
        }

        private static IEnumerable<(ISymbol Original, string NewName)> GetUniqueNamesForSymbolSet(IEnumerable<ISymbol> symbols) {
            var membersByCaseInsensitiveName = symbols.ToLookup(SymbolRenamer.GetName, m => m, StringComparer.OrdinalIgnoreCase);
            var names = new HashSet<string>(membersByCaseInsensitiveName.Select(ms => ms.Key),
                StringComparer.OrdinalIgnoreCase);
            var symbolsWithNewNames = membersByCaseInsensitiveName.Where(ms => ms.Count() > 1)
                .SelectMany(symbolGroup => SymbolRenamer.GetSymbolsWithNewNames(symbolGroup.ToArray(), names, false));
            return symbolsWithNewNames;
        }
    }
}