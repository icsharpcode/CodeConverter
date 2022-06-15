using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp;

internal class HoistedDefaultInitializedLoopVariable : IHoistedNode
{
    public string OriginalVariableName { get; }
    public string Id { get; }
    public ExpressionSyntax Initializer { get; }
    public TypeSyntax Type { get; }
    public bool Nested { get; }

    public HoistedDefaultInitializedLoopVariable(string originalVariableName, ExpressionSyntax initializer, TypeSyntax type, bool nested)
    {
        Debug.Assert(initializer is DefaultExpressionSyntax);
        OriginalVariableName = originalVariableName;
        Id = $"ph{Guid.NewGuid():N}";
        Initializer = initializer;
        Type = type;
        Nested = nested;
    }

}