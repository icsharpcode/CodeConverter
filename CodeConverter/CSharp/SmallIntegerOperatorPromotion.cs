using ICSharpCode.CodeConverter.Util.FromRoslyn;

namespace ICSharpCode.CodeConverter.CSharp;

/// <summary>
/// VB's intrinsic operators on Byte, SByte, Short and UShort give a result of that type (or its nullable),
/// whereas C# promotes such operands to int, so the converted expression is an int.
/// That int is always congruent to VB's result modulo 2^bits, but can be out of the VB type's range, e.g. for a Byte of 200, VB's <c>Not</c> gives 55 where C#'s <c>~</c> gives -201.
/// This decides where the int must be cast back to the VB type, so the cast is added where the operator is converted, whatever consumes it.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>An operand of a wrapping operator (+, -, *, And, Or, Xor, Not, the left of &lt;&lt;) of the same type needs no cast: that operator's own result keeps the congruence and gets cast instead.</item>
/// <item>A value already in range needs no cast where an int converts with the same meaning: as an operand of another intrinsic operator (C# promotes it anyway),
/// or where it converts to a wider numeric type, String or Boolean.</item>
/// <item>Anything else needs the cast: e.g. the VB type itself (a lambda return, an array element), Object (boxing), an enum, or a user-defined conversion.</item>
/// </list>
/// VB spec: https://learn.microsoft.com/dotnet/visual-basic/reference/language-specification/expressions#operator-types
/// C# spec: https://learn.microsoft.com/dotnet/csharp/language-reference/language-specification/expressions#12473-binary-numeric-promotions
/// </remarks>
internal class SmallIntegerOperatorPromotion
{
    private static readonly VBasic.SyntaxKind[] PromotingOperatorKinds = {
        VBasic.SyntaxKind.AddExpression, VBasic.SyntaxKind.SubtractExpression, VBasic.SyntaxKind.MultiplyExpression,
        VBasic.SyntaxKind.IntegerDivideExpression, VBasic.SyntaxKind.ModuloExpression,
        VBasic.SyntaxKind.AndExpression, VBasic.SyntaxKind.OrExpression, VBasic.SyntaxKind.ExclusiveOrExpression,
        VBasic.SyntaxKind.LeftShiftExpression, VBasic.SyntaxKind.RightShiftExpression,
        VBasic.SyntaxKind.UnaryMinusExpression, VBasic.SyntaxKind.UnaryPlusExpression, VBasic.SyntaxKind.NotExpression
    };

    /// <summary>
    /// Operators whose result modulo 2^bits only depends on their operands modulo 2^bits
    /// </summary>
    private static readonly VBasic.SyntaxKind[] WrappingOperatorKinds = {
        VBasic.SyntaxKind.AddExpression, VBasic.SyntaxKind.SubtractExpression, VBasic.SyntaxKind.MultiplyExpression,
        VBasic.SyntaxKind.AndExpression, VBasic.SyntaxKind.OrExpression, VBasic.SyntaxKind.ExclusiveOrExpression,
        VBasic.SyntaxKind.UnaryMinusExpression, VBasic.SyntaxKind.UnaryPlusExpression, VBasic.SyntaxKind.NotExpression
    };

    private readonly SemanticModel _semanticModel;

    public SmallIntegerOperatorPromotion(SemanticModel semanticModel) => _semanticModel = semanticModel;

    /// <summary>
    /// Whether the C# conversion of <paramref name="vbNode"/> is a promoted int that must be cast back to its VB type
    /// </summary>
    public bool NeedsCastToVbType(VBSyntax.ExpressionSyntax vbNode)
    {
        if (GetPromotedTypeOrNull(vbNode) is not { } vbType) return false;
        var outermost = vbNode.OutermostParenthesizedOrSelf();
        if (IsOperandWhoseCongruenceIsKeptBy(outermost, vbType)) return false;
        return !IsResultInRange(vbNode.SkipIntoParens()) || !AcceptsIntWithSameMeaning(outermost);
    }

    /// <summary>
    /// C# rejects a constant cast that overflows unless it's unchecked, e.g. <c>(byte)~1</c>, whereas VB's constant <c>Not</c> is in range
    /// </summary>
    public bool IsConstantThatMayBeOutOfRange(VBSyntax.ExpressionSyntax vbNode) =>
        _semanticModel.GetConstantValue(vbNode).HasValue && !IsResultInRange(vbNode.SkipIntoParens());

    /// <summary>
    /// The VB type of an intrinsic operator on a small integer type, which C# promotes to int, or null for any other expression
    /// </summary>
    public ITypeSymbol GetPromotedTypeOrNull(VBSyntax.ExpressionSyntax vbNode)
    {
        var node = vbNode.SkipIntoParens();
        if (!node.IsKind(PromotingOperatorKinds) || IsUserDefinedOperator(node)) return null;
        var type = _semanticModel.GetTypeInfo(node).Type;
        return IsSmallIntegerType(GetUnderlyingType(type)) ? type : null;
    }

    private bool IsOperandWhoseCongruenceIsKeptBy(VBSyntax.ExpressionSyntax outermost, ITypeSymbol vbType)
    {
        var parent = outermost.Parent as VBSyntax.ExpressionSyntax;
        var isWrappingOperand = parent.IsKind(WrappingOperatorKinds) ||
                                parent is VBSyntax.BinaryExpressionSyntax shift && shift.IsKind(VBasic.SyntaxKind.LeftShiftExpression) && shift.Left == outermost;
        return isWrappingOperand && GetPromotedTypeOrNull(parent) is { } parentType &&
               GetUnderlyingType(parentType).SpecialType == GetUnderlyingType(vbType).SpecialType;
    }

    private bool AcceptsIntWithSameMeaning(VBSyntax.ExpressionSyntax outermost)
    {
        var convertedType = GetUnderlyingType(_semanticModel.GetTypeInfo(outermost).ConvertedType);
        if (convertedType?.SpecialType is SpecialType.System_String or SpecialType.System_Boolean) return true;
        if (IsIntrinsicOperator(outermost.Parent)) return convertedType.IsNumericType();
        return convertedType?.SpecialType is SpecialType.System_Int32 or SpecialType.System_Int64
            or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal;
    }

    /// <summary>
    /// Whether the C# value of the promoted operator always equals the VB value, rather than just being congruent to it
    /// </summary>
    private bool IsResultInRange(VBSyntax.ExpressionSyntax node)
    {
        var isSigned = GetUnderlyingType(_semanticModel.GetTypeInfo(node).Type).SpecialType is SpecialType.System_SByte or SpecialType.System_Int16;
        return node switch {
            VBSyntax.BinaryExpressionSyntax b => VBasic.VisualBasicExtensions.Kind(b) switch {
                VBasic.SyntaxKind.AndExpression or VBasic.SyntaxKind.OrExpression or VBasic.SyntaxKind.ExclusiveOrExpression or VBasic.SyntaxKind.ModuloExpression =>
                    IsCSharpValueInRange(b.Left) && IsCSharpValueInRange(b.Right),
                // The most negative value divided by -1 is out of range
                VBasic.SyntaxKind.IntegerDivideExpression => !isSigned && IsCSharpValueInRange(b.Left) && IsCSharpValueInRange(b.Right),
                VBasic.SyntaxKind.RightShiftExpression => IsCSharpValueInRange(b.Left),
                _ => false
            },
            VBSyntax.UnaryExpressionSyntax u => VBasic.VisualBasicExtensions.Kind(u) switch {
                // Not maps the signed range onto itself (~x == -x - 1)
                VBasic.SyntaxKind.NotExpression => isSigned && IsCSharpValueInRange(u.Operand),
                VBasic.SyntaxKind.UnaryPlusExpression => IsCSharpValueInRange(u.Operand),
                // VB negates an unsigned operand in the next wider signed type, e.g. Byte as Short
                VBasic.SyntaxKind.UnaryMinusExpression => !IsSameUnderlyingType(u, u.Operand) && IsCSharpValueInRange(u.Operand),
                _ => false
            },
            _ => false
        };
    }

    private bool IsCSharpValueInRange(VBSyntax.ExpressionSyntax operand) =>
        GetPromotedTypeOrNull(operand) is null || NeedsCastToVbType(operand) || IsResultInRange(operand.SkipIntoParens());

    private bool IsSameUnderlyingType(VBSyntax.ExpressionSyntax a, VBSyntax.ExpressionSyntax b) =>
        GetUnderlyingType(_semanticModel.GetTypeInfo(a).Type)?.SpecialType == GetUnderlyingType(_semanticModel.GetTypeInfo(b).Type)?.SpecialType;

    private bool IsIntrinsicOperator(SyntaxNode node) =>
        node is VBSyntax.BinaryExpressionSyntax or VBSyntax.UnaryExpressionSyntax && !node.IsKind(VBasic.SyntaxKind.AddressOfExpression) && !IsUserDefinedOperator(node);

    private bool IsUserDefinedOperator(SyntaxNode node) =>
        _semanticModel.GetSymbolInfo(node).Symbol is IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator };

    private static ITypeSymbol GetUnderlyingType(ITypeSymbol type) => type?.GetNullableUnderlyingType() ?? type;

    private static bool IsSmallIntegerType(ITypeSymbol type) =>
        type?.SpecialType is SpecialType.System_Byte or SpecialType.System_SByte or SpecialType.System_Int16 or SpecialType.System_UInt16;
}
