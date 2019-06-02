using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.Util
{
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NR6
    public
#endif
    static class ITypeSymbolExtensions
    {
        public static bool ImplementsSpecialTypeInterface(this ITypeSymbol symbol, SpecialType type)
        {
            if (symbol.SpecialType == type) {
                return true;
            }

            var namedType = symbol as INamedTypeSymbol;
            if (namedType != null && namedType.IsGenericType && namedType != namedType.ConstructedFrom) {
                return namedType.ConstructedFrom.ImplementsSpecialTypeInterface(type);
            }

            var typeParam = symbol as ITypeParameterSymbol;
            if (typeParam != null) {
                return typeParam.ConstraintTypes.Any(x => x.ImplementsSpecialTypeInterface(type));
            }

            if (symbol.AllInterfaces.Any(x => x.ImplementsSpecialTypeInterface(type))) {
                return true;
            }

            return false;
        }

        private const string DefaultParameterName = "p";
        private const string DefaultBuiltInParameterName = "v";

        public static IList<INamedTypeSymbol> GetAllInterfacesIncludingThis(this ITypeSymbol type)
        {
            var allInterfaces = type.AllInterfaces;
            var namedType = type as INamedTypeSymbol;
            if (namedType != null && namedType.TypeKind == TypeKind.Interface && !allInterfaces.Contains(namedType)) {
                var result = new List<INamedTypeSymbol>(allInterfaces.Length + 1);
                result.Add(namedType);
                result.AddRange(allInterfaces);
                return result;
            }

            return allInterfaces;
        }

        public static bool IsAbstractClass(this ITypeSymbol symbol)
        {
            return symbol?.TypeKind == TypeKind.Class && symbol.IsAbstract;
        }

        public static bool IsSystemVoid(this ITypeSymbol symbol)
        {
            return symbol?.SpecialType == SpecialType.System_Void;
        }

        public static bool IsNullable(this ITypeSymbol symbol)
        {
            return symbol?.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
        }

        public static bool IsErrorType(this ITypeSymbol symbol)
        {
            return symbol?.TypeKind == TypeKind.Error;
        }

        public static bool IsModuleType(this ITypeSymbol symbol)
        {
            return symbol?.TypeKind == TypeKind.Module;
        }

        public static bool IsInterfaceType(this ITypeSymbol symbol)
        {
            return symbol?.TypeKind == TypeKind.Interface;
        }

        public static bool IsDelegateType(this ITypeSymbol symbol)
        {
            return symbol?.TypeKind == TypeKind.Delegate;
        }

        public static bool IsAnonymousType(this INamedTypeSymbol symbol)
        {
            return symbol?.IsAnonymousType == true;
        }
        
        public static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(this ITypeSymbol type)
        {
            var current = type;
            while (current != null) {
                yield return current;
                current = current.BaseType;
            }
        }

        public static IEnumerable<INamedTypeSymbol> GetBaseTypes(this ITypeSymbol type)
        {
            var current = type.BaseType;
            while (current != null) {
                yield return current;
                current = current.BaseType;
            }
        }

        public static IEnumerable<ITypeSymbol> GetContainingTypesAndThis(this ITypeSymbol type)
        {
            var current = type;
            while (current != null) {
                yield return current;
                current = current.ContainingType;
            }
        }

        public static IEnumerable<INamedTypeSymbol> GetContainingTypes(this ITypeSymbol type)
        {
            var current = type.ContainingType;
            while (current != null) {
                yield return current;
                current = current.ContainingType;
            }
        }

        public static bool IsAttribute(this ITypeSymbol symbol)
        {
            for (var b = symbol.BaseType; b != null; b = b.BaseType) {
                if (b.MetadataName == "Attribute" &&
                    b.ContainingType == null &&
                    b.ContainingNamespace != null &&
                    b.ContainingNamespace.Name == "System" &&
                    b.ContainingNamespace.ContainingNamespace != null &&
                    b.ContainingNamespace.ContainingNamespace.IsGlobalNamespace) {
                    return true;
                }
            }

            return false;
        }

        public static IList<ITypeParameterSymbol> GetReferencedMethodTypeParameters(
            this ITypeSymbol type, IList<ITypeParameterSymbol> result = null)
        {
            result = result ?? new List<ITypeParameterSymbol>();
            type?.Accept(new CollectTypeParameterSymbolsVisitor(result, onlyMethodTypeParameters: true));
            return result;
        }

        public static IList<ITypeParameterSymbol> GetReferencedTypeParameters(
            this ITypeSymbol type, IList<ITypeParameterSymbol> result = null)
        {
            result = result ?? new List<ITypeParameterSymbol>();
            type?.Accept(new CollectTypeParameterSymbolsVisitor(result, onlyMethodTypeParameters: false));
            return result;
        }

        private class CollectTypeParameterSymbolsVisitor : SymbolVisitor
        {
            private readonly HashSet<ISymbol> _visited = new HashSet<ISymbol>();
            private readonly bool _onlyMethodTypeParameters;
            private readonly IList<ITypeParameterSymbol> _typeParameters;

            public CollectTypeParameterSymbolsVisitor(
                IList<ITypeParameterSymbol> typeParameters,
                bool onlyMethodTypeParameters)
            {
                _onlyMethodTypeParameters = onlyMethodTypeParameters;
                _typeParameters = typeParameters;
            }

            public override void DefaultVisit(ISymbol node)
            {
                throw new NotImplementedException();
            }

            public override void VisitDynamicType(IDynamicTypeSymbol symbol)
            {
            }

            public override void VisitArrayType(IArrayTypeSymbol symbol)
            {
                if (!_visited.Add(symbol)) {
                    return;
                }

                symbol.ElementType.Accept(this);
            }

            public override void VisitNamedType(INamedTypeSymbol symbol)
            {
                if (_visited.Add(symbol)) {
                    foreach (var child in symbol.GetAllTypeArguments()) {
                        child.Accept(this);
                    }
                }
            }

            public override void VisitPointerType(IPointerTypeSymbol symbol)
            {
                if (!_visited.Add(symbol)) {
                    return;
                }

                symbol.PointedAtType.Accept(this);
            }

            public override void VisitTypeParameter(ITypeParameterSymbol symbol)
            {
                if (_visited.Add(symbol)) {
                    if (symbol.TypeParameterKind == TypeParameterKind.Method || !_onlyMethodTypeParameters) {
                        if (!_typeParameters.Contains(symbol)) {
                            _typeParameters.Add(symbol);
                        }
                    }

                    foreach (var constraint in symbol.ConstraintTypes) {
                        constraint.Accept(this);
                    }
                }
            }
        }

        public static bool IsUnexpressableTypeParameterConstraint(this ITypeSymbol typeSymbol)
        {
            if (typeSymbol.IsSealed || typeSymbol.IsValueType) {
                return true;
            }

            switch (typeSymbol.TypeKind) {
                case TypeKind.Array:
                case TypeKind.Delegate:
                    return true;
            }

            switch (typeSymbol.SpecialType) {
                case SpecialType.System_Array:
                case SpecialType.System_Delegate:
                case SpecialType.System_MulticastDelegate:
                case SpecialType.System_Enum:
                case SpecialType.System_ValueType:
                    return true;
            }

            return false;
        }

        public static bool IsNumericType(this ITypeSymbol type)
        {
            if (type != null) {
                switch (type.SpecialType) {
                    case SpecialType.System_Byte:
                    case SpecialType.System_SByte:
                    case SpecialType.System_Int16:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_UInt64:
                    case SpecialType.System_Single:
                    case SpecialType.System_Double:
                    case SpecialType.System_Decimal:
                        return true;
                }
            }

            return false;
        }

        public static bool ContainsAnonymousType(this ITypeSymbol symbol)
        {
            return symbol.TypeSwitch(
                (IArrayTypeSymbol a) => ContainsAnonymousType(a.ElementType),
                (IPointerTypeSymbol p) => ContainsAnonymousType(p.PointedAtType),
                (INamedTypeSymbol n) => ContainsAnonymousType(n),
                _ => false);
        }

        private static bool ContainsAnonymousType(INamedTypeSymbol type)
        {
            if (type.IsAnonymousType) {
                return true;
            }

            foreach (var typeArg in type.GetAllTypeArguments()) {
                if (ContainsAnonymousType(typeArg)) {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSpecialType(this ITypeSymbol symbol)
        {
            if (symbol != null) {
                switch (symbol.SpecialType) {
                    case SpecialType.System_Object:
                    case SpecialType.System_Void:
                    case SpecialType.System_Boolean:
                    case SpecialType.System_SByte:
                    case SpecialType.System_Byte:
                    case SpecialType.System_Decimal:
                    case SpecialType.System_Single:
                    case SpecialType.System_Double:
                    case SpecialType.System_Int16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_Char:
                    case SpecialType.System_String:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_UInt64:
                        return true;
                }
            }

            return false;
        }

        public static bool CanSupportCollectionInitializer(this ITypeSymbol typeSymbol)
        {
            if (typeSymbol.AllInterfaces.Any(i => i.SpecialType == SpecialType.System_Collections_IEnumerable)) {
                var curType = typeSymbol;
                while (curType != null) {
                    if (HasAddMethod(curType))
                        return true;
                    curType = curType.BaseType;
                }
            }
            return false;
        }

        static bool HasAddMethod(ITypeSymbol typeSymbol)
        {
            return typeSymbol
                .GetMembers(WellKnownMemberNames.CollectionInitializerAddMethodName)
                .OfType<IMethodSymbol>().Any(m => m.Parameters.Any());
        }
        
        public static bool IsEnumType(this ITypeSymbol type)
        {
            return type.IsValueType && type.TypeKind == TypeKind.Enum;
        }

        public static ITypeSymbol RemoveNullableIfPresent(this ITypeSymbol symbol)
        {
            if (symbol.IsNullable()) {
                return symbol.GetTypeArguments().Single();
            }

            return symbol;
        }

        public static bool IsIEnumerable(this ITypeSymbol typeSymbol)
        {
            return typeSymbol.ImplementsSpecialTypeInterface(SpecialType.System_Collections_IEnumerable);
        }

        public static bool IsArrayOf(this ITypeSymbol t, SpecialType specialType)
        {
            return t is IArrayTypeSymbol ats && ats.ElementType.SpecialType == specialType;
        }
    }
}

