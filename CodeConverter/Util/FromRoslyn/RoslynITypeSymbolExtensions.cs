using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Util.FromRoslyn
{

    /// <summary>
    /// Modified version of Roslyn implementation - uses "Equals" rather than SymbolEquivalenceComparer
    /// </summary>
    internal static class RoslynITypeSymbolExtensions
    {
        // Determine if "type" inherits from or implements "baseType", ignoring constructed types, and dealing
        // only with original types.
        public static bool InheritsFromOrImplementsOrEqualsIgnoringConstruction(
            this ITypeSymbol type, ITypeSymbol baseType)
        {
            var originalBaseType = baseType.OriginalDefinition;
            type = type.OriginalDefinition;

            if (Equals(type, originalBaseType)) {
                return true;
            }

            IEnumerable<ITypeSymbol> baseTypes = (baseType.TypeKind == TypeKind.Interface) ? type.AllInterfaces : type.GetBaseTypes();
            return baseTypes.Contains(t => Equals(t.OriginalDefinition, originalBaseType));
        }
    }
}

