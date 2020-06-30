using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Rename;

namespace ICSharpCode.CodeConverter.Shared
{
    internal static class SymbolRenamer
    {
        public static IEnumerable<(ISymbol Original, string NewName)> GetSymbolsWithNewNames(
            IEnumerable<ISymbol> toRename, Func<string, bool> canUse, bool canKeepOne)
        {
            var symbolsWithNewNames = toRename.OrderByDescending(x => x.DeclaredAccessibility).ThenByDescending(x => x.Kind == SymbolKind.Parameter || x.Kind == SymbolKind.Property).Skip(canKeepOne ? 1 :0).Select(tr =>
            {
                string newName = NameGenerator.GenerateUniqueName(GetBaseForNewName(tr), canUse);
                return (Original: tr, NewName: newName);
            });
            return symbolsWithNewNames;
        }

        public static string GetName(ISymbol m) {
            if (m.CanBeReferencedByName)
                return m.Name;
            if (m.ExplicitInterfaceImplementations().Any())
                return m.Name.Split('.').Last();
            return m.Name;
        }

        public static async Task<Project> PerformRenamesAsync(Project project, IReadOnlyCollection<(ISymbol Original, string NewName)> symbolsWithNewNames)
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

        private static string GetBaseForNewName(ISymbol declaration)
        {
            string name = GetName(declaration);
            return declaration.Kind switch {
                SymbolKind.Method => name + "Method",
                SymbolKind.Property => name + "Prop",
                SymbolKind.NamedType => name + "Type",
                SymbolKind.Field => name + "Field",
                _ =>  declaration.Kind.ToString().ToLowerInvariant()[0] + name.Substring(0, 1).ToUpperInvariant() + name.Substring(1)
            };
        }

        public static IEnumerable<INamespaceOrTypeSymbol> GetNamespacesAndTypesInAssembly(Project project, Compilation compilation)
        {
            return compilation.GlobalNamespace.FollowProperty((INamespaceOrTypeSymbol n) => n.GetMembers().OfType<INamespaceOrTypeSymbol>().Where(s => s.IsDefinedInSource() && s?.ContainingAssembly?.Name == project.AssemblyName));
        }

        public static IEnumerable<(ISymbol Original, string NewName)> GetSymbolsWithNewNames(IReadOnlyCollection<ISymbol> symbolGroup, HashSet<string> names, bool caseSensitive)
        {
            var canRename = symbolGroup.Where(s => s.IsDefinedInSource() && s.CanBeReferencedByName).ToArray();
            var specialSymbolUsingName = canRename.Length < symbolGroup.Count;
            var methodSymbols = canRename.OfType<IMethodSymbol>().ToArray();
            var canKeepOneNormalMemberName = !specialSymbolUsingName && !methodSymbols.Any();
            symbolGroup = canRename.Except(methodSymbols).ToArray();
            (ISymbol Original, string NewName)[] methodsWithNewNames = GetMethodSymbolsWithNewNames(methodSymbols.ToArray(), names, specialSymbolUsingName, caseSensitive);
            return GetSymbolsWithNewNames(symbolGroup, names.Add, canKeepOneNormalMemberName).Concat(methodsWithNewNames);
        }

        private static (ISymbol Original, string NewName)[] GetMethodSymbolsWithNewNames(IMethodSymbol[] methodSymbols,
            HashSet<string> names,
            bool specialSymbolUsingName, bool caseSensitive)
        {
            var stringComparer = caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
            var methodsBySignature = methodSymbols
                .ToLookup(m => m.GetUnqualifiedMethodSignature(caseSensitive))
                .Where(g => g.Count() > 1)
                .SelectMany(clashingMethodGroup =>
                {
                    var thisMethodGroupNames = new HashSet<string>(stringComparer);
                    var symbolsWithNewNames = GetSymbolsWithNewNames(clashingMethodGroup,
                        n => !names.Contains(n) && thisMethodGroupNames.Add(n),
                        !specialSymbolUsingName).ToArray();
                    return symbolsWithNewNames;
                }).ToArray();

            foreach (var newMethodNames in methodsBySignature.Select(m => m.NewName))
            {
                names.Add(newMethodNames);
            }

            return methodsBySignature;
        }
    }
}