using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace ICSharpCode.CodeConverter.Util
{
#if NR6
    public
#endif
    static class ISymbolExtensions
    {
        // A lot of symbols in DateAndTime do not exist in DateTime, eg. DateSerial(),
        // and some have different names/arguments, eg. DateAdd(). This needs to be handled properly
        // as part of #174
        private static readonly string[] TypesToConvertToDateTime = new[] { "DateTime" };

        public const string ForcePartialTypesAssemblyName = "ProjectToBeConvertedWithPartialTypes";

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

        public static ISymbol ExtractBestMatch(this SymbolInfo info, Func<ISymbol, bool> isMatch = null)
        {
            isMatch = isMatch ?? (_ => true);
            if (info.Symbol == null && info.CandidateSymbols.Length == 0)
                return null;
            if (info.Symbol != null)
                return info.Symbol;
            var matches = info.CandidateSymbols.Where(isMatch).ToList();
            if (matches.Count == 1) {
                return matches.Single();
            }

            return null;
        }

        public static string ToMinimalCSharpDisplayString(this ITypeSymbol symbol, SemanticModel vbSemanticModel, int position, SymbolDisplayFormat format = null)
        {
            if (TryGetSpecialVBTypeConversion(symbol, out var cSharpDisplayString)) return cSharpDisplayString;
            var minimalCSharpDisplayString = symbol.ToMinimalDisplayString(vbSemanticModel, position, format);
            var parentTypes = symbol.FollowProperty(t => t?.ContainingSymbol as ITypeSymbol).Count() - 1;
            // Workaround: The above call can return a name qualified by a partial namespace, e.g. IO.Path if System is already imported.
            // So check that we don't end up qualifying with non-type symbols by checking the number of dots
            return minimalCSharpDisplayString.Count(x => x == '.') > parentTypes ? ToCSharpDisplayString(symbol, format) : minimalCSharpDisplayString;
        }

        public static string ToCSharpDisplayString(this ISymbol symbol, SymbolDisplayFormat format = null)
        {
            if (TryGetSpecialVBTypeConversion(symbol, out var cSharpDisplayString)) return cSharpDisplayString;

            return symbol.ToDisplayString(format);
        }

        private static bool TryGetSpecialVBTypeConversion(ISymbol symbol, out string cSharpDisplayString)
        {
            var containingNamespace = symbol?.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
            if (containingNamespace == "Microsoft.VisualBasic" || containingNamespace == "System") {
                if (symbol is ITypeSymbol && TypesToConvertToDateTime.Contains(symbol.Name)) {
                    {
                        cSharpDisplayString = "DateTime";
                        return true;
                    }
                } else if (TypesToConvertToDateTime.Contains(symbol.ContainingType?.Name)) {
                    {
                        cSharpDisplayString = "DateTime" + "." + symbol.Name;
                        return true;
                    }
                }
            }

            cSharpDisplayString = null;
            return false;
        }

        private static bool ShouldConvertToDateTime(ITypeSymbol symbol, string fullName)
        {
            return TypesToConvertToDateTime.Contains(symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
        }

        public static bool IsPartialMethodImplementation(this ISymbol declaredSymbol)
        {
            return declaredSymbol is IMethodSymbol ms && ms.PartialDefinitionPart != null;
        }

        public static bool IsPartialMethodDefinition(this ISymbol declaredSymbol)
        {
            return declaredSymbol is IMethodSymbol ms && ms.PartialImplementationPart != null;
        }

        public static bool IsPartialClassDefinition(this ISymbol declaredSymbol)
        {
            return declaredSymbol is ITypeSymbol ts && (ts.DeclaringSyntaxReferences.Length > 1
                || ts.ContainingAssembly.Name == ForcePartialTypesAssemblyName);
        }
    }
}

