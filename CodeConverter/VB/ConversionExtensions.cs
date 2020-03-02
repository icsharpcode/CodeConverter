using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using CS = Microsoft.CodeAnalysis.CSharp;

namespace ICSharpCode.CodeConverter.VB
{
    internal static class ConversionExtensions
    {
        public static bool HasUsingDirective(this CS.CSharpSyntaxTree tree, string fullName)
        {
            if (tree == null)
                throw new ArgumentNullException(nameof(tree));
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException("given namespace cannot be null or empty.", nameof(fullName));
            fullName = fullName.Trim();
            return tree.GetRoot()
                .DescendantNodes(MatchesNamespaceOrRoot)
                .OfType<CS.Syntax.UsingDirectiveSyntax>()
                .Any(u => u.Name.ToString().Equals(fullName, StringComparison.OrdinalIgnoreCase));
        }

        private static bool MatchesNamespaceOrRoot(SyntaxNode arg)
        {
            return arg is CS.Syntax.NamespaceDeclarationSyntax || arg is CS.Syntax.CompilationUnitSyntax;
        }

        public static IEnumerable<R> IndexedSelect<T, R>(this IEnumerable<T> source, Func<int, T, R> transform)
        {
            int i = 0;
            foreach (var item in source) {
                yield return transform(i, item);
                i++;
            }
        }
    }
}
