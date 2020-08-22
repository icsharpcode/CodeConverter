// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.Shared
{
    internal static class NameGenerator
    {
        public static IList<string> EnsureUniqueness(
            IList<string> names,
            Func<string, bool> canUse = null)
        {
            return EnsureUniqueness(names, names.Select(_ => false).ToList(), canUse);
        }

        /// <summary>
        /// Ensures that any 'names' is unique and does not collide with any other name.  Names that
        /// are marked as IsFixed can not be touched.  This does mean that if there are two names
        /// that are the same, and both are fixed that you will end up with non-unique names at the
        /// end.
        /// </summary>
        public static IList<string> EnsureUniqueness(
            IList<string> names,
            IList<bool> isFixed,
            Func<string, bool> canUse = null,
            bool isCaseSensitive = true)
        {
            var copy = names.ToList();
            EnsureUniquenessInPlace(copy, isFixed, canUse, isCaseSensitive);
            return copy;
        }

        public static IList<string> EnsureUniqueness(IList<string> names, bool isCaseSensitive)
        {
            return EnsureUniqueness(names, names.Select(_ => false).ToList(), isCaseSensitive: isCaseSensitive);
        }

        /// <summary>
        /// Transforms baseName into a name that does not conflict with any name in 'reservedNames'
        /// </summary>
        public static string EnsureUniqueness(
            string baseName,
            IEnumerable<string> reservedNames,
            bool isCaseSensitive = true)
        {
            var names = new List<string> { baseName };
            var isFixed = new List<bool> { false };

            names.AddRange(reservedNames.Distinct());
            isFixed.AddRange(Enumerable.Repeat(true, names.Count - 1));

            var result = EnsureUniqueness(names, isFixed, isCaseSensitive: isCaseSensitive);
            return result.First();
        }

        private static void EnsureUniquenessInPlace(
            IList<string> names,
            IList<bool> isFixed,
            Func<string, bool> canUse,
            bool isCaseSensitive = true)
        {
            canUse ??= (s => true);

            // Don't enumerate as we will be modifying the collection in place.
            for (var i = 0; i < names.Count; i++) {
                var name = names[i];
                var collisionIndices = GetCollisionIndices(names, name, isCaseSensitive);

                if (canUse(name) && collisionIndices.Count < 2) {
                    // no problems with this parameter name, move onto the next one.
                    continue;
                }

                HandleCollisions(isFixed, names, name, collisionIndices, canUse, isCaseSensitive);
            }
        }

        private static void HandleCollisions(
            IList<bool> isFixed,
            IList<string> names,
            string name,
            List<int> collisionIndices,
            Func<string, bool> canUse,
            bool isCaseSensitive = true)
        {
            var suffix = 1;
            var comparer = isCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
            for (var i = 0; i < collisionIndices.Count; i++) {
                var collisionIndex = collisionIndices[i];
                if (isFixed[collisionIndex]) {
                    // can't do anything about this name.
                    continue;
                }

                while (true) {
                    var newName = name + suffix++;
                    if (!names.Contains(newName, comparer) && canUse(newName)) {
                        // Found a name that doesn't conflict with anything else.
                        names[collisionIndex] = newName;
                        break;
                    }
                }
            }
        }

        private static List<int> GetCollisionIndices(
            IList<string> names,
            string name,
            bool isCaseSensitive = true)
        {
            var comparer = isCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
            var collisionIndices =
                names.Select((currentName, index) => new { currentName, index })
                     .Where(t => comparer.Equals(t.currentName, name))
                     .Select(t => t.index)
                     .ToList();
            return collisionIndices;
        }

        public static string GenerateUniqueName(string baseName, Func<string, bool> canUse)
        {
            return GenerateUniqueName(baseName, string.Empty, canUse);
        }

        public static string GenerateUniqueName(string baseName, HashSet<string> names, StringComparer comparer)
        {
            return GenerateUniqueName(baseName, x => !names.Contains(x, comparer));
        }

        public static string GenerateUniqueName(string baseName, string extension, Func<string, bool> canUse)
        {
            if (!string.IsNullOrEmpty(extension) && !extension.StartsWith(".")) {
                extension = "." + extension;
            }

            var name = baseName + extension;
            var index = 1;

            // Check for collisions
            while (!canUse(name)) {
                name = baseName + index + extension;
                index++;
            }

            return name;
        }

        public static string GenerateSafeCSharpName(string name)
        {
            var token = SyntaxFactory.ParseToken(name);
            if (!token.IsKind(SyntaxKind.IdentifierToken))
                return "@" + name;
            return name;
        }

        public static string GenerateSafeVBName(string name)
        {
            var token = VBasic.SyntaxFactory.ParseToken(name);
            if (!token.IsKind(VBasic.SyntaxKind.IdentifierToken))
                return "[" + name + "]";
            return name;
        }

        public static string GetUniqueVariableNameInScope(SemanticModel semanticModel, HashSet<string> generatedNames, VBasic.VisualBasicSyntaxNode node, string variableNameBase)
        {
            // Need to check not just the symbols this node has access to, but whether there are any nested blocks which have access to this node and contain a conflicting name
            var scopeStarts = GetScopeStarts(node);
            return GenerateUniqueVariableNameInScope(semanticModel, generatedNames, variableNameBase, scopeStarts);
        }

        public static string GetUniqueVariableNameInScope(SemanticModel semanticModel, HashSet<string> generatedNames, CSharpSyntaxNode node, string variableNameBase)
        {
            // Need to check not just the symbols this node has access to, but whether there are any nested blocks which have access to this node and contain a conflicting name
            var scopeStarts = GetScopeStarts(node);
            return GenerateUniqueVariableNameInScope(semanticModel, generatedNames, variableNameBase, scopeStarts);
        }

        private static string GenerateUniqueVariableNameInScope(SemanticModel semanticModel, HashSet<string> generatedNames,
            string variableNameBase, List<int> scopeStarts)
        {
            string uniqueName = GenerateUniqueName(variableNameBase, string.Empty,
                n => {
                    var matchingSymbols =
                        scopeStarts.SelectMany(scopeStart => semanticModel.LookupSymbols(scopeStart, name: n));
                    return !generatedNames.Contains(n) && !matchingSymbols.Any();
                });
            generatedNames.Add(uniqueName);
            return uniqueName;
        }

        private static List<int> GetScopeStarts(VBasic.VisualBasicSyntaxNode node)
        {
            return node.GetAncestorOrThis<VBSyntax.StatementSyntax>().DescendantNodesAndSelf()
                .OfType<VBSyntax.StatementSyntax>().Select(n => n.SpanStart).ToList();
        }

        private static List<int> GetScopeStarts(CSharpSyntaxNode node)
        {
            return node.GetAncestorOrThis<StatementSyntax>()?.DescendantNodesAndSelf()
                .OfType<StatementSyntax>().Select(n => n.SpanStart).ToList()
                ?? new List<int> { node.SpanStart };
        }
    }
}
