using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Util
{
#if NR6
    public
#endif
    static class SymbolExtensions
    {
        /// <summary>
        /// Returns true if the symbol wasn't tagged with
        /// [System.ComponentModel.BrowsableAttribute (false)]
        /// </summary>
        /// <returns><c>true</c> if is designer browsable the specified symbol; otherwise, <c>false</c>.</returns>
        /// <param name="symbol">Symbol.</param>
        public static bool IsDesignerBrowsable(this ISymbol symbol)
        {
            if (symbol == null)
                throw new ArgumentNullException("symbol");
            var browsableState = symbol.GetAttributes().FirstOrDefault(attr => attr.AttributeClass.Name == "BrowsableAttribute" && attr.AttributeClass.ContainingNamespace.MetadataName == "System.ComponentModel");
            if (browsableState != null && browsableState.ConstructorArguments.Length == 1) {
                try {
                    return (bool)browsableState.ConstructorArguments[0].Value;
                } catch {
                }
            }
            return true;
        }

        /// <summary>
        /// Returns the component category.
        /// [System.ComponentModel.CategoryAttribute (CATEGORY)]
        /// </summary>
        /// <param name="symbol">Symbol.</param>
        public static string GetComponentCategory(this ISymbol symbol)
        {
            if (symbol == null)
                throw new ArgumentNullException("symbol");
            var browsableState = symbol.GetAttributes().FirstOrDefault(attr => attr.AttributeClass.Name == "CategoryAttribute" && attr.AttributeClass.ContainingNamespace.MetadataName == "System.ComponentModel");
            if (browsableState != null && browsableState.ConstructorArguments.Length == 1) {
                try {
                    return (string)browsableState.ConstructorArguments[0].Value;
                } catch {
                }
            }
            return null;
        }

        public static ImmutableArray<IParameterSymbol> GetParameters(this ISymbol symbol)
        {
            if (symbol == null)
                throw new ArgumentNullException("symbol");
            var method = symbol as IMethodSymbol;
            if (method != null)
                return method.Parameters;
            var property = symbol as IPropertySymbol;
            if (property != null)
                return property.Parameters;
            var ev = symbol as IEventSymbol;
            if (ev != null)
                return ev.Type.GetDelegateInvokeMethod().Parameters;
            return ImmutableArray<IParameterSymbol>.Empty;
        }

        public static ImmutableArray<ITypeParameterSymbol> GetTypeParameters(this ISymbol symbol)
        {
            if (symbol == null)
                throw new ArgumentNullException("symbol");
            var type = symbol as INamedTypeSymbol;
            if (type != null)
                return type.TypeParameters;
            var method = symbol as IMethodSymbol;
            if (method != null)
                return method.TypeParameters;
            return ImmutableArray<ITypeParameterSymbol>.Empty;
        }

        public static bool IsAnyConstructor(this ISymbol symbol)
        {
            if (symbol == null)
                throw new ArgumentNullException("symbol");
            var method = symbol as IMethodSymbol;
            return method != null && (method.MethodKind == MethodKind.Constructor || method.MethodKind == MethodKind.StaticConstructor);
        }

        public static bool IsConstructor(this ISymbol symbol)
        {
            if (symbol == null)
                throw new ArgumentNullException("symbol");
            return symbol is IMethodSymbol && ((IMethodSymbol)symbol).MethodKind == MethodKind.Constructor;
        }

        public static bool IsStaticConstructor(this ISymbol symbol)
        {
            if (symbol == null)
                throw new ArgumentNullException("symbol");
            return symbol is IMethodSymbol && ((IMethodSymbol)symbol).MethodKind == MethodKind.StaticConstructor;
        }

        public static bool IsDestructor(this ISymbol symbol)
        {
            if (symbol == null)
                throw new ArgumentNullException("symbol");
            return symbol is IMethodSymbol && ((IMethodSymbol)symbol).MethodKind == MethodKind.Destructor;
        }

        public static bool IsDelegateType(this ISymbol symbol)
        {
            if (symbol == null)
                throw new ArgumentNullException("symbol");
            return symbol is ITypeSymbol && ((ITypeSymbol)symbol).TypeKind == TypeKind.Delegate;
        }

        public static ITypeSymbol GetReturnType(this ISymbol symbol)
        {
            if (symbol == null)
                throw new ArgumentNullException("symbol");
            switch (symbol.Kind) {
                case SymbolKind.Field:
                    var field = (IFieldSymbol)symbol;
                    return field.Type;
                case SymbolKind.Method:
                    var method = (IMethodSymbol)symbol;
                    if (method.MethodKind == MethodKind.Constructor)
                        return method.ContainingType;
                    return method.ReturnType;
                case SymbolKind.Property:
                    var property = (IPropertySymbol)symbol;
                    return property.Type;
                case SymbolKind.Event:
                    var evt = (IEventSymbol)symbol;
                    return evt.Type;
                case SymbolKind.Parameter:
                    var param = (IParameterSymbol)symbol;
                    return param.Type;
                case SymbolKind.Local:
                    var local = (ILocalSymbol)symbol;
                    return local.Type;
            }
            return null;
        }

        public static bool IsType(this ISymbol symbol)
        {
            var typeSymbol = symbol as ITypeSymbol;
            return typeSymbol != null && typeSymbol.IsType;
        }

        public static bool IsAccessorMethod(this ISymbol symbol)
        {
            var accessorSymbol = symbol as IMethodSymbol;
            return accessorSymbol != null &&
                (accessorSymbol.MethodKind == MethodKind.PropertySet || accessorSymbol.MethodKind == MethodKind.PropertyGet ||
                    accessorSymbol.MethodKind == MethodKind.EventRemove || accessorSymbol.MethodKind == MethodKind.EventAdd);
        }

        public static bool IsAccessorPropertySet(this ISymbol symbol)
        {
            var accessorSymbol = symbol as IMethodSymbol;
            return accessorSymbol != null && accessorSymbol.MethodKind == MethodKind.PropertySet;
        }

        public static bool IsPublic(this ISymbol symbol)
        {
            return symbol.DeclaredAccessibility == Accessibility.Public;
        }

        public static bool IsErrorType(this ISymbol symbol)
        {
            return
                symbol is ITypeSymbol &&
                ((ITypeSymbol)symbol).TypeKind == TypeKind.Error;
        }

        public static bool IsIndexer(this ISymbol symbol)
        {
            return (symbol as IPropertySymbol)?.IsIndexer == true;
        }

        public static bool IsUserDefinedOperator(this ISymbol symbol)
        {
            return (symbol as IMethodSymbol)?.MethodKind == MethodKind.UserDefinedOperator;
        }

        public static SymbolVisibility GetResultantVisibility(this ISymbol symbol)
        {
            // Start by assuming it's visible.
            var visibility = SymbolVisibility.Public;

            switch (symbol.Kind) {
                case SymbolKind.Alias:
                    // Aliases are uber private.  They're only visible in the same file that they
                    // were declared in.
                    return SymbolVisibility.Private;

                case SymbolKind.Parameter:
                    // Parameters are only as visible as their containing symbol
                    return GetResultantVisibility(symbol.ContainingSymbol);

                case SymbolKind.TypeParameter:
                    // Type Parameters are private.
                    return SymbolVisibility.Private;
            }

            while (symbol != null && symbol.Kind != SymbolKind.Namespace) {
                switch (symbol.DeclaredAccessibility) {
                    // If we see anything private, then the symbol is private.
                    case Accessibility.NotApplicable:
                    case Accessibility.Private:
                        return SymbolVisibility.Private;

                    // If we see anything internal, then knock it down from public to
                    // internal.
                    case Accessibility.Internal:
                    case Accessibility.ProtectedAndInternal:
                        visibility = SymbolVisibility.Internal;
                        break;

                        // For anything else (Public, Protected, ProtectedOrInternal), the
                        // symbol stays at the level we've gotten so far.
                }

                symbol = symbol.ContainingSymbol;
            }

            return visibility;
        }

        public static bool IsAnonymousType(this ISymbol symbol)
        {
            return symbol is INamedTypeSymbol && ((INamedTypeSymbol)symbol).IsAnonymousType;
        }

        public static ISymbol OverriddenMember(this ISymbol symbol)
        {
            switch (symbol.Kind) {
                case SymbolKind.Event:
                    return ((IEventSymbol)symbol).OverriddenEvent;

                case SymbolKind.Method:
                    return ((IMethodSymbol)symbol).OverriddenMethod;

                case SymbolKind.Property:
                    return ((IPropertySymbol)symbol).OverriddenProperty;

                case SymbolKind.NamedType:
                    return ((INamedTypeSymbol)symbol).BaseType;
            }

            return null;
        }

        public static ImmutableArray<ISymbol> ExplicitInterfaceImplementations(this ISymbol symbol)
        {
            return symbol.TypeSwitch(
                (IEventSymbol @event) => @event.ExplicitInterfaceImplementations.As<ISymbol>(),
                (IMethodSymbol method) => method.ExplicitInterfaceImplementations.As<ISymbol>(),
                (IPropertySymbol property) => property.ExplicitInterfaceImplementations.As<ISymbol>(),
                _ => ImmutableArray.Create<ISymbol>());
        }

        public static bool IsOverridable(this ISymbol symbol)
        {
            return
                symbol != null &&
                symbol.ContainingType != null &&
                symbol.ContainingType.TypeKind == TypeKind.Class &&
                (symbol.IsVirtual || symbol.IsAbstract || symbol.IsOverride) &&
                !symbol.IsSealed;
        }

        public static bool IsImplementable(this ISymbol symbol)
        {
            if (symbol != null &&
                symbol.ContainingType != null &&
                symbol.ContainingType.TypeKind == TypeKind.Interface) {
                if (symbol.Kind == SymbolKind.Event) {
                    return true;
                }

                if (symbol.Kind == SymbolKind.Property) {
                    return true;
                }

                if (symbol.Kind == SymbolKind.Method && ((IMethodSymbol)symbol).MethodKind == MethodKind.Ordinary) {
                    return true;
                }
            }

            return false;
        }

        public static INamedTypeSymbol GetContainingTypeOrThis(this ISymbol symbol)
        {
            if (symbol is INamedTypeSymbol) {
                return (INamedTypeSymbol)symbol;
            }

            return symbol.ContainingType;
        }

        public static bool IsPointerType(this ISymbol symbol)
        {
            return symbol is IPointerTypeSymbol;
        }

        public static bool IsModuleType(this ISymbol symbol)
        {
            return (symbol as ITypeSymbol)?.IsModuleType() == true;
        }

        public static bool IsInterfaceType(this ISymbol symbol)
        {
            return (symbol as ITypeSymbol)?.IsInterfaceType() == true;
        }

        public static bool IsArrayType(this ISymbol symbol)
        {
            return symbol?.Kind == SymbolKind.ArrayType;
        }

        public static bool IsAnonymousFunction(this ISymbol symbol)
        {
            return (symbol as IMethodSymbol)?.MethodKind == MethodKind.AnonymousFunction;
        }

        public static bool IsKind(this ISymbol symbol, SymbolKind kind)
        {
            return symbol.MatchesKind(kind);
        }

        public static bool MatchesKind(this ISymbol symbol, SymbolKind kind)
        {
            return symbol?.Kind == kind;
        }

        public static bool MatchesKind(this ISymbol symbol, SymbolKind kind1, SymbolKind kind2)
        {
            return symbol != null
                && (symbol.Kind == kind1 || symbol.Kind == kind2);
        }

        public static bool MatchesKind(this ISymbol symbol, SymbolKind kind1, SymbolKind kind2, SymbolKind kind3)
        {
            return symbol != null
                && (symbol.Kind == kind1 || symbol.Kind == kind2 || symbol.Kind == kind3);
        }

        public static bool MatchesKind(this ISymbol symbol, params SymbolKind[] kinds)
        {
            return symbol != null
                && kinds.Contains(symbol.Kind);
        }

        public static bool IsReducedExtension(this ISymbol symbol)
        {
            return symbol is IMethodSymbol && ((IMethodSymbol)symbol).MethodKind == MethodKind.ReducedExtension;
        }

        public static bool IsExtensionMethod(this ISymbol symbol)
        {
            return symbol.Kind == SymbolKind.Method && ((IMethodSymbol)symbol).IsExtensionMethod;
        }

        public static bool IsModuleMember(this ISymbol symbol)
        {
            return symbol != null && symbol.ContainingSymbol is INamedTypeSymbol && symbol.ContainingType.TypeKind == TypeKind.Module;
        }

        public static bool IsConversion(this ISymbol symbol)
        {
            return (symbol as IMethodSymbol)?.MethodKind == MethodKind.Conversion;
        }

        public static bool IsOrdinaryMethod(this ISymbol symbol)
        {
            return (symbol as IMethodSymbol)?.MethodKind == MethodKind.Ordinary;
        }

        public static bool IsNormalAnonymousType(this ISymbol symbol)
        {
            return symbol.IsAnonymousType() && !symbol.IsDelegateType();
        }

        public static bool IsAnonymousDelegateType(this ISymbol symbol)
        {
            return symbol.IsAnonymousType() && symbol.IsDelegateType();
        }

        public static bool IsAnonymousTypeProperty(this ISymbol symbol)
        {
            return symbol is IPropertySymbol && symbol.ContainingType.IsNormalAnonymousType();
        }

        public static bool IsWriteableFieldOrProperty(this ISymbol symbol)
        {
            var fieldSymbol = symbol as IFieldSymbol;
            if (fieldSymbol != null) {
                return !fieldSymbol.IsReadOnly
                    && !fieldSymbol.IsConst;
            }

            var propertySymbol = symbol as IPropertySymbol;
            if (propertySymbol != null) {
                return !propertySymbol.IsReadOnly;
            }

            return false;
        }

        public static ITypeSymbol GetMemberType(this ISymbol symbol)
        {
            switch (symbol.Kind) {
                case SymbolKind.Field:
                    return ((IFieldSymbol)symbol).Type;
                case SymbolKind.Property:
                    return ((IPropertySymbol)symbol).Type;
                case SymbolKind.Method:
                    return ((IMethodSymbol)symbol).ReturnType;
                case SymbolKind.Event:
                    return ((IEventSymbol)symbol).Type;
            }

            return null;
        }

        public static int GetArity(this ISymbol symbol)
        {
            switch (symbol.Kind) {
                case SymbolKind.NamedType:
                    return ((INamedTypeSymbol)symbol).Arity;
                case SymbolKind.Method:
                    return ((IMethodSymbol)symbol).Arity;
                default:
                    return 0;
            }
        }

        public static ISymbol GetOriginalUnreducedDefinition(this ISymbol symbol)
        {
            if (symbol.IsReducedExtension()) {
                // note: ReducedFrom is only a method definition and includes no type arguments.
                symbol = ((IMethodSymbol)symbol).GetConstructedReducedFrom();
            }

            if (symbol.IsFunctionValue()) {
                var method = symbol.ContainingSymbol as IMethodSymbol;
                if (method != null) {
                    symbol = method;

                    if (method.AssociatedSymbol != null) {
                        symbol = method.AssociatedSymbol;
                    }
                }
            }

            if (symbol.IsNormalAnonymousType() || symbol.IsAnonymousTypeProperty()) {
                return symbol;
            }

            var parameter = symbol as IParameterSymbol;
            if (parameter != null) {
                var method = parameter.ContainingSymbol as IMethodSymbol;
                if (method?.IsReducedExtension() == true) {
                    symbol = method.GetConstructedReducedFrom().Parameters[parameter.Ordinal + 1];
                }
            }

            return symbol?.OriginalDefinition;
        }

        public static bool IsFunctionValue(this ISymbol symbol)
        {
            return symbol is ILocalSymbol && ((ILocalSymbol)symbol).IsFunctionValue;
        }

        public static bool IsThisParameter(this ISymbol symbol)
        {
            return symbol != null && symbol.Kind == SymbolKind.Parameter && ((IParameterSymbol)symbol).IsThis;
        }

        public static ISymbol ConvertThisParameterToType(this ISymbol symbol)
        {
            if (symbol.IsThisParameter()) {
                return ((IParameterSymbol)symbol).Type;
            }

            return symbol;
        }

        public static bool IsParams(this ISymbol symbol)
        {
            var parameters = symbol.GetParameters();
            return parameters.Length > 0 && parameters[parameters.Length - 1].IsParams;
        }

        public static ImmutableArray<ITypeSymbol> GetTypeArguments(this ISymbol symbol)
        {
            return symbol.TypeSwitch(
                (IMethodSymbol m) => m.TypeArguments,
                (INamedTypeSymbol nt) => nt.TypeArguments,
                _ => ImmutableArray.Create<ITypeSymbol>());
        }

        public static ImmutableArray<ITypeSymbol> GetAllTypeArguments(this ISymbol symbol)
        {
            var results = new List<ITypeSymbol>(symbol.GetTypeArguments());

            var containingType = symbol.ContainingType;
            while (containingType != null) {
                results.AddRange(containingType.GetTypeArguments());
                containingType = containingType.ContainingType;
            }

            return ImmutableArray.CreateRange(results);
        }

        public static bool IsAttribute(this ISymbol symbol)
        {
            return (symbol as ITypeSymbol)?.IsAttribute() == true;
        }

        public static bool IsStaticType(this ISymbol symbol)
        {
            return symbol != null && symbol.Kind == SymbolKind.NamedType && symbol.IsStatic;
        }

        public static bool IsNamespace(this ISymbol symbol)
        {
            return symbol?.Kind == SymbolKind.Namespace;
        }
        public static IEnumerable<IPropertySymbol> GetValidAnonymousTypeProperties(this ISymbol symbol)
        {
            // Contract.ThrowIfFalse(symbol.IsNormalAnonymousType());
            return ((INamedTypeSymbol)symbol).GetMembers().OfType<IPropertySymbol>().Where(p => p.CanBeReferencedByName);
        }

        public static Accessibility ComputeResultantAccessibility(this ISymbol symbol, ITypeSymbol finalDestination)
        {
            if (symbol == null) {
                return Accessibility.Private;
            }

            switch (symbol.DeclaredAccessibility) {
                default:
                    return symbol.DeclaredAccessibility;
                case Accessibility.ProtectedAndInternal:
                    return symbol.ContainingAssembly.GivesAccessTo(finalDestination.ContainingAssembly)
                        ? Accessibility.ProtectedAndInternal
                            : Accessibility.Internal;
                case Accessibility.ProtectedOrInternal:
                    return symbol.ContainingAssembly.GivesAccessTo(finalDestination.ContainingAssembly)
                        ? Accessibility.ProtectedOrInternal
                            : Accessibility.Protected;
            }
        }

        /// <returns>
        /// Returns true if symbol is a local variable and its declaring syntax node is
        /// after the current position, false otherwise (including for non-local symbols)
        /// </returns>
        public static bool IsInaccessibleLocal(this ISymbol symbol, int position)
        {
            if (symbol.Kind != SymbolKind.Local) {
                return false;
            }

            // Implicitly declared locals (with Option Explicit Off in VB) are scoped to the entire
            // method and should always be considered accessible from within the same method.
            if (symbol.IsImplicitlyDeclared) {
                return false;
            }

            var declarationSyntax = symbol.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).FirstOrDefault();
            return declarationSyntax != null && position < declarationSyntax.SpanStart;
        }

        public static bool IsEventAccessor(this ISymbol symbol)
        {
            var method = symbol as IMethodSymbol;
            return method != null &&
                (method.MethodKind == MethodKind.EventAdd ||
                    method.MethodKind == MethodKind.EventRaise ||
                    method.MethodKind == MethodKind.EventRemove);
        }

        public static ITypeSymbol GetSymbolType(this ISymbol symbol)
        {
            var localSymbol = symbol as ILocalSymbol;
            if (localSymbol != null) {
                return localSymbol.Type;
            }

            var fieldSymbol = symbol as IFieldSymbol;
            if (fieldSymbol != null) {
                return fieldSymbol.Type;
            }

            var propertySymbol = symbol as IPropertySymbol;
            if (propertySymbol != null) {
                return propertySymbol.Type;
            }

            var parameterSymbol = symbol as IParameterSymbol;
            if (parameterSymbol != null) {
                return parameterSymbol.Type;
            }

            var aliasSymbol = symbol as IAliasSymbol;
            if (aliasSymbol != null) {
                return aliasSymbol.Target as ITypeSymbol;
            }

            return symbol as ITypeSymbol;
        }
    }

    public enum SymbolVisibility
    {
        Public,
        Internal,
        Private,
    }

}