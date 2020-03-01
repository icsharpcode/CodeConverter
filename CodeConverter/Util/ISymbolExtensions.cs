using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Util
{
#if NR6
    public
#endif
    internal static class ISymbolExtensions
    {
        // A lot of symbols in DateAndTime do not exist in DateTime, eg. DateSerial(),
        // and some have different names/arguments, eg. DateAdd(). This needs to be handled properly
        // as part of #174
        private static readonly string[] TypesToConvertToDateTime = new[] { "DateTime" };

        public const string ForcePartialTypesAssemblyName = "ProjectToBeConvertedWithPartialTypes";

        public static bool IsDefinedInMetadata(this ISymbol symbol)
        {
            return symbol.Locations.Any(loc => loc.IsInMetadata);
        }

        public static bool IsDefinedInSource(this ISymbol symbol)
        {
            return symbol.Locations.Any(loc => loc.IsInSource);
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

        public static bool CanHaveMethodBody(this ISymbol declaredSymbol)
        {
            return !(declaredSymbol is IMethodSymbol ms) || ms.PartialImplementationPart == null && !ms.IsExtern;
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

        public static bool IsReducedTypeParameterMethod(this ISymbol symbol)
        {
            return symbol is IMethodSymbol ms && ms.ReducedFrom?.TypeParameters.Count() > ms.TypeParameters.Count();
        }
    }
}

