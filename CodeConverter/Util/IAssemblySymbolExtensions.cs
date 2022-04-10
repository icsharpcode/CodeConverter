namespace ICSharpCode.CodeConverter.Util;

internal static class IAssemblySymbolExtensions
{
    public static bool IsSameAssemblyOrHasFriendAccessTo(this IAssemblySymbol assembly, IAssemblySymbol toAssembly)
    {
        return
            SymbolEqualityComparer.IncludeNullability.Equals(assembly, toAssembly) ||
            (assembly.IsInteractive && toAssembly.IsInteractive) ||
            toAssembly.GivesAccessTo(assembly);
    }
}