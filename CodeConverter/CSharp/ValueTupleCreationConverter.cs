using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp;

/// <summary>
/// Converts <c>New ValueTuple(Of T1, T2)(a, b)</c> to the tuple literal <c>(a, b)</c>, which creates the same value.
/// C# can't write <c>new (T1, T2)(a, b)</c> (CS8181), so anything this declines is created with <c>new ValueTuple&lt;T1, T2&gt;(a, b)</c>.
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

    public async Task<ExpressionSyntax> ConvertToTupleLiteralOrNullAsync(VBSyntax.ObjectCreationExpressionSyntax node)
    {
        if (_commonConversions.TriviaConvertingExpressionVisitor.IsWithinExpressionTree) return null;
        var elements = GetTupleElementsOrNull(node);
        if (elements is not {Count: > 1} || !elements.All(e => _commonConversions.TypeConversionAnalyzer.KeepsConvertedTypeWithoutTargetType(e.Expression))) return null;

        var arguments = await elements.SelectAsync(async e => await e.AcceptAsync<ArgumentSyntax>(_commonConversions.TriviaConvertingExpressionVisitor));
        return SyntaxFactory.TupleExpression(SyntaxFactory.SeparatedList(arguments));
    }

    /// <summary>
    /// The arguments in element order, flattening a TRest created inline, since that's how a tuple literal with more than 7 elements is laid out.
    /// </summary>
    private List<VBSyntax.SimpleArgumentSyntax> GetTupleElementsOrNull(VBSyntax.ObjectCreationExpressionSyntax node)
    {
        if (node.Initializer != null || _semanticModel.GetTypeInfo(node).Type is not {IsTupleType: true}) return null;
        if (node.ArgumentList?.Arguments.All(a => a is VBSyntax.SimpleArgumentSyntax {IsNamed: false}) != true) return null;

        var elements = node.ArgumentList.Arguments.Cast<VBSyntax.SimpleArgumentSyntax>().ToList();
        if (elements.Count <= RestIndex) return elements;

        var restElements = elements[RestIndex].Expression is VBSyntax.ObjectCreationExpressionSyntax restCreation && HasNoConversion(restCreation)
            ? GetTupleElementsOrNull(restCreation)
            : null;
        return restElements is null ? null : elements.Take(RestIndex).Concat(restElements).ToList();
    }

    private bool HasNoConversion(VBSyntax.ExpressionSyntax expression) =>
        _semanticModel.GetTypeInfo(expression) is var typeInfo && SymbolEqualityComparer.Default.Equals(typeInfo.Type, typeInfo.ConvertedType);
}
