using System.Linq;
using ICSharpCode.CodeConverter.Util.FromRoslyn;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Util
{
    internal static class IMethodSymbolExtensions
    {
        public static string GetParameterSignature(this IMethodSymbol methodSymbol)
        {
            return string.Join(" ", methodSymbol.Parameters.Select(p => p.Type));
        }

        public static (string Name, int TypeParameterCount, string ParameterTypes) GetUnqualifiedMethodSignature(this IMethodSymbol methodSymbol, bool caseSensitiveName)
        {
            return (caseSensitiveName ? methodSymbol.Name : methodSymbol.Name.ToLowerInvariant() , methodSymbol.TypeParameters.Length, GetParameterSignature(methodSymbol));
        }

        public static bool ReturnsVoidOrAsyncTask(this IMethodSymbol enclosingMethodInfo)
        {
            return enclosingMethodInfo.ReturnsVoid || enclosingMethodInfo.IsAsync && enclosingMethodInfo.ReturnType.GetArity() == 0;
        }
    }
}