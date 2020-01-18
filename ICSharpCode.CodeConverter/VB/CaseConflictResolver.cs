using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Rename;
using CS = Microsoft.CodeAnalysis.CSharp;
using CSS = Microsoft.CodeAnalysis.CSharp.Syntax;

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
        public static async Task<Project> RenameClashingSymbols(Project project)
        {
            var compilation = await project.GetCompilationAsync();
            var memberRenames = compilation.GlobalNamespace.FollowProperty((INamespaceOrTypeSymbol n) => n.GetMembers().OfType<INamespaceOrTypeSymbol>().Where(s => s.IsDefinedInSource()))
                .SelectMany(x => GetSymbolsWithNewNames(x, compilation)).Flatten();
            return await PerformRenames(project, memberRenames.ToList());
        }

        private static IEnumerable<IEnumerable<(ISymbol Original, string NewName)>> GetSymbolsWithNewNames(INamespaceOrTypeSymbol containerSymbol, Compilation compilation)
        {
            var members = containerSymbol.GetMembers();
            var localScopeSymbolSets = GetUniqueNamesForScopeSymbols(containerSymbol, compilation, members);
            foreach (var scopeSymbolSet in localScopeSymbolSets) {
                yield return GetUniqueNamesForSymbolSet(scopeSymbolSet);
            }
            yield return GetUniqueNamesForSymbolSet(members);
        }

        private static IEnumerable<IEnumerable<ISymbol>> GetUniqueNamesForScopeSymbols(INamespaceOrTypeSymbol containerSymbol, Compilation compilation, System.Collections.Immutable.ImmutableArray<ISymbol> members)
        {
            if (!(containerSymbol is ITypeSymbol)) return Enumerable.Empty<IEnumerable<ISymbol>>();

            var semanticModel = compilation.GetSemanticModel(containerSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.SyntaxTree, true);
            return members.SelectMany(x => GetCsLocalSymbolsPerScope(semanticModel, x).Select(y => y.Union(x.Yield())));
        }

        private static IEnumerable<(ISymbol Original, string NewName)> GetUniqueNamesForSymbolSet(IEnumerable<ISymbol> symbols) {
            var membersByCaseInsensitiveName = symbols.ToLookup(m => m.Name, m => m, StringComparer.OrdinalIgnoreCase);
            var names = new HashSet<string>(membersByCaseInsensitiveName.Select(ms => ms.Key),
                StringComparer.OrdinalIgnoreCase);
            var symbolsWithNewNames = membersByCaseInsensitiveName.Where(ms => ms.Count() > 1)
                .SelectMany(symbolGroup => GetSymbolsWithNewNames(symbolGroup.ToArray(), names));
            return symbolsWithNewNames;
        }

        private static IEnumerable<(ISymbol Original, string NewName)> GetSymbolsWithNewNames(IReadOnlyCollection<ISymbol> symbolGroup, HashSet<string> names)
        {
            var methodSymbols = symbolGroup.OfType<IMethodSymbol>().Where(s => s.IsDefinedInSource()).ToArray();
            var cannotRename = symbolGroup.Where(s => !s.IsDefinedInSource() || s.IsIndexer()).ToArray();
            var specialSymbolUsingName = cannotRename.Any();
            var canKeepOneNormalMemberName = !specialSymbolUsingName && !methodSymbols.Any();
            symbolGroup = symbolGroup.Except(cannotRename).Except(methodSymbols).ToArray();
            (ISymbol Original, string NewName)[] methodsWithNewNames = GetMethodSymbolsWithNewNames(methodSymbols, names, specialSymbolUsingName);
            return GetSymbolsWithNewNames(symbolGroup, names.Add, canKeepOneNormalMemberName).Concat(methodsWithNewNames);
        }

        private static (ISymbol Original, string NewName)[] GetMethodSymbolsWithNewNames(IMethodSymbol[] methodSymbols,
            HashSet<string> names,
            bool specialSymbolUsingName)
        {
            var methodsByCaseInsensitiveSignature = methodSymbols
                .ToLookup(m => (m.Name.ToLowerInvariant(), string.Join(" ", m.Parameters.Select(p => p.Type))))
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
            var symbolsWithNewNames = toRename.OrderByDescending(x => x.DeclaredAccessibility).Skip(canKeepOne ? 1 :0).Select(tr =>
            {
                string newName = NameGenerator.GenerateUniqueName(GetBaseName(tr), canUse);
                return (Original: tr, NewName: newName);
            });
            return symbolsWithNewNames;
        }

        private static async Task<Project> PerformRenames(Project project, IReadOnlyCollection<(ISymbol Original, string NewName)> symbolsWithNewNames)
        {
            var solution = project.Solution;
            foreach (var (originalSymbol, newName) in symbolsWithNewNames) {
                project = solution.GetProject(project.Id);
                var compilation = await project.GetCompilationAsync();
                ISymbol currentDeclaration = SymbolFinder.FindSimilarSymbols(originalSymbol, compilation).First();
                solution = await Renamer.RenameSymbolAsync(solution, currentDeclaration, newName, solution.Workspace.Options);
            }

            return solution.GetProject(project.Id);
        }

        /// <remarks>
        /// In VB there's a special extra local defined with the same name as the method name, so the method symbol should be included in any conflict analysis
        /// </remarks>
        private static IEnumerable<IEnumerable<ISymbol>> GetCsLocalSymbolsPerScope(SemanticModel semanticModel, ISymbol x)
        {
            switch (x)
            {
                case IMethodSymbol methodSymbol:
                    return GetCsSymbolsDeclaredByMethod(semanticModel, methodSymbol, (CSS.BaseMethodDeclarationSyntax n) => (CS.CSharpSyntaxNode)n.ExpressionBody ?? n.Body);
                case IPropertySymbol propertySymbol:
                    return GetCsSymbolsDeclaredByProperty(semanticModel, propertySymbol);
                case IEventSymbol eventSymbol:
                    return GetCsSymbolsDeclaredByEvent(semanticModel, eventSymbol);
                case IFieldSymbol fieldSymbol:
                    return GetCsSymbolsDeclaredByField(semanticModel, fieldSymbol).Yield();
                default:
                    return Array.Empty<ISymbol>().Yield();
            }
        }

        public static IEnumerable<IEnumerable<ISymbol>> GetCsSymbolsDeclaredByMethod<TNode>(SemanticModel semanticModel, IMethodSymbol methodSymbol, Func<TNode, CS.CSharpSyntaxNode> selectWhereNotNull)
        {
            if (methodSymbol == null) {
                yield return Array.Empty<ISymbol>();
                yield break;
            }
            var bodies = DeclarationWhereNotNull(methodSymbol, selectWhereNotNull).Where(x => x.SyntaxTree == semanticModel.SyntaxTree);
            foreach (var body in bodies) {
                var descendantNodes = body.DescendantNodesAndSelf().OfType<CSS.BlockSyntax>().Where(x => x.DescendantNodes().OfType<CSS.BlockSyntax>().IsEmpty());
                foreach (var descendant in descendantNodes) {
                    yield return semanticModel.LookupSymbols(descendant.SpanStart).Where(x => x.MatchesKind(SymbolKind.Local, SymbolKind.Parameter, SymbolKind.TypeParameter));
                }
            }
        }

        private static IEnumerable<TResult> DeclarationWhereNotNull<TNode, TResult>(ISymbol symbol, Func<TNode, TResult> selectWhereNotNull)
        {
            return symbol.DeclaringSyntaxReferences.Select(d => d.GetSyntax()).OfType<TNode>().Select(selectWhereNotNull).Where(x => x != null);
        }

        private static IEnumerable<TResult> DeclarationWhereManyNotNull<TNode, TResult>(ISymbol symbol, Func<TNode, IEnumerable<TResult>> selectManyWhereNotNull)
        {
            return symbol.DeclaringSyntaxReferences.Select(d => d.GetSyntax()).OfType<TNode>().SelectMany(selectManyWhereNotNull).Where(x => x != null);
        }

        public static IEnumerable<IEnumerable<ISymbol>> GetCsSymbolsDeclaredByProperty(SemanticModel semanticModel, IPropertySymbol propertySymbol)
        {
            Func<CSS.AccessorDeclarationSyntax, CS.CSharpSyntaxNode> getAccessorBody = (CSS.AccessorDeclarationSyntax n) => (CS.CSharpSyntaxNode)n.ExpressionBody ?? n.Body;
            return GetCsSymbolsDeclaredByMethod(semanticModel, propertySymbol.GetMethod, getAccessorBody)
                .Concat(GetCsSymbolsDeclaredByMethod(semanticModel, propertySymbol.SetMethod, getAccessorBody));
        }

        public static IEnumerable<ISymbol> GetCsSymbolsDeclaredByField(SemanticModel semanticModel, IFieldSymbol fieldSymbol)
        {
            return DeclarationWhereManyNotNull(fieldSymbol,
                (CSS.BaseFieldDeclarationSyntax f) => f.Declaration.Variables.Select(v => v.Initializer?.Value))
                .SelectMany(i => semanticModel.LookupSymbols(i.SpanStart, fieldSymbol.ContainingType));
        }

        public static IEnumerable<IEnumerable<ISymbol>> GetCsSymbolsDeclaredByEvent(SemanticModel semanticModel, IEventSymbol propertySymbol)
        {
            Func<CSS.AccessorDeclarationSyntax, CS.CSharpSyntaxNode> getAccessorBody = (CSS.AccessorDeclarationSyntax n) => (CS.CSharpSyntaxNode)n.ExpressionBody ?? n.Body;
            return GetCsSymbolsDeclaredByMethod(semanticModel, propertySymbol.AddMethod, getAccessorBody)
                .Concat(GetCsSymbolsDeclaredByMethod(semanticModel, propertySymbol.RemoveMethod, getAccessorBody));
        }

        private static string GetBaseName(ISymbol declaration)
        {
            string prefix = declaration.Kind.ToString().ToLowerInvariant()[0] + "_";
            string baseName = prefix + declaration.Name.Substring(0, 1).ToUpperInvariant() + declaration.Name.Substring(1);
            return baseName;
        }
    }
}