using System.Linq;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Util
{
    internal static class IMethodSymbolExtensions
    {
        public static string GetParameterSignature(this IMethodSymbol methodSymbol)
        {
            return string.Join(" ", methodSymbol.Parameters.Select(p => p.Type));
        }
    }
}