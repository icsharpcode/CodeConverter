using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Util
{
    internal static class IAssemblySymbolExtensions
    {
        public static bool IsSameAssemblyOrHasFriendAccessTo(this IAssemblySymbol assembly, IAssemblySymbol toAssembly)
        {
            return
                Equals(assembly, toAssembly) ||
                (assembly.IsInteractive && toAssembly.IsInteractive) ||
                toAssembly.GivesAccessTo(assembly);
        }
    }
}

