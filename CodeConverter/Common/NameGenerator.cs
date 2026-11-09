// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using ICSharpCode.CodeConverter.CSharp;

namespace ICSharpCode.CodeConverter.Common;

internal class NameGenerator
{
    private readonly Func<string, string> _getValidIdentifier;

    public static NameGenerator CS { get; } = new NameGenerator(x => CommonConversions.CsEscapedIdentifier(x).Text);
    public static NameGenerator Generic { get; } = new NameGenerator(x => x);

    private NameGenerator(Func<string, string> getValidIdentifier) => _getValidIdentifier = getValidIdentifier;

    public string GenerateUniqueName(string baseName, Func<string, bool> canUse) => GenerateUniqueName(baseName, string.Empty, canUse);

    public string GetUniqueVariableNameInScope(SemanticModel semanticModel, HashSet<string> generatedNames, VBasic.VisualBasicSyntaxNode node, string variableNameBase)
    {
        // Need to check not just the symbols this node has access to, but whether there are any nested blocks which have access to this node and contain a conflicting name
        var scopeStarts = GetScopeStarts(node);
        return GenerateUniqueVariableNameInScope(semanticModel, generatedNames, variableNameBase, scopeStarts);
    }

    public bool IsNameFreeInScope(SemanticModel semanticModel, HashSet<string> generatedNames, VBasic.VisualBasicSyntaxNode node, string name) =>
        IsNameFreeInScope(semanticModel, generatedNames, GetScopeStarts(node), name);

    public string GenerateUniqueVariableName(HashSet<string> generatedNames, string variableNameBase)
    {
        string uniqueName = GenerateUniqueName(variableNameBase, string.Empty, n => !generatedNames.Contains(n));
        generatedNames.Add(uniqueName);
        return uniqueName;
    }

    private string GenerateUniqueVariableNameInScope(SemanticModel semanticModel, HashSet<string> generatedNames,
        string variableNameBase, List<int> scopeStarts)
    {
        string uniqueName = GenerateUniqueName(variableNameBase, string.Empty, n => IsNameFreeInScope(semanticModel, generatedNames, scopeStarts, n));
        generatedNames.Add(uniqueName);
        return uniqueName;
    }

    private static bool IsNameFreeInScope(SemanticModel semanticModel, HashSet<string> generatedNames, List<int> scopeStarts, string name) =>
        !generatedNames.Contains(name) && !scopeStarts.SelectMany(scopeStart => semanticModel.LookupSymbols(scopeStart, name: name)).Any();

    private string GenerateUniqueName(string baseName, string extension, Func<string, bool> canUse)
    {
        baseName = _getValidIdentifier(baseName);
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

    private static List<int> GetScopeStarts(VBasic.VisualBasicSyntaxNode node)
    {
        return node.GetAncestorOrThis<VBSyntax.StatementSyntax>().DescendantNodesAndSelf()
            .OfType<VBSyntax.StatementSyntax>().Select(n => n.SpanStart).ToList();
    }

}