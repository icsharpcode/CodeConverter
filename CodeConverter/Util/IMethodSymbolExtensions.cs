namespace ICSharpCode.CodeConverter.Util;

internal static class IMethodSymbolExtensions
{
    public static string GetParameterSignature(this IMethodSymbol methodSymbol) => string.Join(" ", methodSymbol.Parameters.Select(p => p.Type));
    public static string GetParameterSignature(this IPropertySymbol propertySymbol) => string.Join(" ", propertySymbol.Parameters.Select(p => p.Type));

    public static (string Name, int TypeParameterCount, string ParameterTypes) GetUnqualifiedMethodOrPropertySignature(this ISymbol s, bool caseSensitiveName) => s switch {
        IMethodSymbol m => m.GetUnqualifiedMethodSignature(caseSensitiveName),
        IPropertySymbol p => p.GetUnqualifiedPropertySignature(caseSensitiveName),
        _ => throw new ArgumentOutOfRangeException(nameof(s), s, $"Symbol must be property or method, but was {s.Kind}")
    };

    public static (string Name, int TypeParameterCount, string ParameterTypes) GetUnqualifiedMethodSignature(this IMethodSymbol methodSymbol, bool caseSensitiveName) =>
        (caseSensitiveName ? methodSymbol.Name : methodSymbol.Name.ToLowerInvariant(), methodSymbol.TypeParameters.Length, GetParameterSignature(methodSymbol));

    public static (string Name, int TypeParameterCount, string ParameterTypes) GetUnqualifiedPropertySignature(this IPropertySymbol propertySymbol, bool caseSensitiveName) =>
        (caseSensitiveName ? propertySymbol.Name : propertySymbol.Name.ToLowerInvariant(), 0, GetParameterSignature(propertySymbol));

    public static bool ReturnsVoidOrAsyncTask(this IMethodSymbol enclosingMethodInfo) =>
        enclosingMethodInfo.ReturnsVoid || enclosingMethodInfo.IsAsync && enclosingMethodInfo.ReturnType.GetArity() == 0;
}