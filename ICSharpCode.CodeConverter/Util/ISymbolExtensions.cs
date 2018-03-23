using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace ICSharpCode.CodeConverter.Util
{
#if NR6
    public
#endif
    static class ISymbolExtensions
    {
        /// <summary>
        /// Checks if 'symbol' is accessible from within 'within'.
        /// </summary>
        public static bool IsAccessibleWithin(
            this ISymbol symbol,
            ISymbol within,
            ITypeSymbol throughTypeOpt = null)
        {
            if (within is IAssemblySymbol) {
                return symbol.IsAccessibleWithin((IAssemblySymbol)within, throughTypeOpt);
            } else if (within is INamedTypeSymbol) {
                return symbol.IsAccessibleWithin((INamedTypeSymbol)within, throughTypeOpt);
            } else {
                throw new ArgumentException();
            }
        }

        // Is a top-level type with accessibility "declaredAccessibility" inside assembly "assembly"
        // accessible from "within", which must be a named type of an assembly.
        private static bool IsNonNestedTypeAccessible(
            IAssemblySymbol assembly,
            Accessibility declaredAccessibility,
            ISymbol within)
        {
            //            Contract.Requires(within is INamedTypeSymbol || within is IAssemblySymbol);
            //            Contract.ThrowIfNull(assembly);
            var withinAssembly = (within as IAssemblySymbol) ?? ((INamedTypeSymbol)within).ContainingAssembly;

            switch (declaredAccessibility) {
                case Accessibility.NotApplicable:
                case Accessibility.Public:
                    // Public symbols are always accessible from any context
                    return true;

                case Accessibility.Private:
                case Accessibility.Protected:
                case Accessibility.ProtectedAndInternal:
                    // Shouldn't happen except in error cases.
                    return false;

                case Accessibility.Internal:
                case Accessibility.ProtectedOrInternal:
                    // An internal type is accessible if we're in the same assembly or we have
                    // friend access to the assembly it was defined in.
                    return withinAssembly.IsSameAssemblyOrHasFriendAccessTo(assembly);

                default:
                    throw new Exception("unreachable");
            }
        }

        // Is a private symbol access
        private static bool IsPrivateSymbolAccessible(
            ISymbol within,
            INamedTypeSymbol originalContainingType)
        {
            //Contract.Requires(within is INamedTypeSymbol || within is IAssemblySymbol);

            var withinType = within as INamedTypeSymbol;
            if (withinType == null) {
                // If we're not within a type, we can't access a private symbol
                return false;
            }

            // A private symbol is accessible if we're (optionally nested) inside the type that it
            // was defined in.
            return IsNestedWithinOriginalContainingType(withinType, originalContainingType);
        }

        // Is the type "withinType" nested within the original type "originalContainingType".
        private static bool IsNestedWithinOriginalContainingType(
            INamedTypeSymbol withinType,
            INamedTypeSymbol originalContainingType)
        {
            //            Contract.ThrowIfNull(withinType);
            //            Contract.ThrowIfNull(originalContainingType);

            // Walk up my parent chain and see if I eventually hit the owner.  If so then I'm a
            // nested type of that owner and I'm allowed access to everything inside of it.
            var current = withinType.OriginalDefinition;
            while (current != null) {
                //Contract.Requires(current.IsDefinition);
                if (current.Equals(originalContainingType)) {
                    return true;
                }

                // NOTE(cyrusn): The container of an 'original' type is always original.
                current = current.ContainingType;
            }

            return false;
        }

        public static bool IsDefinedInMetadata(this ISymbol symbol)
        {
            return symbol.Locations.Any(loc => loc.IsInMetadata);
        }

        public static bool IsDefinedInSource(this ISymbol symbol)
        {
            return symbol.Locations.All(loc => loc.IsInSource);
        }

        public static DeclarationModifiers GetSymbolModifiers(this ISymbol symbol)
        {
            // ported from roslyn source - why they didn't use DeclarationModifiers.From (symbol) ?
            return DeclarationModifiers.None
                .WithIsStatic(symbol.IsStatic)
                .WithIsAbstract(symbol.IsAbstract)
                .WithIsUnsafe(symbol.IsUnsafe())
                .WithIsVirtual(symbol.IsVirtual)
                .WithIsOverride(symbol.IsOverride)
                .WithIsSealed(symbol.IsSealed);
        }

        public static IEnumerable<SyntaxReference> GetDeclarations(this ISymbol symbol)
        {
            return symbol != null
                ? symbol.DeclaringSyntaxReferences.AsEnumerable()
                    : SpecializedCollections.EmptyEnumerable<SyntaxReference>();
        }

        public static ISymbol GetContainingMemberOrThis(this ISymbol symbol)
        {
            if (symbol == null)
                return null;
            switch (symbol.Kind) {
                case SymbolKind.Assembly:
                case SymbolKind.NetModule:
                case SymbolKind.Namespace:
                case SymbolKind.Preprocessing:
                case SymbolKind.Alias:
                case SymbolKind.ArrayType:
                case SymbolKind.DynamicType:
                case SymbolKind.ErrorType:
                case SymbolKind.NamedType:
                case SymbolKind.PointerType:
                case SymbolKind.Label:
                    throw new NotSupportedException();
                case SymbolKind.Field:
                case SymbolKind.Property:
                case SymbolKind.Event:
                    return symbol;
                case SymbolKind.Method:
                    if (symbol.IsAccessorMethod())
                        return ((IMethodSymbol)symbol).AssociatedSymbol;
                    return symbol;
                case SymbolKind.Local:
                case SymbolKind.Parameter:
                case SymbolKind.TypeParameter:
                case SymbolKind.RangeVariable:
                    return GetContainingMemberOrThis(symbol.ContainingSymbol);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static ISymbol ExtractBestMatch(this SymbolInfo info)
        {
            if (info.Symbol == null && info.CandidateSymbols.Length == 0)
                return null;
            if (info.Symbol != null)
                return info.Symbol;
            if (info.CandidateSymbols.Length == 1)
                return info.CandidateSymbols[0];
            return null;
        }
    }
}

