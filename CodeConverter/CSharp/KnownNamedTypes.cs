namespace ICSharpCode.CodeConverter.CSharp;

internal class KnownNamedTypes
{
    public KnownNamedTypes(SemanticModel semanticModel)
    {
        Boolean = semanticModel.Compilation.GetTypeByMetadataName("System.Boolean");
        String = semanticModel.Compilation.GetTypeByMetadataName("System.String");
        DefaultParameterValueAttribute = semanticModel.Compilation.GetTypeByMetadataName("System.Runtime.InteropServices.DefaultParameterValueAttribute");
        OptionalAttribute = semanticModel.Compilation.GetTypeByMetadataName("System.Runtime.InteropServices.OptionalAttribute");
        System_Linq_Expressions_Expression_T = semanticModel.Compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression`1");
        VbCompilerStringType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.VisualBasic.CompilerServices.StringType");
    }

    public INamedTypeSymbol System_Linq_Expressions_Expression_T { get; set; }

    public INamedTypeSymbol Boolean { get; }
    public INamedTypeSymbol String { get; }
    public INamedTypeSymbol DefaultParameterValueAttribute { get; }
    public INamedTypeSymbol OptionalAttribute { get; }
    public INamedTypeSymbol VbCompilerStringType { get; }
}