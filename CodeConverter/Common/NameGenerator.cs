// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.CompilerServices;
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
        var namesInBlock = GetNamesDeclaredInContainingBlock(node);
        string uniqueName = GenerateUniqueName(variableNameBase, string.Empty, n => !namesInBlock.Contains(n) && IsNameFreeInScope(semanticModel, generatedNames, scopeStarts, n));
        generatedNames.Add(uniqueName);
        return uniqueName;
    }

    /// <summary>
    /// A label is in its own namespace in C#, so only other labels matter. They aren't found by <see cref="SemanticModel.LookupSymbols"/>.
    /// </summary>
    public string GetUniqueLabelNameInScope(SemanticModel semanticModel, HashSet<string> generatedNames, VBasic.VisualBasicSyntaxNode node, string labelNameBase)
    {
        var scopeStarts = GetScopeStarts(node);
        string uniqueName = GenerateUniqueName(labelNameBase, string.Empty, n => !generatedNames.Contains(n) &&
            !scopeStarts.SelectMany(scopeStart => semanticModel.LookupLabels(scopeStart, n)).Any());
        generatedNames.Add(uniqueName);
        return uniqueName;
    }

    /// <summary>
    /// For a local (or pattern variable) declared in the C# block that holds the node's statement, without reserving the name
    /// </summary>
    public bool IsLocalNameFreeInScope(SemanticModel semanticModel, VBasic.VisualBasicSyntaxNode node, string name) =>
        !GetNamesDeclaredInContainingBlock(node).Contains(name) && !GetScopeStarts(node).Any(scopeStart => semanticModel.LookupSymbols(scopeStart, name: name).Any());

    /// <summary>
    /// For a name declared in a scope of its own (e.g. a query range variable), which a name in a sibling nested scope doesn't conflict with, unlike a local's
    /// </summary>
    public bool IsNameFreeInScope(SemanticModel semanticModel, HashSet<string> generatedNames, VBasic.VisualBasicSyntaxNode node, string name) =>
        IsNameFreeInScope(semanticModel, generatedNames, GetScopeStarts(node), name);

    public string GenerateUniqueVariableName(HashSet<string> generatedNames, string variableNameBase)
    {
        string uniqueName = GenerateUniqueName(variableNameBase, string.Empty, n => !generatedNames.Contains(n));
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

    /// <summary>
    /// A generated local is declared before the statement, in the method body block that contains it, and a C# local can't share a name with any local
    /// (or lambda parameter, or query range variable) in a scope nested in its own, even one declared before it, e.g. a For Each variable in an earlier loop (CS0136).
    /// Those names are found once per block, rather than looking each name up at every statement in it, which grows with the square of the block's size.
    /// A type member's name (e.g. a Static local's field) is outside any method body, so it isn't affected.
    /// </summary>
    private static IReadOnlyCollection<string> GetNamesDeclaredInContainingBlock(VBasic.VisualBasicSyntaxNode node)
    {
        var statement = GetExecutableStatementOrSelf(node.GetAncestorOrThis<VBSyntax.StatementSyntax>());
        return statement is VBSyntax.ExecutableStatementSyntax { Parent: { } block }
            ? NamesDeclaredPerBlock.GetValue(block, GetNamesDeclaredWithin)
            : Array.Empty<string>();
    }

    /// <summary>
    /// A block's header (e.g. If, ElseIf, Loop Until, Case, For Each) isn't a statement of its own in C#: a name declared in it is in scope in the block's body,
    /// so it's the names of the block it heads, and the block that contains that, which matter
    /// </summary>
    private static VBSyntax.StatementSyntax GetExecutableStatementOrSelf(VBSyntax.StatementSyntax statement)
    {
        while (statement is not VBSyntax.ExecutableStatementSyntax && statement?.Parent is VBSyntax.StatementSyntax and not VBSyntax.DeclarationStatementSyntax and var parent) {
            statement = parent;
        }
        return statement;
    }

    private static readonly ConditionalWeakTable<SyntaxNode, HashSet<string>> NamesDeclaredPerBlock = new();

    private static HashSet<string> GetNamesDeclaredWithin(SyntaxNode block) =>
        new(block.DescendantNodes().Select(n => n switch {
            VBSyntax.ModifiedIdentifierSyntax declared => declared.Identifier.ValueText,
            VBSyntax.ForStatementSyntax { ControlVariable: VBSyntax.IdentifierNameSyntax inferred } => inferred.Identifier.ValueText,
            VBSyntax.ForEachStatementSyntax { ControlVariable: VBSyntax.IdentifierNameSyntax inferred } => inferred.Identifier.ValueText,
            VBSyntax.CatchStatementSyntax { IdentifierName: { } caught } => caught.Identifier.ValueText,
            _ => null
        }).Where(name => name != null), StringComparer.OrdinalIgnoreCase);
}
