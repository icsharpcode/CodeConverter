using ICSharpCode.CodeConverter.Util.FromRoslyn;
using Microsoft.CodeAnalysis.CSharp;
using ExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax;
using TypeSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax;

namespace ICSharpCode.CodeConverter.CSharp;

/// <summary>
/// Converts a constant to the type it's declared as where C# requires a constant of that type: a parameter default, an enum member's value or an attribute argument.
/// VB converts the constant at compile time, so its converted value is always available, but a literal hides the names and calculations it came from. So:
/// <list type="number">
/// <item>Keep the value as written where C# converts the constant implicitly (e.g. an Integer constant in range of a Byte).</item>
/// <item>Otherwise cast it, which still gives a C# constant (e.g. <c>(int)L</c> for a Long constant, <c>(Col)1</c>).</item>
/// <item>Otherwise use VB's converted value as a literal: C# won't round a fractional value to an integral type, can't convert a Boolean to a number,
/// and a conversion that became a call (e.g. <c>Conversions.ToShort(true) &lt;&lt; 3</c>) isn't a C# constant.</item>
/// </list>
/// A nullable type's default is a constant of its underlying type.
/// </summary>
/// <remarks>
/// C# constant conversions: https://learn.microsoft.com/dotnet/csharp/language-reference/language-specification/conversions#10212-implicit-constant-expression-conversions
/// </remarks>
internal class DeclaredConstantConverter
{
    private readonly SemanticModel _semanticModel;
    private readonly CSharpCompilation _csCompilation;
    private readonly Func<ITypeSymbol, TypeSyntax> _getTypeSyntax;

    public DeclaredConstantConverter(SemanticModel semanticModel, CSharpCompilation csCompilation, Func<ITypeSymbol, TypeSyntax> getTypeSyntax)
    {
        _semanticModel = semanticModel;
        _csCompilation = csCompilation;
        _getTypeSyntax = getTypeSyntax;
    }

    /// <param name="requiresExactType">The constant must already have the target type, e.g. for [DefaultParameterValue], whose argument is an Object</param>
    /// <param name="parameter">For a default, the parameter, whose default value is the one VB converted the constant to (with its type, for an Object parameter)</param>
    public ExpressionSyntax Convert(VBSyntax.ExpressionSyntax vbValue, ExpressionSyntax csValue, ITypeSymbol targetType, bool requiresExactType = false, IParameterSymbol parameter = null)
    {
        if (targetType == null || vbValue.SkipIntoParens().IsKind(VBasic.SyntaxKind.NothingLiteralExpression)) return csValue;
        if (_semanticModel.GetConstantValue(vbValue) is not { HasValue: true, Value: { } value }) return csValue;
        var target = targetType.GetNullableUnderlyingType() ?? targetType;
        var sourceType = _semanticModel.GetTypeInfo(vbValue).Type;
        if (sourceType == null) return csValue;
        if (target.SpecialType == SpecialType.System_Object) return ConvertToObject(vbValue, csValue, value, sourceType, parameter);
        // C# converts a Single or Double to Decimal at runtime precision, VB at compile time to its own value, e.g. 0.1F gives 0.100000001490116
        if (target.SpecialType == SpecialType.System_Decimal && sourceType.IsFractionalNumericType() && parameter?.GetValidExplicitDefaultValueOrNull() is decimal decimalValue) {
            return LiteralConversions.GetLiteralExpression(decimalValue);
        }
        if (!IsConstantInCSharp(vbValue, csValue)) return GetConvertedLiteralOrNull(value, target, requiresExactType) ?? csValue;
        if (SymbolEqualityComparer.Default.Equals(sourceType, target)) return csValue;
        if (sourceType.IsFractionalNumericType() && SmallIntegerOperatorPromotion.GetUnderlyingNumericType(target).IsIntegralType()) {
            return GetConvertedLiteralOrNull(value, target, requiresExactType) ?? csValue;
        }

        // An enum converts to and from a number only explicitly (and may not be in the C# compilation yet), so its conversions are those of its underlying type
        var isEnumConversion = sourceType.IsEnumType() || target.IsEnumType();
        var csSource = GetCSharpSpecialTypeOrNull(SmallIntegerOperatorPromotion.GetUnderlyingNumericType(sourceType));
        var csTarget = GetCSharpSpecialTypeOrNull(SmallIntegerOperatorPromotion.GetUnderlyingNumericType(target));
        if (csSource == null || csTarget == null) return csValue;
        var conversion = _csCompilation.ClassifyConversion(csSource, csTarget);
        if (isEnumConversion) {
            return conversion.IsNumeric || conversion.IsIdentity
                ? ValidSyntaxFactory.CastExpression(_getTypeSyntax(target), csValue)
                : GetConvertedLiteralOrNull(value, target, requiresExactType) ?? csValue;
        }
        if (!requiresExactType && (conversion.IsImplicit || IsImplicitConstantConversion(value, target))) return csValue;
        if (conversion.Exists) return ValidSyntaxFactory.CastExpression(_getTypeSyntax(target), csValue);
        return GetConvertedLiteralOrNull(value, target, requiresExactType) ?? csValue;
    }

    /// <summary>
    /// An Object takes the constant with its own VB type, e.g. CByte(True) is the Byte 255 and CType(2.5, E) the E 2 (rounded), which a cast in C# may not give.
    /// So only a literal or a named constant is kept as written. A default value is stored as VB stores it, e.g. an enum as its underlying value.
    /// </summary>
    private ExpressionSyntax ConvertToObject(VBSyntax.ExpressionSyntax vbValue, ExpressionSyntax csValue, object value, ITypeSymbol sourceType, IParameterSymbol parameter)
    {
        if (vbValue.SkipIntoParens() is VBSyntax.LiteralExpressionSyntax or VBSyntax.IdentifierNameSyntax or VBSyntax.MemberAccessExpressionSyntax && IsConstantInCSharp(vbValue, csValue)) {
            return csValue;
        }
        if (parameter?.GetValidExplicitDefaultValueOrNull() is { } defaultValue && GetSpecialTypeOrNull(defaultValue.GetType()) is { } defaultValueType) {
            return GetConvertedLiteralOrNull(defaultValue, defaultValueType, requiresExactType: true) ?? csValue;
        }
        return GetConvertedLiteralOrNull(value, sourceType, requiresExactType: true) ?? csValue;
    }

    private ITypeSymbol GetSpecialTypeOrNull(Type type) =>
        Enum.TryParse<SpecialType>("System_" + type.Name, out var specialType) && type.Namespace == "System" ? _semanticModel.Compilation.GetSpecialType(specialType) : null;

    /// <summary>
    /// Calls (e.g. to Conversions or Math.Round) are never C# constants, except nameof.
    /// Nor is a concatenation with a Char, which C# only folds for strings, e.g. ChrW(66) &amp; "b" becomes 'B' + "b".
    /// </summary>
    private bool IsConstantInCSharp(VBSyntax.ExpressionSyntax vbValue, ExpressionSyntax csValue) =>
        !csValue.DescendantNodesAndSelf().OfType<CSSyntax.InvocationExpressionSyntax>()
            .Any(i => i.Expression is not CSSyntax.IdentifierNameSyntax { Identifier.ValueText: "nameof" }) &&
        !vbValue.DescendantNodesAndSelf().OfType<VBSyntax.BinaryExpressionSyntax>()
            .Any(b => b.IsKind(VBasic.SyntaxKind.ConcatenateExpression, VBasic.SyntaxKind.AddExpression) &&
                      (_semanticModel.GetTypeInfo(b.Left).Type?.SpecialType == SpecialType.System_Char || _semanticModel.GetTypeInfo(b.Right).Type?.SpecialType == SpecialType.System_Char));

    private ITypeSymbol GetCSharpSpecialTypeOrNull(ITypeSymbol vbType) =>
        vbType?.SpecialType is { } specialType && specialType != SpecialType.None ? _csCompilation.GetSpecialType(specialType) : null;

    private static bool IsImplicitConstantConversion(object value, ITypeSymbol target) =>
        value switch {
            int i => target.SpecialType switch {
                SpecialType.System_SByte => i is >= sbyte.MinValue and <= sbyte.MaxValue,
                SpecialType.System_Byte => i is >= byte.MinValue and <= byte.MaxValue,
                SpecialType.System_Int16 => i is >= short.MinValue and <= short.MaxValue,
                SpecialType.System_UInt16 => i is >= ushort.MinValue and <= ushort.MaxValue,
                SpecialType.System_UInt32 or SpecialType.System_UInt64 => i >= 0,
                _ => false
            },
            long l => target.SpecialType == SpecialType.System_UInt64 && l >= 0,
            _ => false
        };

    /// <summary>
    /// VB's own conversion of the constant, e.g. rounding 2.5 to even
    /// </summary>
    /// <remarks>C# has no literal of a type smaller than int, so for an exact type that needs a cast too</remarks>
    private ExpressionSyntax GetConvertedLiteralOrNull(object value, ITypeSymbol target, bool requiresExactType)
    {
        var numericTarget = SmallIntegerOperatorPromotion.GetUnderlyingNumericType(target);
        if (!ExpressionEvaluator.ConversionsTypeFullNames.TryGetValue(numericTarget.GetFullMetadataName(), out var conversion)) return null;
        var literal = LiteralConversions.GetLiteralExpression(conversion.Invoke(null, new[] { value }), convertedType: numericTarget);
        var hasNoLiteralOfItsType = numericTarget.SpecialType is SpecialType.System_Byte or SpecialType.System_SByte or SpecialType.System_Int16 or SpecialType.System_UInt16;
        // A cast of a negative literal needs parentheses: (E)-1 parses as a subtraction
        return target.IsEnumType() || requiresExactType && hasNoLiteralOfItsType ? ValidSyntaxFactory.CastExpression(_getTypeSyntax(target), literal.AddParens()) : literal;
    }
}
