using Microsoft.CodeAnalysis;
using System.Linq;

namespace ICSharpCode.CodeConverter.Util.FromRoslyn
{
    /// <summary>
    /// Converted from https://github.com/dotnet/roslyn/blob/159707383710936bc0730a25be652081a2350878/src/EditorFeatures/VisualBasic/Utilities/NamedTypeSymbolExtensions.vb#L9-L41
    /// </summary>
    internal static class NamedTypeSymbolExtensions
    {
        /// <summary>
        /// Determines if the default constructor emitted by the compiler would contain an InitializeComponent() call.
        /// </summary>
        public static bool IsDesignerGeneratedTypeWithInitializeComponent(this INamedTypeSymbol type, Compilation compilation)
        {
            var designerGeneratedAttribute = compilation.DesignerGeneratedAttributeType();
            if (designerGeneratedAttribute == null) {
                return false;
            }

            if (!type.GetAttributes().Where(a => Equals(a.AttributeClass, designerGeneratedAttribute)).Any()) {
                return false;
            }

            // We now need to see if we have an InitializeComponent that matches the pattern. This is 
            // the same check as in Semantics::IsInitializeComponent in the old compiler.
            foreach (var baseType in type.GetBaseTypesAndThis()) {
                var possibleInitializeComponent = baseType.GetMembers("InitializeComponent").OfType<IMethodSymbol>().FirstOrDefault();
                if (possibleInitializeComponent?.IsAccessibleWithin(type) == true && !possibleInitializeComponent.Parameters.Any() && possibleInitializeComponent.ReturnsVoid && !possibleInitializeComponent.IsStatic) {
                    return true;
                }
            }

            return false;
        }
    }
}