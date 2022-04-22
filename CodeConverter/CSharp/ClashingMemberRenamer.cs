using ICSharpCode.CodeConverter.Util.FromRoslyn;

namespace ICSharpCode.CodeConverter.CSharp;

internal static class ClashingMemberRenamer
{
    /// <summary>
    /// Renames symbols in a VB project so that they don't clash with rules for C# member names, attempting to rename the least public ones first.
    /// See https://github.com/icsharpcode/CodeConverter/issues/420
    /// </summary>
    public static async Task<Project> RenameClashingSymbolsAsync(Project project, IProgress<ConversionProgress> progress)
    {
        var compilation = await project.GetCompilationAsync();
        var memberRenames = SymbolRenamer.GetNamespacesAndTypesInAssembly(project, compilation)
            .SelectMany(x => GetSymbolsWithNewNames(x, compilation));
        return await SymbolRenamer.PerformRenamesAsync(project, memberRenames.ToList(), progress);
    }

    private static IEnumerable<(ISymbol Original, string NewName)> GetSymbolsWithNewNames(INamespaceOrTypeSymbol containerSymbol, Compilation compilation)
    {
        if (containerSymbol.IsNamespace) return Enumerable.Empty<(ISymbol Original, string NewName)>();

        // A hack here
        // Enum Retirement.Shared.Enumerations.DynamicSection.Fld has over 10,000 elements
        // The conversion program tries to rename each of the element and it takes forever.
        // We don't want to rename them
        if ((containerSymbol.Name == "Fld") && (containerSymbol.ContainingSymbol.Name == "DynamicSection")) return Enumerable.Empty<(ISymbol Original, string NewName)>();

        // Enum WindowsLogon.LOGON32_LOGON and WindowsLogon.LOGON32_PROVIDER were renamed. We don't want to rename them
        if ((containerSymbol.Name == "LOGON32_LOGON") && (containerSymbol.ContainingSymbol.Name == "WindowsLogon")) return Enumerable.Empty<(ISymbol Original, string NewName)>();
        if ((containerSymbol.Name == "LOGON32_PROVIDER") && (containerSymbol.ContainingSymbol.Name == "WindowsLogon")) return Enumerable.Empty<(ISymbol Original, string NewName)>();

        var members = containerSymbol.GetMembers()
            .Where(m => m.Locations.Any(loc => loc.SourceTree != null && compilation.ContainsSyntaxTree(loc.SourceTree)))
            .Where(s => containerSymbol.Name == s.Name || containerSymbol is INamedTypeSymbol nt && nt.IsEnumType() && SymbolRenamer.GetName(s).StartsWith(containerSymbol.Name, StringComparison.InvariantCulture));
        var symbolSet = containerSymbol.Yield().Concat(members).ToArray();
        return SymbolRenamer.GetSymbolsWithNewNames(symbolSet, new HashSet<string>(symbolSet.Select(SymbolRenamer.GetName)), true);
    }
}