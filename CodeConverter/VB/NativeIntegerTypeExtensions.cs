namespace ICSharpCode.CodeConverter.VB;

/// <summary>
/// C# <c>nint</c>/<c>nuint</c> are contextual keywords for System.IntPtr/System.UIntPtr (the same types since C# 11),
/// and Roslyn displays those types with them, e.g. in the casts that Simplifier.Expand adds.
/// VB has no keyword for them, and there is no System.nint type to qualify them with.
/// </summary>
internal static class NativeIntegerTypeExtensions
{
    public static ITypeSymbol GetNativeIntegerTypeOrNull(this SemanticModel semanticModel, CSSyntax.IdentifierNameSyntax name)
    {
        if (!name.IsNint && !name.IsNuint || name.SyntaxTree != semanticModel.SyntaxTree) return null;
        return semanticModel.GetSymbolInfo(name).Symbol is ITypeSymbol { IsNativeIntegerType: true } type ? type : null;
    }
}
