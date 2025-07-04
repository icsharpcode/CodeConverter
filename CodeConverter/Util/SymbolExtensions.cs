using System.Collections.Immutable;

namespace ICSharpCode.CodeConverter.Util;
#if NR6
    public
#endif
internal static class SymbolExtensions
{

    public static ImmutableArray<IParameterSymbol> GetParameters(this ISymbol symbol)
    {
        if (symbol == null)
            throw new ArgumentNullException(nameof(symbol));
        if (symbol is IMethodSymbol method)
            return method.Parameters;
        if (symbol is IPropertySymbol property)
            return property.Parameters;
        if (symbol is IEventSymbol ev)
            return ev.Type.GetDelegateInvokeMethod().Parameters;
        return ImmutableArray<IParameterSymbol>.Empty;
    }

    public static IParameterSymbol GetArgument(this ImmutableArray<IParameterSymbol> args, string name, int ordinal) => 
        name is not null ? GetArgument(args, name) : GetArgument(args, ordinal);

    public static IParameterSymbol GetArgument(this ImmutableArray<IParameterSymbol> args, string name) => args.FirstOrDefault(argSymbol => argSymbol.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public static IParameterSymbol GetArgument(this ImmutableArray<IParameterSymbol> args, int ordinal) => ordinal < args.Length ? args[ordinal] : null;

    public static bool IsConstructor(this ISymbol symbol)
    {
        if (symbol == null)
            throw new ArgumentNullException(nameof(symbol));
        return symbol is IMethodSymbol && ((IMethodSymbol)symbol).MethodKind == MethodKind.Constructor;
    }

    public static bool IsStaticConstructor(this ISymbol symbol)
    {
        if (symbol == null)
            throw new ArgumentNullException(nameof(symbol));
        return symbol is IMethodSymbol && ((IMethodSymbol)symbol).MethodKind == MethodKind.StaticConstructor;
    }

    public static bool IsDelegateType(this ISymbol symbol)
    {
        if (symbol == null)
            throw new ArgumentNullException(nameof(symbol));
        return symbol is ITypeSymbol && ((ITypeSymbol)symbol).TypeKind == TypeKind.Delegate;
    }

    public static ITypeSymbol GetReturnType(this ISymbol symbol)
    {
        if (symbol == null)
            throw new ArgumentNullException(nameof(symbol));
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

    public static bool IsAccessorWithValueInCsharp(this ISymbol symbol)
    {
        return symbol is IMethodSymbol ms &&
               new[] { MethodKind.PropertySet, MethodKind.EventAdd, MethodKind.EventRemove }.Contains(ms.MethodKind);
    }

    public static bool IsErrorType(this ISymbol symbol)
    {
        return
            symbol is ITypeSymbol &&
            ((ITypeSymbol)symbol).TypeKind == TypeKind.Error;
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

    public static bool AllWriteUsagesKnowable(this ISymbol symbol)
    {
        if (symbol == null) return false;
        return symbol.MatchesKind(SymbolKind.Local, SymbolKind.Parameter) || symbol.GetResultantVisibility() > SymbolVisibility.Public;
    }

    public static bool IsAnonymousType(this ISymbol symbol)
    {
        return symbol is INamedTypeSymbol && ((INamedTypeSymbol)symbol).IsAnonymousType;
    }


    public static ISymbol BaseMember(this ISymbol symbol)
    {
        return symbol.ExplicitInterfaceImplementations().FirstOrDefault() ?? symbol.OverriddenMember();
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

    public static bool HasOverloads(this ISymbol invocationSymbol)
    {
        if (invocationSymbol?.ContainingType is null) {
            return false;
        }

        return invocationSymbol.ContainingType
            .GetMembers()
            .Where(it => string.Equals(it.Name, invocationSymbol.Name, StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .Count() > 1;
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

        var eventSymnol = symbol as IEventSymbol;
        if (eventSymnol != null) {
            return eventSymnol.Type;
        }

        var aliasSymbol = symbol as IAliasSymbol;
        if (aliasSymbol != null) {
            return aliasSymbol.Target as ITypeSymbol;
        }

        return symbol as ITypeSymbol;
    }

    public static bool IsGenericMethod(this ISymbol symbol)
    {
        return symbol is IMethodSymbol ms && (ms.IsGenericMethod || ms.IsReducedTypeParameterMethod());
    }
    public static bool IsNonPublicInterfaceImplementation(this ISymbol declaredSymbol)
    {
        return declaredSymbol switch {
            IMethodSymbol methodSymbol => methodSymbol.DeclaredAccessibility != Accessibility.Public &&
                                          methodSymbol.ExplicitInterfaceImplementations.Any(),
            IPropertySymbol propertySymbol => propertySymbol.DeclaredAccessibility != Accessibility.Public &&
                                              propertySymbol.ExplicitInterfaceImplementations.Any(),
            _ => throw new ArgumentOutOfRangeException(nameof(declaredSymbol))
        };
    }

    public static bool IsRenamedInterfaceMember(this ISymbol declaredSymbol,
        SyntaxToken directlyConvertedCsIdentifier, IEnumerable<ISymbol> explicitInterfaceImplementations)
    {
        bool IsRenamed(ISymbol csIdentifier) =>
            declaredSymbol switch {
                IMethodSymbol methodSymbol => !StringComparer.OrdinalIgnoreCase.Equals(directlyConvertedCsIdentifier.Value, csIdentifier.Name) && methodSymbol.ExplicitInterfaceImplementations.Any(),
                IPropertySymbol propertySymbol => !StringComparer.OrdinalIgnoreCase.Equals(directlyConvertedCsIdentifier.Value, csIdentifier.Name) && propertySymbol.ExplicitInterfaceImplementations.Any(),
                _ => throw new ArgumentOutOfRangeException(nameof(declaredSymbol))
            };

        return explicitInterfaceImplementations.Any(IsRenamed);
    }
}

public enum SymbolVisibility
{
    Public,
    Internal,
    Private,
}