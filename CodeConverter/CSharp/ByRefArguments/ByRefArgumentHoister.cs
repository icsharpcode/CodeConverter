using Microsoft.CodeAnalysis.Operations;

namespace ICSharpCode.CodeConverter.CSharp.ByRefArguments;

/// <summary>
/// C# can only pass a variable by reference, so a ByRef argument that isn't one gets a temporary, and VB's copy-back becomes a write-back statement.
/// </summary>
internal class ByRefArgumentHoister
{
    private readonly ITypeContext _typeContext;
    private readonly SemanticModel _semanticModel;
    private CommonConversions CommonConversions { get; }

    /// <summary>
    /// The write-backs of each argument list being converted, innermost call on top.
    /// </summary>
    private readonly Stack<List<AdditionalAssignment>> _writeBacksPerCall = new();

    public ByRefArgumentHoister(ITypeContext typeContext, SemanticModel semanticModel, CommonConversions commonConversions)
    {
        _typeContext = typeContext;
        _semanticModel = semanticModel;
        CommonConversions = commonConversions;
    }

    /// <summary>
    /// VB copies back a call's ByRef arguments from the last to the first (copyBackArray.ReverseContents() in Roslyn's LocalRewriter_Call.vb),
    /// which is observable when two arguments write back to the same place.
    /// A nested call has already written back by then, so only this call's own write-backs are reversed.
    /// </summary>
    public async Task<T> WithWriteBacksInVbOrderAsync<T>(Func<Task<T>> convertArguments)
    {
        _writeBacksPerCall.Push(new List<AdditionalAssignment>());
        T converted;
        try {
            converted = await convertArguments();
        } finally {
            var writeBacks = _writeBacksPerCall.Pop();
            foreach (var writeBack in Enumerable.Reverse(writeBacks)) _typeContext.PerScopeState.Hoist(writeBack);
        }
        return converted;
    }

    public CSSyntax.ExpressionSyntax HoistByRefDeclaration(VBSyntax.ExpressionSyntax node, CSSyntax.ExpressionSyntax refLValue, SemanticModelExtensions.RefConversion refType, string argName, RefKind refKind)
    {
        string prefix = $"arg{argName}";
        var expressionTypeInfo = _semanticModel.GetTypeInfo(node);
        bool useVar = expressionTypeInfo.Type?.Equals(expressionTypeInfo.ConvertedType, SymbolEqualityComparer.IncludeNullability) == true && !CommonConversions.ShouldPreferExplicitType(node, expressionTypeInfo.ConvertedType, out var _);
        var typeSyntax = CommonConversions.GetTypeSyntax(expressionTypeInfo.ConvertedType, useVar);

        if (refLValue is CSSyntax.ElementAccessExpressionSyntax eae) {
            //Hoist out the container so we can assign back to the same one after (like VB does)
            var tmpContainer = _typeContext.PerScopeState.Hoist(new AdditionalDeclaration("tmp", eae.Expression, ValidSyntaxFactory.VarType));
            refLValue = eae.WithExpression(tmpContainer.IdentifierName);
        }

        var withCast = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node, refLValue, defaultToCast: refKind != RefKind.None);

        var local = _typeContext.PerScopeState.Hoist(new AdditionalDeclaration(prefix, withCast, typeSyntax));

        if (refType == SemanticModelExtensions.RefConversion.PreAndPostAssignment) {
            var convertedLocalIdentifier = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node, local.IdentifierName, forceSourceType: expressionTypeInfo.ConvertedType, forceTargetType: expressionTypeInfo.Type);
            var writeBack = CreateWriteBack(node, refLValue, convertedLocalIdentifier);
            if (_writeBacksPerCall.Count > 0) _writeBacksPerCall.Peek().Add(writeBack);
            else _typeContext.PerScopeState.Hoist(writeBack);
        }

        return local.IdentifierName;
    }

    private AdditionalAssignment CreateWriteBack(VBSyntax.ExpressionSyntax vbTarget, CSSyntax.ExpressionSyntax csTarget, CSSyntax.ExpressionSyntax value)
    {
        if (_semanticModel.GetOperation(vbTarget) is IPropertyReferenceOperation { Arguments.Length: > 0 } pro && !VBasic.VisualBasicExtensions.IsDefault(pro.Property) &&
            csTarget is CSSyntax.InvocationExpressionSyntax getterCall) {
            return AdditionalAssignment.ViaSetter(CreateSetterCall(getterCall, pro.Property, value));
        }
        return new AdditionalAssignment(csTarget, value);
    }

    private static CSSyntax.InvocationExpressionSyntax CreateSetterCall(CSSyntax.InvocationExpressionSyntax getterCall, IPropertySymbol property, CSSyntax.ExpressionSyntax value)
    {
        var setterName = ValidSyntaxFactory.IdentifierName(property.SetMethod.Name);
        CSSyntax.ExpressionSyntax setter = getterCall.Expression switch {
            CSSyntax.MemberAccessExpressionSyntax maes => maes.WithName(setterName),
            CSSyntax.SimpleNameSyntax => setterName,
            var other => throw new NotSupportedException($"Parameterized property getter call {other.Kind()}")
        };
        return CS.SyntaxFactory.InvocationExpression(setter, CommonConversions.AddSetterValueArgument(getterCall.ArgumentList, property, value));
    }
}
