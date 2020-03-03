using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Util.FromRoslyn
{
    internal static class CompilationExtensions
    {
        /// <remarks>
        /// Matches https://github.com/dotnet/roslyn/blob/159707383710936bc0730a25be652081a2350878/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/Core/Extensions/ICompilationExtensions.cs#L82-L83
        /// </remarks>
        public static INamedTypeSymbol DesignerGeneratedAttributeType(this Compilation compilation)
            => compilation.GetTypeByMetadataName("Microsoft.VisualBasic.CompilerServices.DesignerGeneratedAttribute");

        public static INamedTypeSymbol ExpressionOfTType(this Compilation compilation)
            => compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression`1");
    }
}