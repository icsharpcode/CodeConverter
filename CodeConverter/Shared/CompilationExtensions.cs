using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using ISymbolExtensions = ICSharpCode.CodeConverter.Util.ISymbolExtensions;

namespace ICSharpCode.CodeConverter.Shared;

internal static class CompilationExtensions
{
    public static IEnumerable<INamespaceOrTypeSymbol> GetAllNamespacesAndTypes(this Compilation compilation) => compilation.GlobalNamespace.FollowProperty((INamespaceOrTypeSymbol n) => n.GetMembers().OfType<INamespaceOrTypeSymbol>());
}