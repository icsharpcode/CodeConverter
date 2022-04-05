using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Shared;

internal static class CompilationExtensions
{
    public static IEnumerable<INamespaceOrTypeSymbol> GetAllNamespacesAndTypes(this Compilation compilation) => compilation.GlobalNamespace.FollowProperty((INamespaceOrTypeSymbol n) => n.GetMembers().Where(s => s.IsNamespace() || s.IsDefinedInSource()).OfType<INamespaceOrTypeSymbol>());
}