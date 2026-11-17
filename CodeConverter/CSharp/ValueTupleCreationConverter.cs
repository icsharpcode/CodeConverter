using ICSharpCode.CodeConverter.Util.FromRoslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp;

/// <summary>
/// Converts <c>New ValueTuple(Of T1, T2)(a, b)</c> to the tuple literal <c>(a, b)</c>, and <c>New ValueTuple(Of T1, T2)()</c> to <c>default((T1, T2))</c>,
/// which create the same values. C# can't write <c>new (T1, T2)(a, b)</c> (CS8181), so anything this declines is created with <c>new ValueTuple&lt;T1, T2&gt;(a, b)</c>.
/// </summary>
/// <remarks>
/// A tuple literal's element types come from a target type if it has one, and from its elements otherwise (e.g. assigned to var).
/// Only a literal whose elements already have the VB type arguments keeps the same type in both cases.
/// It's declined for: a 1-tuple (no literal syntax), named arguments, object initializers,
/// and expression trees, which can't contain a tuple literal (CS8143).
/// </remarks>
internal class ValueTupleCreationConverter
{
    /// <summary>The position of TRest in <c>ValueTuple(Of T1, ..., T7, TRest)</c>, which a longer tuple literal nests its remaining elements in.</summary>
    private const int RestIndex = 7;

    private readonly SemanticModel _semanticModel;
    private readonly CommonConversions _commonConversions;

    public ValueTupleCreationConverter(SemanticModel semanticModel, CommonConversions commonConversions)
    {
        _semanticModel = semanticModel;
        _commonConversions = commonConversions;
    }

    public async Task<ExpressionSyntax> ConvertOrNullAsync(VBSyntax.ObjectCreationExpressionSyntax node)
    {
        if (GetPlainTupleCreationTypeOrNull(node) is not { } tupleType) return null;
        if (node.ArgumentList?.Arguments.Any() != true) return SyntaxFactory.DefaultExpression(_commonConversions.GetTypeSyntax(tupleType));
        if (_commonConversions.TriviaConvertingExpressionVisitor.IsWithinExpressionTree) return null;
        var elements = GetTupleElementsOrNull(node);
        if (elements is not {Count: > 1} || !elements.All(e => _commonConversions.TypeConversionAnalyzer.KeepsConvertedTypeWithoutTargetType(e.Expression))) return null;

        var arguments = await elements.SelectAsync(ConvertElementAsync);
        return SyntaxFactory.TupleExpression(SyntaxFactory.SeparatedList(arguments));
    }

    private async Task<ArgumentSyntax> ConvertElementAsync(VBSyntax.SimpleArgumentSyntax element)
    {
        var argument = await element.AcceptAsync<ArgumentSyntax>(_commonConversions.TriviaConvertingExpressionVisitor);
        return IsCastOfCast(argument.Expression) && GetIntegralConstantLiteralOrNull(element.Expression) is { } literal
            ? argument.WithExpression(literal.WithTriviaFrom(argument.Expression))
            : argument;
    }

    private static bool IsCastOfCast(ExpressionSyntax expression) =>
        expression.SkipIntoParens() is CastExpressionSyntax outerCast && outerCast.Expression.SkipIntoParens() is CastExpressionSyntax;

    /// <summary>
    /// Without a target type, an element needs its converted type explicitly, so a constant the VB code already converted (e.g. <c>CShort(1)</c> passed for an Integer)
    /// would be cast twice: <c>(int)(short)1</c>. VB evaluates that conversion at compile time, so the literal of its value in the converted type is the same value.
    /// Only integral constants converted to a type with its own literal form are written this way: they convert exactly, and the literal keeps the converted type.
    /// </summary>
    private ExpressionSyntax GetIntegralConstantLiteralOrNull(VBSyntax.ExpressionSyntax expression)
    {
        var typeInfo = _semanticModel.GetTypeInfo(expression);
        if (!typeInfo.Type.IsIntegralType() || !typeInfo.ConvertedType.IsIntegralType()) return null;
        if (_semanticModel.GetConstantValue(expression) is not {HasValue: true, Value: { } value}) return null;
        return LiteralConversions.ConvertLiteralNumericValueOrNull(value, typeInfo.ConvertedType) is { } convertedValue
            ? LiteralConversions.GetLiteralExpression(convertedValue, convertedType: typeInfo.ConvertedType)
            : null;
    }

    /// <summary>
    /// The arguments in element order, flattening a TRest created inline, since that's how a tuple literal with more than 7 elements is laid out.
    /// A TRest created without arguments has no elements to flatten: the literal would lose the nested tuple.
    /// </summary>
    private List<VBSyntax.SimpleArgumentSyntax> GetTupleElementsOrNull(VBSyntax.ObjectCreationExpressionSyntax node)
    {
        if (GetPlainTupleCreationTypeOrNull(node) is null) return null;
        if (node.ArgumentList?.Arguments.All(a => a is VBSyntax.SimpleArgumentSyntax {IsNamed: false}) != true) return null;

        var elements = node.ArgumentList.Arguments.Cast<VBSyntax.SimpleArgumentSyntax>().ToList();
        if (elements.Count <= RestIndex) return elements;

        var restElements = elements[RestIndex].Expression is VBSyntax.ObjectCreationExpressionSyntax restCreation && HasNoConversion(restCreation)
            ? GetTupleElementsOrNull(restCreation)
            : null;
        return restElements is {Count: > 0} ? elements.Take(RestIndex).Concat(restElements).ToList() : null;
    }

    /// <summary>The created tuple type, unless an object initializer sets members after construction.</summary>
    private ITypeSymbol GetPlainTupleCreationTypeOrNull(VBSyntax.ObjectCreationExpressionSyntax node) =>
        node.Initializer == null && _semanticModel.GetTypeInfo(node).Type is {IsTupleType: true} tupleType ? tupleType : null;

    private bool HasNoConversion(VBSyntax.ExpressionSyntax expression)
    {
        var typeInfo = _semanticModel.GetTypeInfo(expression);
        return SymbolEqualityComparer.Default.Equals(typeInfo.Type, typeInfo.ConvertedType);
    }
}
