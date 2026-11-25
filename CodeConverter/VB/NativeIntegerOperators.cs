using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.VisualBasic;
using ExpressionSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax;
using SyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory;
using SyntaxKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;

namespace ICSharpCode.CodeConverter.VB;

/// <summary>
/// C#'s built-in operators on nint/nuint (also spelled IntPtr/UIntPtr since C# 11) have no VB counterpart: VB only sees
/// IntPtr/UIntPtr's own operators (+ and - with an Integer offset, =, &lt;&gt;) and explicit conversions, because .NET 7's
/// numeric operators are explicit interface implementations. With Option Strict Off VB would otherwise narrow an operand
/// to Integer (throwing beyond Int32, e.g. for the distance between two pointers) or not compile at all.
/// So such an operation is evaluated on Long/ULong, with the result converted back when C#'s result is a native integer.
/// Where VB's own IntPtr/UIntPtr operator does what C#'s does, the output is left as it is.
/// Like other integer arithmetic, the Long/ULong operation is overflow-checked in VB where C#'s is unchecked by default.
/// </summary>
internal class NativeIntegerOperators
{
    private readonly SemanticModel _semanticModel;
    private readonly CommonConversions _commonConversions;

    public NativeIntegerOperators(SemanticModel semanticModel, CommonConversions commonConversions)
    {
        _semanticModel = semanticModel;
        _commonConversions = commonConversions;
    }

    /// <returns>Null when the operation needs nothing special</returns>
    public ExpressionSyntax ConvertBinaryOrNull(CSSyntax.BinaryExpressionSyntax node, ExpressionSyntax vbLeft, ExpressionSyntax vbRight)
    {
        if (_semanticModel.GetOperation(node) is not IBinaryOperation { OperatorMethod: null } operation) return null;
        var operatorType = IsNativeInteger(operation.LeftOperand.Type) ? operation.LeftOperand.Type
            : IsNativeInteger(operation.RightOperand.Type) ? operation.RightOperand.Type
            : null;
        return ConvertOrNull(operation.OperatorKind, operatorType, OriginalType(operation.LeftOperand), vbLeft,
            OriginalType(operation.RightOperand), vbRight, operation.Type);
    }

    /// <returns>The value to assign to the target, or null when the compound assignment needs nothing special</returns>
    public ExpressionSyntax ConvertCompoundAssignmentValueOrNull(CSSyntax.AssignmentExpressionSyntax node, ExpressionSyntax vbTarget, ExpressionSyntax vbValue)
    {
        if (_semanticModel.GetOperation(node) is not ICompoundAssignmentOperation { OperatorMethod: null } operation) return null;
        var targetType = operation.Target.Type;
        var operatorType = IsNativeInteger(targetType) ? targetType : null;
        return ConvertOrNull(operation.OperatorKind, operatorType, targetType, vbTarget, OriginalType(operation.Value), vbValue, targetType);
    }

    /// <returns>Null when the operation needs nothing special</returns>
    public ExpressionSyntax ConvertUnaryOrNull(CSSyntax.PrefixUnaryExpressionSyntax node, ExpressionSyntax vbOperand)
    {
        if (_semanticModel.GetOperation(node) is not IUnaryOperation { OperatorMethod: null } operation || !IsNativeInteger(operation.Type)) return null;
        var kind = operation.OperatorKind switch {
            UnaryOperatorKind.Minus => SyntaxKind.UnaryMinusExpression,
            UnaryOperatorKind.Plus => SyntaxKind.UnaryPlusExpression,
            UnaryOperatorKind.BitwiseNegation => SyntaxKind.NotExpression,
            _ => SyntaxKind.None
        };
        if (kind == SyntaxKind.None) return null;
        var unary = SyntaxFactory.UnaryExpression(kind, SyntaxFactory.Token(kind.GetExpressionOperatorTokenKind()), To64Bit(vbOperand, operation.Type));
        return ToNativeInteger(unary, operation.Type);
    }

    private ExpressionSyntax ConvertOrNull(BinaryOperatorKind operatorKind, ITypeSymbol operatorType,
        ITypeSymbol leftType, ExpressionSyntax vbLeft, ITypeSymbol rightType, ExpressionSyntax vbRight, ITypeSymbol resultType)
    {
        if (operatorType == null && !IsNativeInteger(leftType) && !IsNativeInteger(rightType)) return null;
        if (operatorType != null && HasMatchingVbOperator(operatorKind, leftType, rightType)) return null;
        var kind = ToVbBinaryKind(operatorKind);
        if (kind == SyntaxKind.None) return null;

        var isShift = operatorKind is BinaryOperatorKind.LeftShift or BinaryOperatorKind.RightShift;
        var binary = SyntaxFactory.BinaryExpression(kind,
            ConvertOperand(vbLeft, leftType, operatorType),
            SyntaxFactory.Token(kind.GetExpressionOperatorTokenKind()),
            isShift ? vbRight : ConvertOperand(vbRight, rightType, operatorType));
        return IsNativeInteger(resultType) ? ToNativeInteger(binary, resultType) : binary;
    }

    /// <summary>
    /// IntPtr/UIntPtr declare = and &lt;&gt;, and + and - with an Integer offset, which are unchecked like C#'s.
    /// An operand narrower than Integer widens to it.
    /// </summary>
    private static bool HasMatchingVbOperator(BinaryOperatorKind operatorKind, ITypeSymbol leftType, ITypeSymbol rightType) =>
        operatorKind switch {
            BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals => true,
            BinaryOperatorKind.Add or BinaryOperatorKind.Subtract => IsNativeInteger(leftType) && WidensToInteger(rightType),
            _ => false
        };

    private static bool WidensToInteger(ITypeSymbol type) =>
        type?.SpecialType is SpecialType.System_SByte or SpecialType.System_Byte or SpecialType.System_Int16
            or SpecialType.System_UInt16 or SpecialType.System_Int32;

    /// <summary>
    /// A native integer operand becomes Long/ULong. With a ULong operator, VB would otherwise pick Decimal for ULong with
    /// a signed operand, so the other operand becomes ULong too; with a Long operator VB widens it.
    /// </summary>
    private static ExpressionSyntax ConvertOperand(ExpressionSyntax vbOperand, ITypeSymbol operandType, ITypeSymbol operatorType)
    {
        if (IsNativeInteger(operandType)) return To64Bit(vbOperand, operandType);
        if (operatorType?.SpecialType == SpecialType.System_UIntPtr && operandType?.SpecialType != SpecialType.System_UInt64) {
            return To64Bit(vbOperand, operatorType);
        }
        return vbOperand;
    }

    private static ExpressionSyntax To64Bit(ExpressionSyntax vbOperand, ITypeSymbol nativeIntegerType)
    {
        var keyword = nativeIntegerType.SpecialType == SpecialType.System_UIntPtr ? SyntaxKind.CULngKeyword : SyntaxKind.CLngKeyword;
        return SyntaxFactory.PredefinedCastExpression(SyntaxFactory.Token(keyword), vbOperand);
    }

    private ExpressionSyntax ToNativeInteger(ExpressionSyntax vbExpression, ITypeSymbol nativeIntegerType) =>
        SyntaxFactory.CTypeExpression(vbExpression, _commonConversions.GetFullyQualifiedNameSyntax(nativeIntegerType, allowGlobalPrefix: false));

    private static SyntaxKind ToVbBinaryKind(BinaryOperatorKind operatorKind) =>
        operatorKind switch {
            BinaryOperatorKind.Add => SyntaxKind.AddExpression,
            BinaryOperatorKind.Subtract => SyntaxKind.SubtractExpression,
            BinaryOperatorKind.Multiply => SyntaxKind.MultiplyExpression,
            BinaryOperatorKind.Divide => SyntaxKind.IntegerDivideExpression,
            BinaryOperatorKind.Remainder => SyntaxKind.ModuloExpression,
            BinaryOperatorKind.LeftShift => SyntaxKind.LeftShiftExpression,
            BinaryOperatorKind.RightShift => SyntaxKind.RightShiftExpression,
            BinaryOperatorKind.And => SyntaxKind.AndExpression,
            BinaryOperatorKind.Or => SyntaxKind.OrExpression,
            BinaryOperatorKind.ExclusiveOr => SyntaxKind.ExclusiveOrExpression,
            BinaryOperatorKind.Equals => SyntaxKind.EqualsExpression,
            BinaryOperatorKind.NotEquals => SyntaxKind.NotEqualsExpression,
            BinaryOperatorKind.LessThan => SyntaxKind.LessThanExpression,
            BinaryOperatorKind.LessThanOrEqual => SyntaxKind.LessThanOrEqualExpression,
            BinaryOperatorKind.GreaterThan => SyntaxKind.GreaterThanExpression,
            BinaryOperatorKind.GreaterThanOrEqual => SyntaxKind.GreaterThanOrEqualExpression,
            _ => SyntaxKind.None
        };

    private static ITypeSymbol OriginalType(IOperation operand) =>
        operand is IConversionOperation { IsImplicit: true } conversion ? conversion.Operand.Type : operand.Type;

    private static bool IsNativeInteger(ITypeSymbol type) => type?.SpecialType is SpecialType.System_IntPtr or SpecialType.System_UIntPtr;
}
