using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Util.FromRoslyn;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Util
{
    internal static class ISymbolExtensions
    {
        // A lot of symbols in DateAndTime do not exist in DateTime, eg. DateSerial(),
        // and some have different names/arguments, eg. DateAdd(). This needs to be handled properly
        // as part of #174
        private static readonly string[] TypesToConvertToDateTime = new[] { "DateTime" };

        public const string ForcePartialTypesAssemblyName = "ProjectToBeConvertedWithPartialTypes";


        public static bool IsDefinedInSource(this ISymbol symbol)
        {
            return symbol.Locations.Any(loc => loc.IsInSource);
        }

        public static TSymbol ExtractBestMatch<TSymbol>(this SymbolInfo info, Func<TSymbol, bool> isMatch = null) where TSymbol : class, ISymbol
        {
            isMatch ??= (_ => true);
            if (info.Symbol == null && info.CandidateSymbols.Length == 0)
                return null;
            if (info.Symbol != null)
                return info.Symbol as TSymbol;
            var matches = info.CandidateSymbols.OfType<TSymbol>().Where(isMatch).ToList();
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

