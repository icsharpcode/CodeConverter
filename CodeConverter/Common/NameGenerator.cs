// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace ICSharpCode.CodeConverter.Common;

internal static class NameGenerator
{
    public static string GenerateUniqueName(string baseName, Func<string, bool> canUse)
    {
        return GenerateUniqueName(baseName, string.Empty, canUse);
    }

    private static string GenerateUniqueName(string baseName, string extension, Func<string, bool> canUse)
    {
        if (!string.IsNullOrEmpty(extension) && !extension.StartsWith(".", StringComparison.InvariantCulture)) {
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

    public static string GetUniqueVariableNameInScope(SemanticModel semanticModel, HashSet<string> generatedNames, VBasic.VisualBasicSyntaxNode node, string variableNameBase)
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
}