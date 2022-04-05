namespace ICSharpCode.CodeConverter.Common;

internal static class CompilationExtensions
{
    public static IEnumerable<INamespaceOrTypeSymbol> GetAllNamespacesAndTypes(this Compilation compilation) => compilation.GlobalNamespace.FollowProperty((INamespaceOrTypeSymbol n) => n.GetMembers().Where(s => s.IsNamespace() || s.IsDefinedInSource()).OfType<INamespaceOrTypeSymbol>());
}