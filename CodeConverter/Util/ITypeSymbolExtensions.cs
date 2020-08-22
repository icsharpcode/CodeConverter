using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ICSharpCode.CodeConverter.Util.FromRoslyn;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Util
{
    internal static class ITypeSymbolExtensions
    {

        public static bool IsDelegateReferencableByName(this ITypeSymbol t)
        {
            return t.CanBeReferencedByName && t.IsDelegateType();
        }

        public static bool ContainsMember(this ITypeSymbol potentialContainer, ISymbol potentialMember)
        {
            return potentialContainer.FollowProperty(t => t.BaseType).Contains(potentialMember.ContainingType);
        }

        public static bool HasCsKeyword(this ITypeSymbol typeSymbol)
        {
            if (typeSymbol != null) {
                switch (typeSymbol.SpecialType) {
                    case SpecialType.System_Boolean:
                    case SpecialType.System_Char:
                    case SpecialType.System_SByte:
                    case SpecialType.System_Byte:
                    case SpecialType.System_Int16:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_UInt64:
                    case SpecialType.System_Decimal:
                    case SpecialType.System_Single:
                    case SpecialType.System_Double:
                    case SpecialType.System_String:
                        return true;
                }
            }

            return false;
        }

        public static bool IsArrayOf(this ITypeSymbol t, SpecialType specialType)
        {
            return t is IArrayTypeSymbol ats && ats.ElementType.SpecialType == specialType;
        }

        public static bool IsEnumerableOfExactType(this ITypeSymbol symbol, ITypeSymbol typeArg)
        {
            return SymbolEquivalenceComparer.Instance.Equals(GetEnumerableElementTypeOrDefault(symbol), typeArg);
        }

        private static ITypeSymbol GetEnumerableElementTypeOrDefault(ITypeSymbol symbol)
        {
            if (symbol is IArrayTypeSymbol ats) return ats.ElementType;
            if (symbol is INamedTypeSymbol nt) return nt.Yield().Concat(nt.AllInterfaces).OfType<INamedTypeSymbol>()
                .OnlyOrDefault(impl => impl.MetadataName == "IEnumerable`1")
                ?.TypeArguments.OnlyOrDefault();
            return null;
        }
    }
}

