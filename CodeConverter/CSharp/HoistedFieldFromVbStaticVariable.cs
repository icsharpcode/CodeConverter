using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp;

internal class HoistedFieldFromVbStaticVariable : IHoistedNode
{
    public string OriginalMethodName { get; }
    public string OriginalVariableName { get; }
    public ExpressionSyntax Initializer { get; }
    public TypeSyntax Type { get; }
    public bool IsStatic { get; }

    public HoistedFieldFromVbStaticVariable(string originalMethodName, string originalVariableName, ExpressionSyntax initializer, TypeSyntax type, bool isStatic)
    {
        OriginalMethodName = originalMethodName;
        OriginalVariableName = originalVariableName;
        Initializer = initializer;
        Type = type;
        IsStatic = isStatic;
    }

    public string FieldName => OriginalMethodName != null ? $"_{OriginalMethodName}_{OriginalVariableName}" : $"_{OriginalVariableName}";
}