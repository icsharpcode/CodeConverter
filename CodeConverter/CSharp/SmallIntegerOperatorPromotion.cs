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
/// <item>A value already in range needs no cast where converting the int gives the same as converting the VB type: as an operand of another intrinsic operator (C# promotes it anyway),
/// or converted to another numeric type (or enum), String or Boolean, or switched on by Select Case. That includes explicit conversions such as CInt, whose target type is what counts.</item>
/// <item>Any value needs no cast where it's converted to an integral type (or enum of one) no wider than the VB type, or from a nullable to its value, since that conversion truncates it anyway, e.g. CByte(Not us).</item>
/// <item>Anything else needs the cast: e.g. the VB type itself (a lambda return, an array element), Object (boxing), an enum, or a user-defined conversion.</item>
/// </list>
/// Where no cast is needed, <see cref="GetCSharpTypeOrNull"/> tells the conversion to the target that the source is an int.
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

    /// <summary>
    /// Each answer depends on the operands' answers, so without these a chain of operators would be re-analyzed exponentially.
    /// Operators are converted innermost first (see <see cref="BinaryExpressionConverter"/>), so the operands' answers are already here and the recursion stays shallow.
    /// </summary>
    private readonly Dictionary<VBSyntax.ExpressionSyntax, bool> _needsCastToVbType = new();
    private readonly Dictionary<VBSyntax.ExpressionSyntax, bool> _isResultInRange = new();

    public SmallIntegerOperatorPromotion(SemanticModel semanticModel) => _semanticModel = semanticModel;

    /// <summary>
    /// Whether the C# conversion of <paramref name="vbNode"/> is a promoted int that must be cast back to its VB type
    /// </summary>
    public bool NeedsCastToVbType(VBSyntax.ExpressionSyntax vbNode) =>
        Memoized(_needsCastToVbType, vbNode.SkipIntoParens(), AnalyzeNeedsCastToVbType);

    /// <param name="node">An operator, not parenthesized</param>
    private bool AnalyzeNeedsCastToVbType(VBSyntax.ExpressionSyntax node)
    {
        if (GetPromotedTypeOrNull(node) is not { } vbType) return false;
        var outermost = node.OutermostParenthesizedOrSelf();
        if (!IsConstantThatMayBeOutOfRange(node) && IsOperandWhoseCongruenceIsKeptBy(outermost, vbType)) return false;
        return !AcceptsPromotedValue(outermost, vbType, IsResultInRange(node));
    }

    /// <summary>
    /// C# rejects a constant cast that overflows unless it's unchecked, e.g. <c>(byte)~1</c>, whereas VB's constant <c>Not</c> is in range.
    /// It also rejects a constant operation that overflows int, so such a constant is cast where it is, rather than by a wrapping parent:
    /// e.g. <c>(Not C1) * C2</c> on UShort folds to -65535 * 40000.
    /// </summary>
    public bool IsConstantThatMayBeOutOfRange(VBSyntax.ExpressionSyntax vbNode) =>
        _semanticModel.GetConstantValue(vbNode).HasValue && !IsResultInRange(vbNode.SkipIntoParens());

    /// <summary>
    /// Whether this node's conversion is cast in an unchecked context of its own, rather than one an enclosing operator adds
    /// </summary>
    /// <remarks>
    /// An unchecked context covers everything inside it, so only the outermost one is needed.
    /// That's decided from the VB model rather than tracked while converting, since operators are converted innermost first (see <see cref="BinaryExpressionConverter"/>).
    /// An enclosing operator that adds one is itself constant, so the search stops at the first ancestor that isn't.
    /// </remarks>
    public bool NeedsOwnUncheckedContext(VBSyntax.ExpressionSyntax vbNode) =>
        IsConstantThatMayBeOutOfRange(vbNode) && !vbNode.OutermostParenthesizedOrSelf().Ancestors().OfType<VBSyntax.ExpressionSyntax>()
            .TakeWhile(a => _semanticModel.GetConstantValue(a).HasValue)
            .Any(a => NeedsCastToVbType(a) && IsConstantThatMayBeOutOfRange(a));

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

    /// <summary>
    /// VB masks a shift count to the bits of the shifted type (e.g. 7 for Byte), C# masks it to the bits of the promoted int (31).
    /// Larger types match, so need no mask.
    /// </summary>
    /// <remarks>https://learn.microsoft.com/dotnet/visual-basic/language-reference/operators/left-shift-operator</remarks>
    /// <param name="shiftedType">The type of the shift, or of its left operand: an enum is shifted as its underlying type (with Option Strict Off)</param>
    public static int? GetShiftCountMaskOrNull(ITypeSymbol shiftedType) =>
        GetUnderlyingNumericType(shiftedType)?.SpecialType switch {
            SpecialType.System_Byte or SpecialType.System_SByte => 7,
            SpecialType.System_Int16 or SpecialType.System_UInt16 => 15,
            _ => null
        };

    private bool IsOperandWhoseCongruenceIsKeptBy(VBSyntax.ExpressionSyntax outermost, ITypeSymbol vbType)
    {
        var parent = outermost.Parent as VBSyntax.ExpressionSyntax;
        var isWrappingOperand = parent.IsKind(WrappingOperatorKinds) ||
                                parent is VBSyntax.BinaryExpressionSyntax shift && shift.IsKind(VBasic.SyntaxKind.LeftShiftExpression) && shift.Left == outermost;
        return isWrappingOperand && GetPromotedTypeOrNull(parent) is { } parentType &&
               GetUnderlyingType(parentType).SpecialType == GetUnderlyingType(vbType).SpecialType;
    }

    /// <summary>
    /// The type the value is converted to by its consumer, unless that's the VB type itself
    /// </summary>
    /// <remarks>For an explicit conversion (e.g. CInt), the operand's own converted type is just its type</remarks>
    public ITypeSymbol GetCSharpTypeOrNull(VBSyntax.ExpressionSyntax vbNode, ITypeSymbol targetType)
    {
        if (GetPromotedTypeOrNull(vbNode) is not { } vbType || NeedsCastToVbType(vbNode) || !IsConversionToAnotherType(targetType, vbType)) {
            return null;
        }
        return GetInt32Like(vbType);
    }

    /// <summary>
    /// The type VB shifts a left operand of <paramref name="leftType"/> as, which is also the shift's result type: an integral type stays as it is.
    /// With Option Strict Off, an enum is shifted as its underlying type, Boolean as Short, and a fractional type or String as Long. C# can shift none of those.
    /// </summary>
    /// <remarks>https://learn.microsoft.com/dotnet/visual-basic/language-reference/operators/left-shift-operator</remarks>
    public ITypeSymbol GetVbShiftedType(ITypeSymbol leftType)
    {
        var compilation = _semanticModel.Compilation;
        var underlying = GetUnderlyingType(leftType);
        var shifted = underlying switch {
            INamedTypeSymbol { EnumUnderlyingType: { } enumUnderlyingType } => enumUnderlyingType,
            { SpecialType: SpecialType.System_Boolean } => compilation.GetSpecialType(SpecialType.System_Int16),
            { SpecialType: SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal or SpecialType.System_String } =>
                compilation.GetSpecialType(SpecialType.System_Int64),
            _ => underlying
        };
        return leftType.IsNullable() ? compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(shifted) : shifted;
    }

    /// <summary>
    /// The type C# gives an intrinsic operator whose VB result type is <paramref name="vbResultType"/>
    /// </summary>
    public ITypeSymbol GetCSharpOperatorResultType(ITypeSymbol vbResultType) =>
        IsSmallIntegerType(GetUnderlyingType(vbResultType)) ? GetInt32Like(vbResultType) : vbResultType;

    private ITypeSymbol GetInt32Like(ITypeSymbol vbType)
    {
        var int32 = _semanticModel.Compilation.GetSpecialType(SpecialType.System_Int32);
        return vbType.IsNullable() ? _semanticModel.Compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(int32) : int32;
    }

    private bool AcceptsPromotedValue(VBSyntax.ExpressionSyntax outermost, ITypeSymbol vbType, bool isInRange)
    {
        var convertedType = GetTargetType(outermost);
        var targetType = GetUnderlyingType(convertedType);
        var underlyingVbType = GetUnderlyingType(vbType);
        // C#'s ?? converts neither operand to the If's type, so the int stays an int, unless that's a wider type
        var isCoalesced = IsCoalescedOperand(outermost);
        var isConversionToAnotherType = isCoalesced
            ? !SymbolEqualityComparer.Default.Equals(targetType, underlyingVbType)
            : IsConversionToAnotherType(convertedType, vbType);
        if (!isCoalesced && isConversionToAnotherType && IsIntegralNoWiderThan(GetUnderlyingNumericType(targetType), underlyingVbType)) return true;
        if (!isInRange) return false;
        if (targetType?.SpecialType is SpecialType.System_String or SpecialType.System_Boolean) return true;
        // Without a format string, an in-range int formats like the VB type (whereas e.g. :X formats a negative int with 32 bits)
        if (outermost.Parent is VBSyntax.InterpolationSyntax { FormatClause: null }) return true;
        // Each case compares with the value, which an in-range int has just the same
        if (outermost.Parent is VBSyntax.SelectStatementSyntax) return true;
        if (IsIntrinsicOperator(outermost.Parent)) return targetType.IsNumericType();
        return GetUnderlyingNumericType(targetType).IsNumericType() && isConversionToAnotherType;
    }

    /// <summary>
    /// Whether converting to <paramref name="targetType"/> produces a value of another C# type (so the int can be converted instead), rather than just wrapping it in Nullable
    /// </summary>
    private static bool IsConversionToAnotherType(ITypeSymbol targetType, ITypeSymbol vbType) =>
        !SymbolEqualityComparer.Default.Equals(GetUnderlyingType(targetType), GetUnderlyingType(vbType)) || vbType.IsNullable() && !targetType.IsNullable();

    private ITypeSymbol GetTargetType(VBSyntax.ExpressionSyntax outermost) => outermost.Parent switch {
        VBSyntax.CastExpressionSyntax or VBSyntax.PredefinedCastExpressionSyntax => _semanticModel.GetTypeInfo(outermost.Parent).Type,
        // The value of a nullable first operand is converted to the type of If, rather than to its own converted type
        VBSyntax.BinaryConditionalExpressionSyntax coalesce when IsCoalescedOperand(outermost) => _semanticModel.GetTypeInfo(coalesce).Type,
        _ => _semanticModel.GetTypeInfo(outermost).ConvertedType
    };

    private static bool IsCoalescedOperand(VBSyntax.ExpressionSyntax outermost) =>
        outermost.Parent is VBSyntax.BinaryConditionalExpressionSyntax coalesce && coalesce.FirstExpression == outermost;

    private static bool IsIntegralNoWiderThan(ITypeSymbol type, ITypeSymbol other) =>
        GetIntegralBitsOrNull(type) is { } bits && bits <= GetIntegralBitsOrNull(other);

    private static int? GetIntegralBitsOrNull(ITypeSymbol type) => type?.SpecialType switch {
        SpecialType.System_Byte or SpecialType.System_SByte => 8,
        SpecialType.System_Int16 or SpecialType.System_UInt16 => 16,
        SpecialType.System_Int32 or SpecialType.System_UInt32 => 32,
        SpecialType.System_Int64 or SpecialType.System_UInt64 => 64,
        _ => null
    };

    /// <summary>
    /// Whether the C# value of the promoted operator always equals the VB value, rather than just being congruent to it
    /// </summary>
    private bool IsResultInRange(VBSyntax.ExpressionSyntax node) => Memoized(_isResultInRange, node, AnalyzeIsResultInRange);

    private bool AnalyzeIsResultInRange(VBSyntax.ExpressionSyntax node)
    {
        var isSigned = GetUnderlyingType(_semanticModel.GetTypeInfo(node).Type).SpecialType is SpecialType.System_SByte or SpecialType.System_Int16;
        if (_semanticModel.GetConstantValue(node).HasValue) return IsConstantResultInRange(node, isSigned);
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
                // VB negates an operand of another type (e.g. Byte or Boolean) in the result type, which is wide enough for every negated value
                VBasic.SyntaxKind.UnaryMinusExpression => !IsSameUnderlyingType(u, u.Operand) && IsCSharpValueInRange(u.Operand),
                _ => false
            },
            _ => false
        };
    }

    /// <summary>
    /// VB rejects a constant that overflows its type, so given exact operands, only the operators that wrap without overflowing (Not on an unsigned type, and &lt;&lt;) can give C# a different value.
    /// Not on an unsigned type always does; a left shift does if it carries bits out of the type.
    /// </summary>
    private bool IsConstantResultInRange(VBSyntax.ExpressionSyntax node, bool isSigned) => node switch {
        VBSyntax.UnaryExpressionSyntax u => (isSigned || !u.IsKind(VBasic.SyntaxKind.NotExpression)) && IsCSharpValueInRange(u.Operand),
        VBSyntax.BinaryExpressionSyntax b when b.IsKind(VBasic.SyntaxKind.LeftShiftExpression) => IsCSharpValueInRange(b.Left) && IsConstantShiftExact(b),
        VBSyntax.BinaryExpressionSyntax b => IsCSharpValueInRange(b.Left) && IsCSharpValueInRange(b.Right),
        _ => false
    };

    /// <summary>
    /// C# shifts the exact left operand by the masked count without wrapping, see <see cref="GetShiftCountMaskOrNull"/>
    /// </summary>
    private bool IsConstantShiftExact(VBSyntax.BinaryExpressionSyntax shift)
    {
        var mask = GetShiftCountMaskOrNull(_semanticModel.GetTypeInfo(shift).Type) ?? 31;
        // VB's True is -1 as a number
        var left = _semanticModel.GetConstantValue(shift.Left).Value is bool b ? (b ? -1 : 0) : Convert.ToInt64(_semanticModel.GetConstantValue(shift.Left).Value);
        var count = Convert.ToInt32(_semanticModel.GetConstantValue(shift.Right).Value) & mask;
        return left << count == Convert.ToInt64(_semanticModel.GetConstantValue(shift).Value);
    }

    private static bool Memoized(Dictionary<VBSyntax.ExpressionSyntax, bool> cache, VBSyntax.ExpressionSyntax node, Func<VBSyntax.ExpressionSyntax, bool> analyze)
    {
        if (!cache.TryGetValue(node, out var result)) {
            cache[node] = result = analyze(node);
        }
        return result;
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

    public static ITypeSymbol GetUnderlyingNumericType(ITypeSymbol type) =>
        GetUnderlyingType(type) is INamedTypeSymbol { EnumUnderlyingType: { } enumUnderlyingType } ? enumUnderlyingType : GetUnderlyingType(type);

    private static bool IsSmallIntegerType(ITypeSymbol type) =>
        type?.SpecialType is SpecialType.System_Byte or SpecialType.System_SByte or SpecialType.System_Int16 or SpecialType.System_UInt16;
}
