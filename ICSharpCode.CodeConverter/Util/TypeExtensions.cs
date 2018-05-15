using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Util
{
#if NR6
    public
#endif
    static class TypeExtensions
    {
        #region GetDelegateInvokeMethod
        /// <summary>
        /// Gets the invoke method for a delegate type.
        /// </summary>
        /// <remarks>
        /// Returns null if the type is not a delegate type; or if the invoke method could not be found.
        /// </remarks>
        public static IMethodSymbol GetDelegateInvokeMethod(this ITypeSymbol type)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (type.TypeKind == TypeKind.Delegate)
                return type.GetMembers("Invoke").OfType<IMethodSymbol>().FirstOrDefault(m => m.MethodKind == MethodKind.DelegateInvoke);
            return null;
        }
        #endregion

        /// <summary>
        /// Gets the full name of the namespace.
        /// </summary>
        public static string GetFullName(this INamespaceSymbol ns)
        {
            return ns.ToCSharpDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        }

        /// <summary>
        /// Gets the full name. The full name is no 1:1 representation of a type it's missing generics and it has a poor
        /// representation for inner types (just dot separated).
        /// DO NOT use this method unless you're know what you do. It's only implemented for legacy code.
        /// </summary>
        public static string GetFullName(this ITypeSymbol type)
        {
            return type.ToCSharpDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        }

        /// <summary>
        /// Returns true if the type is public and was tagged with
        /// [System.ComponentModel.ToolboxItem (true)]
        /// </summary>
        /// <returns><c>true</c> if is designer browsable the specified symbol; otherwise, <c>false</c>.</returns>
        /// <param name="symbol">Symbol.</param>
        public static bool IsToolboxItem(this ITypeSymbol symbol)
        {
            if (symbol == null)
                throw new ArgumentNullException("symbol");
            if (symbol.DeclaredAccessibility != Accessibility.Public)
                return false;
            var toolboxItemAttr = symbol.GetAttributes().FirstOrDefault(attr => attr.AttributeClass.Name == "ToolboxItemAttribute" && attr.AttributeClass.ContainingNamespace.MetadataName == "System.ComponentModel");
            if (toolboxItemAttr != null && toolboxItemAttr.ConstructorArguments.Length == 1) {
                try {
                    return (bool)toolboxItemAttr.ConstructorArguments[0].Value;
                } catch {
                }
            }
            return false;
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
        /// Gets all base classes.
        /// </summary>
        /// <returns>The all base classes.</returns>
        /// <param name="type">Type.</param>
        public static IEnumerable<INamedTypeSymbol> GetAllBaseClasses(this INamedTypeSymbol type, bool includeSuperType = false)
        {
            if (!includeSuperType)
                type = type.BaseType;
            while (type != null) {
                yield return type;
                type = type.BaseType;
            }
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
        /// Determines if derived from baseType. Includes itself and all base classes, but does not include interfaces.
        /// </summary>
        /// <returns><c>true</c> if is derived from class the specified type baseType; otherwise, <c>false</c>.</returns>
        /// <param name="type">Type.</param>
        /// <param name="baseType">Base type.</param>
        public static bool IsDerivedFromClass(this INamedTypeSymbol type, INamedTypeSymbol baseType)
        {
            //NR5 is returning true also for same type
            for (; type != null; type = type.BaseType) {
                if (type == baseType) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Determines if derived from baseType. Includes itself, all base classes and all interfaces.
        /// </summary>
        /// <returns><c>true</c> if is derived from the specified type baseType; otherwise, <c>false</c>.</returns>
        /// <param name="type">Type.</param>
        /// <param name="baseType">Base type.</param>
        public static bool IsDerivedFromClassOrInterface(this INamedTypeSymbol type, INamedTypeSymbol baseType)
        {
            //NR5 is returning true also for same type
            for (; type != null; type = type.BaseType) {
                if (type == baseType) {
                    return true;
                }
            }
            //And interfaces
            foreach (var inter in type.AllInterfaces) {
                if (inter == baseType) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the full name of the metadata.
        /// In case symbol is not INamedTypeSymbol it returns raw MetadataName
        /// Example: Generic type returns T1, T2...
        /// </summary>
        /// <returns>The full metadata name.</returns>
        /// <param name="symbol">Symbol.</param>
        public static string GetFullMetadataName(this ITypeSymbol symbol)
        {
            //This is for comaptibility with NR5 reflection name in case of generic types like T1, T2...
            var namedTypeSymbol = symbol as INamedTypeSymbol;
            return namedTypeSymbol != null ? GetFullMetadataName(namedTypeSymbol) : symbol.MetadataName;
        }

        /// <summary>
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
            var ns = symbol.ContainingNamespace;
            while (ns != null && !ns.IsGlobalNamespace) {
                fullName.Insert(0, '.');
                fullName.Insert(0, ns.MetadataName);
                ns = ns.ContainingNamespace;
            }
            return fullName.ToString();
        }
    }
}

