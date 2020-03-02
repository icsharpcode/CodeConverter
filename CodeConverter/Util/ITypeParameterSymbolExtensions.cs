using System.Linq;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Util
{
#if NR6
    public
#endif
    internal static class ITypeParameterSymbolExtensions
    {
        public static INamedTypeSymbol GetNamedTypeSymbolConstraint(this ITypeParameterSymbol typeParameter)
        {
            return typeParameter.ConstraintTypes.Select(GetNamedTypeSymbol).WhereNotNull().FirstOrDefault();
        }

        private static INamedTypeSymbol GetNamedTypeSymbol(ITypeSymbol type)
        {
            return type is INamedTypeSymbol
                ? (INamedTypeSymbol)type
                    : type is ITypeParameterSymbol
                ? GetNamedTypeSymbolConstraint((ITypeParameterSymbol)type)
                    : null;
        }
    }
}
