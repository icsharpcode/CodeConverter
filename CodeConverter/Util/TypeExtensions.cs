using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Util
{
    internal static class TypeExtensions
    {
        /// <summary>
        /// Gets the invoke method for a delegate type.
        /// </summary>
        /// <remarks>
        /// Returns null if the type is not a delegate type; or if the invoke method could not be found.
        /// </remarks>
        public static IMethodSymbol GetDelegateInvokeMethod(this ITypeSymbol type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (type.TypeKind == TypeKind.Delegate && type is INamedTypeSymbol namedType)
                return namedType.DelegateInvokeMethod;
            return null;
        }

        public static bool IsNullableType(this ITypeSymbol type)
        {
            var original = type.OriginalDefinition;
            return original.SpecialType == SpecialType.System_Nullable_T;
        }

        public static ITypeSymbol GetNullableUnderlyingType(this ITypeSymbol type)
        {
            if (!IsNullableType(type))
                return null;
            return ((INamedTypeSymbol)type).TypeArguments[0];
        }

        /// <summary>
        /// Gets all base classes and interfaces.
        /// </summary>
        /// <returns>All classes and interfaces.</returns>
        /// <param name="type">Type.</param>
        public static IEnumerable<INamedTypeSymbol> GetAllBaseClassesAndInterfaces(this INamedTypeSymbol type, bool includeSuperType = false)
        {
            if (!includeSuperType)
                type = type.BaseType;
            var curType = type;
            while (curType != null) {
                yield return curType;
                curType = curType.BaseType;
            }

            foreach (var inter in type.AllInterfaces) {
                yield return inter;
            }
        }

        /// <summary>
        /// TODO: Eradicate this in favour of CommonConversions.GetFullyQualifiedNameSyntax
        /// Gets the full name of the metadata.
        /// In case symbol is not INamedTypeSymbol it returns raw MetadataName
        /// Example: Generic type returns T1, T2...
        /// </summary>
        /// <returns>The full metadata name.</returns>
        /// <param name="symbol">Symbol.</param>
        public static string GetFullMetadataName(this ITypeSymbol symbol)
        {
            if (symbol is IArrayTypeSymbol ats) {
                return GetFullMetadataName(ats.ElementType) + "[" + new string(Enumerable.Repeat(',', ats.Rank - 1).ToArray()) + "]";
            }
            //This is for comaptibility with NR5 reflection name in case of generic types like T1, T2...
            var namedTypeSymbol = symbol as INamedTypeSymbol;
            return namedTypeSymbol != null ? GetFullMetadataName(namedTypeSymbol) : symbol.MetadataName;
        }

        /// <summary>
        /// TODO: Eradicate this in favour of CommonConversions.GetFullyQualifiedNameSyntax
        /// Gets the full MetadataName(ReflectionName in NR5).
        /// Example: Namespace1.Namespace2.Classs1+NestedClassWithTwoGenericTypes`2+NestedClassWithoutGenerics
        /// </summary>
        /// <returns>The full metadata name.</returns>
        /// <param name="symbol">Symbol.</param>
        public static string GetFullMetadataName(this INamedTypeSymbol symbol)
        {
            var fullName = new StringBuilder(symbol.MetadataName);
            var parentType = symbol.ContainingType;
            while (parentType != null) {
                fullName.Insert(0, '+');
                fullName.Insert(0, parentType.MetadataName);
                parentType = parentType.ContainingType;
            }
            return GetFullMetadataName(symbol.ContainingNamespace, fullName);
        }

        public static string GetFullMetadataName(this INamespaceSymbol ns, StringBuilder sb = null)
        {
            sb ??= new StringBuilder();
            while (ns != null && !ns.IsGlobalNamespace) {
                if (sb.Length > 0) sb.Insert(0, '.');
                sb.Insert(0, ns.MetadataName);
                ns = ns.ContainingNamespace;
            }

            return sb.ToString();
        }
    }
}

