using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Rename;

namespace ICSharpCode.CodeConverter.VB
{
    internal static class CaseConflictResolver
    {
        /// <summary>
        /// Renames symbols in a CSharp project so that they don't clash on case within the same named scope, attempting to rename the least public ones first.
        /// This is because C# is case sensitive but VB is case insensitive.
        /// </summary>
        /// <remarks>
        /// Cases in different named scopes should be dealt with by <seealso cref="DocumentExtensions.ExpandVbAsync"/>.
        /// For names scoped within a type member, see <seealso cref="GetCsLocalSymbolsPerScope"/>.
        /// </remarks>
        public static async Task<Project> RenameClashingSymbolsAsync(Project project)
        {
            var compilation = await project.GetCompilationAsync();
            var memberRenames = compilation.GlobalNamespace.FollowProperty((INamespaceOrTypeSymbol n) => n.GetMembers().OfType<INamespaceOrTypeSymbol>().Where(s => s.IsDefinedInSource()))
                .SelectMany(x => GetSymbolsWithNewNames(x, compilation));
            return await PerformRenamesAsync(project, memberRenames.ToList());
        }

        private static IEnumerable<(ISymbol Original, string NewName)> GetSymbolsWithNewNames(INamespaceOrTypeSymbol containerSymbol, Compilation compilation)
        {
            var members = containerSymbol.GetMembers().Where(m => m.Locations.Any(loc => compilation.ContainsSyntaxTree(loc.SourceTree))).ToArray();
            var symbolSets = GetLocalSymbolSets(containerSymbol, compilation, members).Concat(members.AsEnumerable().Yield());
            return symbolSets.SelectMany(GetUniqueNamesForSymbolSet);
        }

        public static IEnumerable<IEnumerable<ISymbol>> GetLocalSymbolSets(INamespaceOrTypeSymbol containerSymbol, Compilation compilation, IReadOnlyCollection<ISymbol> members)
        {
            if (!(containerSymbol is ITypeSymbol)) return Enumerable.Empty<IEnumerable<ISymbol>>();

            var semanticModels = containerSymbol.Locations.Select(loc => loc.SourceTree).Distinct()
                .Where(sourceTree => compilation.ContainsSyntaxTree(sourceTree))
                .Select(sourceTree => compilation.GetSemanticModel(sourceTree, true));
            return semanticModels.SelectMany(semanticModel => members.SelectMany(m => semanticModel.GetCsSymbolsPerScope(m)));
        }

        private static IEnumerable<(ISymbol Original, string NewName)> GetUniqueNamesForSymbolSet(IEnumerable<ISymbol> symbols) {
            var membersByCaseInsensitiveName = symbols.ToLookup(m => GetName(m), m => m, StringComparer.OrdinalIgnoreCase);
            var names = new HashSet<string>(membersByCaseInsensitiveName.Select(ms => ms.Key),
                StringComparer.OrdinalIgnoreCase);
            var symbolsWithNewNames = membersByCaseInsensitiveName.Where(ms => ms.Count() > 1)
                .SelectMany(symbolGroup => GetSymbolsWithNewNames(symbolGroup.ToArray(), names));
            return symbolsWithNewNames;
        }
        private static string GetName(ISymbol m) {
            if (m.CanBeReferencedByName)
                return m.Name;
            if (m.ExplicitInterfaceImplementations().Any())
                return m.Name.Split('.').Last();
            return m.Name;
        }

        private static IEnumerable<(ISymbol Original, string NewName)> GetSymbolsWithNewNames(IReadOnlyCollection<ISymbol> symbolGroup, HashSet<string> names)
        {
            var canRename = symbolGroup.Where(s => s.IsDefinedInSource() && s.CanBeReferencedByName).ToArray();
            var specialSymbolUsingName = canRename.Length < symbolGroup.Count;
            var methodSymbols = canRename.OfType<IMethodSymbol>().ToArray();
            var canKeepOneNormalMemberName = !specialSymbolUsingName && !methodSymbols.Any();
            symbolGroup = canRename.Except(methodSymbols).ToArray();
            (ISymbol Original, string NewName)[] methodsWithNewNames = GetMethodSymbolsWithNewNames(methodSymbols.ToArray(), names, specialSymbolUsingName);
            return GetSymbolsWithNewNames(symbolGroup, names.Add, canKeepOneNormalMemberName).Concat(methodsWithNewNames);
        }

        private static (ISymbol Original, string NewName)[] GetMethodSymbolsWithNewNames(IMethodSymbol[] methodSymbols,
            HashSet<string> names,
            bool specialSymbolUsingName)
        {
            var methodsByCaseInsensitiveSignature = methodSymbols
                .ToLookup(m => m.GetUnqualifiedMethodSignature(false))
                .Where(g => g.Count() > 1)
                .SelectMany(clashingMethodGroup =>
                {
                    var thisMethodGroupNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var symbolsWithNewNames = GetSymbolsWithNewNames(clashingMethodGroup,
                        n => !names.Contains(n) && thisMethodGroupNames.Add(n),
                        !specialSymbolUsingName).ToArray();
                    return symbolsWithNewNames;
                }).ToArray();

            foreach (var newMethodNames in methodsByCaseInsensitiveSignature.Select(m => m.NewName))
            {
                names.Add(newMethodNames);
            }

            return methodsByCaseInsensitiveSignature;
        }

        private static IEnumerable<(ISymbol Original, string NewName)> GetSymbolsWithNewNames(
            IEnumerable<ISymbol> toRename, Func<string, bool> canUse, bool canKeepOne)
        {
            var symbolsWithNewNames = toRename.OrderByDescending(x => x.DeclaredAccessibility).ThenByDescending(x => x.Kind == SymbolKind.Parameter || x.Kind == SymbolKind.Property).Skip(canKeepOne ? 1 :0).Select(tr =>
            {
                string newName = NameGenerator.GenerateUniqueName(GetBaseName(tr), canUse);
                return (Original: tr, NewName: newName);
            });
            return symbolsWithNewNames;
        }

        private static async Task<Project> PerformRenamesAsync(Project project, IReadOnlyCollection<(ISymbol Original, string NewName)> symbolsWithNewNames)
        {
            var solution = project.Solution;
            foreach (var (originalSymbol, newName) in symbolsWithNewNames) {
                project = solution.GetProject(project.Id);
                var compilation = await project.GetCompilationAsync();
                ISymbol currentDeclaration = SymbolFinder.FindSimilarSymbols(originalSymbol, compilation).FirstOrDefault();
                if (currentDeclaration == null)
                    continue; //Must have already renamed this symbol for a different reason
                solution = await Renamer.RenameSymbolAsync(solution, currentDeclaration, newName, solution.Workspace.Options);
            }

            return solution.GetProject(project.Id);
        }

        private static string GetBaseName(ISymbol declaration)
        {
            string prefix = declaration.Kind.ToString().ToLowerInvariant()[0] + "_";
            string name = GetName(declaration);
            return prefix + name.Substring(0, 1).ToUpperInvariant() + name.Substring(1);
        }
    }
}