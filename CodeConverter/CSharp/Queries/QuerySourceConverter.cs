using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace ICSharpCode.CodeConverter.CSharp.Queries;

/// <summary>
/// Converts the collection a range variable is drawn from, making explicit what VB does implicitly to query it
/// </summary>
internal class QuerySourceConverter
{
    private readonly SemanticModel _semanticModel;
    private readonly CommonConversions _commonConversions;
    private readonly Func<VBSyntax.ExpressionSyntax, Task<ExpressionSyntax>> _convertAsync;

    public QuerySourceConverter(SemanticModel semanticModel, CommonConversions commonConversions, Func<VBSyntax.ExpressionSyntax, Task<ExpressionSyntax>> convertAsync)
    {
        _semanticModel = semanticModel;
        _commonConversions = commonConversions;
        _convertAsync = convertAsync;
    }

    /// <returns>A type for the C# from clause if it needs one (i.e. Cast), and the collection</returns>
    /// <remarks>VB implicitly calls AsEnumerable or Cast(Of Object) on some sources, and converts each element to a declared range variable type</remarks>
    public async Task<(TypeSyntax Type, ExpressionSyntax Expression)> ConvertAsync(VBSyntax.CollectionRangeVariableSyntax vbVariable)
    {
        var expression = await _convertAsync(vbVariable.Expression);
        var sourceElementType = _semanticModel.GetTypeInfo(vbVariable.Expression).Type?.GetEnumerableElementTypeOrDefault();
        TypeSyntax castType = null;
        if (GetImplicitQueryableCall(vbVariable.Expression) is { } implicitCall) {
            if (implicitCall.TargetMethod.Name == nameof(Enumerable.AsEnumerable)) {
                expression = SyntaxFactory.InvocationExpression(ValidSyntaxFactory.MemberAccess(CsQueryBuilder.ParenthesizeIfNeeded(expression), nameof(Enumerable.AsEnumerable)), SyntaxFactory.ArgumentList());
            } else if (implicitCall.TargetMethod.Name == nameof(Enumerable.Cast)) {
                sourceElementType = implicitCall.TargetMethod.TypeArguments.Single();
                castType = _commonConversions.GetTypeSyntax(sourceElementType);
            }
        }

        var declaredType = vbVariable.AsClause == null ? null : _semanticModel.GetTypeInfo(vbVariable.AsClause.Type).Type;
        if (declaredType == null || sourceElementType == null || SymbolEqualityComparer.Default.Equals(sourceElementType, declaredType)) {
            return (castType, expression);
        }
        if (IsSameAsCast(sourceElementType, declaredType)) {
            return (_commonConversions.GetTypeSyntax(declaredType), expression);
        }
        var source = castType == null ? expression : CallCast(expression, castType);
        return (null, SelectConverted(vbVariable, source, sourceElementType, declaredType));
    }

    private IInvocationOperation GetImplicitQueryableCall(VBSyntax.ExpressionSyntax collection)
    {
        var parent = _semanticModel.GetOperation(collection)?.Parent;
        while (parent is IConversionOperation { IsImplicit: true }) parent = parent.Parent;
        return parent is IInvocationOperation { IsImplicit: true } invocation ? invocation : null;
    }

    /// <summary>
    /// VB's CType runs conversions like Object to String or Integer to Long, but C#'s Cast&lt;T&gt; would throw
    /// </summary>
    private bool IsSameAsCast(ITypeSymbol sourceType, ITypeSymbol targetType)
    {
        var conversion = VBasic.VisualBasicExtensions.ClassifyConversion(_semanticModel.Compilation, sourceType, targetType);
        return conversion.IsReference && !conversion.IsUserDefined && targetType.SpecialType != SpecialType.System_String;
    }

    private static ExpressionSyntax CallCast(ExpressionSyntax source, TypeSyntax type)
    {
        var cast = SyntaxFactory.GenericName(ValidSyntaxFactory.IdentifierName(nameof(Enumerable.Cast)).Identifier, SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(type)));
        return SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, CsQueryBuilder.ParenthesizeIfNeeded(source), cast), SyntaxFactory.ArgumentList());
    }

    private ExpressionSyntax SelectConverted(VBSyntax.CollectionRangeVariableSyntax vbVariable, ExpressionSyntax source, ITypeSymbol sourceType, ITypeSymbol targetType)
    {
        var variableName = vbVariable.Identifier.Identifier.ValueText;
        var parameter = CommonConversions.CsEscapedIdentifier(targetType.Name == variableName ? CsQueryBuilder.GeneratedElementName : variableName);
        var converted = _commonConversions.TypeConversionAnalyzer.AddExplicitConversion(vbVariable.Expression, SyntaxFactory.IdentifierName(parameter),
            forceSourceType: sourceType, forceTargetType: targetType);
        var lambda = SyntaxFactory.SimpleLambdaExpression(SyntaxFactory.Parameter(parameter), converted);
        return SyntaxFactory.InvocationExpression(ValidSyntaxFactory.MemberAccess(CsQueryBuilder.ParenthesizeIfNeeded(source), nameof(Enumerable.Select)),
            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(lambda))));
    }
}
