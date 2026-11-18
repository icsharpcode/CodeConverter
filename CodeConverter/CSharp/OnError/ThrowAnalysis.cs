using Microsoft.CodeAnalysis.Operations;
namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// A conservative "can this statement or expression raise an error" check, used to tell when a secondary error is impossible,
/// and which resume points need a guard when errors are being ignored.
/// </summary>
/// <remarks>
/// Only exits, jumps, labels, lambdas (creating one runs none of its body) and copies of constants, locals and parameters without a conversion are treated as safe.
/// Integer <c>+</c>, <c>-</c> and <c>*</c> of locals and constants of the one type are safe too, assigned or compound assigned, because the output is unchecked.
/// Conditions and loop bounds are safe when they only compare or combine constants, locals and parameters of primitive types.
/// </remarks>
internal static class ThrowAnalysis
{
    public static bool CanThrow(VBSyntax.StatementSyntax statement, SemanticModel semanticModel) => statement switch {
        VBSyntax.LabelStatementSyntax or VBSyntax.GoToStatementSyntax or VBSyntax.ResumeStatementSyntax or VBSyntax.ExitStatementSyntax or
            VBSyntax.ContinueStatementSyntax or VBSyntax.StopOrEndStatementSyntax => false,
        VBSyntax.ReturnStatementSyntax r => r.Expression != null && !IsSafeValue(r.Expression, semanticModel),
        VBSyntax.AssignmentStatementSyntax a when a.IsKind(VBasic.SyntaxKind.SimpleAssignmentStatement) =>
            !IsLocal(a.Left, semanticModel) || !IsSafeValue(a.Right, semanticModel) && !IsSafeIntegerArithmetic(a.Right, LocalType(a.Left, semanticModel), semanticModel),
        VBSyntax.AssignmentStatementSyntax a when a.Kind() is VBasic.SyntaxKind.AddAssignmentStatement or VBasic.SyntaxKind.SubtractAssignmentStatement or VBasic.SyntaxKind.MultiplyAssignmentStatement =>
            !IsLocal(a.Left, semanticModel) || !IsSafeIntegerOperand(a.Right, LocalType(a.Left, semanticModel), semanticModel),
        VBSyntax.LocalDeclarationStatementSyntax d => d.Declarators.Any(v => CanThrow(v, semanticModel)),
        _ => true
    };

    public static bool CanThrow(VBSyntax.VariableDeclaratorSyntax declarator, SemanticModel semanticModel) =>
        declarator.AsClause is VBSyntax.AsNewClauseSyntax || declarator.Names.Any(n => n.ArrayBounds != null) ||
        declarator.Initializer != null && !IsSafeValue(declarator.Initializer.Value, semanticModel);

    /// <summary>A condition, loop bound or similar: safe when no operation in it can fail, whichever way it's converted.</summary>
    public static bool CanThrow(VBSyntax.ExpressionSyntax expression, SemanticModel semanticModel) =>
        !IsSafeValue(expression, semanticModel) && !IsSafeCombination(expression, semanticModel);

    /// <summary>Comparisons and logical operators over safe primitive operands. Strings and objects are excluded: they bind to runtime helpers.</summary>
    private static bool IsSafeCombination(VBSyntax.ExpressionSyntax expression, SemanticModel semanticModel) => expression switch {
        VBSyntax.ParenthesizedExpressionSyntax p => IsSafeCombination(p.Expression, semanticModel) || IsSafeValue(p.Expression, semanticModel),
        VBSyntax.BinaryExpressionSyntax b when SafeBinaryKinds.Contains(VBasic.VisualBasicExtensions.Kind(b)) =>
            IsSafeOperand(b.Left, semanticModel) && IsSafeOperand(b.Right, semanticModel),
        VBSyntax.UnaryExpressionSyntax u when u.IsKind(VBasic.SyntaxKind.NotExpression) => IsSafeOperand(u.Operand, semanticModel),
        _ => false
    };

    private static readonly HashSet<VBasic.SyntaxKind> SafeBinaryKinds = new() {
        VBasic.SyntaxKind.EqualsExpression, VBasic.SyntaxKind.NotEqualsExpression, VBasic.SyntaxKind.LessThanExpression,
        VBasic.SyntaxKind.LessThanOrEqualExpression, VBasic.SyntaxKind.GreaterThanExpression, VBasic.SyntaxKind.GreaterThanOrEqualExpression,
        VBasic.SyntaxKind.AndExpression, VBasic.SyntaxKind.OrExpression, VBasic.SyntaxKind.ExclusiveOrExpression,
        VBasic.SyntaxKind.AndAlsoExpression, VBasic.SyntaxKind.OrElseExpression
    };

    private static bool IsSafeOperand(VBSyntax.ExpressionSyntax operand, SemanticModel semanticModel)
    {
        if (!IsPrimitiveNonString(semanticModel.GetTypeInfo(operand).Type)) return false;
        return semanticModel.GetConstantValue(operand).HasValue ||
               semanticModel.GetSymbolInfo(operand).Symbol is ILocalSymbol or IParameterSymbol ||
               IsSafeCombination(operand, semanticModel);
    }

    private static bool IsPrimitiveNonString(ITypeSymbol type) =>
        type?.SpecialType is SpecialType.System_Boolean or SpecialType.System_Byte or SpecialType.System_SByte or SpecialType.System_Int16 or SpecialType.System_UInt16 or
            SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_Single or
            SpecialType.System_Double or SpecialType.System_Decimal or SpecialType.System_Char;

    /// <summary>
    /// Whether every operand of a <c>For</c> header is a constant or variable of the control variable's own type: a different type needs a conversion that can overflow.
    /// </summary>
    public static bool CanThrow(VBSyntax.ForStatementSyntax header, SemanticModel semanticModel)
    {
        var control = semanticModel.GetSymbolInfo(header.ControlVariable).Symbol ?? (semanticModel.GetOperation(header.ControlVariable) as IVariableDeclaratorOperation)?.Symbol;
        var controlType = (control as ILocalSymbol)?.Type;
        var bounds = new[] { header.FromValue, header.ToValue, header.StepClause?.StepValue }.Where(e => e != null);
        return !IsPrimitiveNonString(controlType) || bounds.Any(b => CanThrow(b, semanticModel) ||
            !semanticModel.GetConstantValue(b).HasValue && !SymbolEqualityComparer.Default.Equals(semanticModel.GetTypeInfo(b).Type, controlType));
    }

    private static readonly HashSet<VBasic.SyntaxKind> UncheckedArithmeticKinds = new() {
        VBasic.SyntaxKind.AddExpression, VBasic.SyntaxKind.SubtractExpression, VBasic.SyntaxKind.MultiplyExpression
    };

    /// <summary>
    /// <c>+</c>, <c>-</c> and <c>*</c> over integral locals, parameters and constants that are all of the one type, which is also the type assigned to, so there is no conversion.
    /// The output is unchecked, so overflow wraps rather than throwing. Division, <c>\</c>, <c>Mod</c> and <c>^</c> can throw or go through helpers, and <c>Decimal</c> overflows.
    /// </summary>
    private static bool IsSafeIntegerArithmetic(VBSyntax.ExpressionSyntax expression, ITypeSymbol type, SemanticModel semanticModel) => expression switch {
        VBSyntax.ParenthesizedExpressionSyntax p => IsSafeIntegerArithmetic(p.Expression, type, semanticModel),
        VBSyntax.BinaryExpressionSyntax b when UncheckedArithmeticKinds.Contains(VBasic.VisualBasicExtensions.Kind(b)) =>
            IsIntegral(type) && SymbolEqualityComparer.Default.Equals(semanticModel.GetTypeInfo(b).Type, type) &&
            IsSafeIntegerOperand(b.Left, type, semanticModel) && IsSafeIntegerOperand(b.Right, type, semanticModel),
        _ => false
    };

    private static bool IsSafeIntegerOperand(VBSyntax.ExpressionSyntax operand, ITypeSymbol type, SemanticModel semanticModel)
    {
        if (!IsIntegral(type)) return false;
        var typeInfo = semanticModel.GetTypeInfo(operand);
        var isConstantOrVariable = semanticModel.GetConstantValue(operand).HasValue || semanticModel.GetSymbolInfo(operand).Symbol is ILocalSymbol or IParameterSymbol;
        return isConstantOrVariable && SymbolEqualityComparer.Default.Equals(typeInfo.ConvertedType, type) &&
               (semanticModel.GetConstantValue(operand).HasValue || SymbolEqualityComparer.Default.Equals(typeInfo.Type, type)) ||
               IsSafeIntegerArithmetic(operand, type, semanticModel);
    }

    private static ITypeSymbol LocalType(VBSyntax.ExpressionSyntax expression, SemanticModel semanticModel) =>
        (semanticModel.GetSymbolInfo(expression).Symbol as ILocalSymbol)?.Type;

    private static bool IsIntegral(ITypeSymbol type) =>
        type?.SpecialType is SpecialType.System_Byte or SpecialType.System_SByte or SpecialType.System_Int16 or SpecialType.System_UInt16 or
            SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64;

    private static bool IsLocal(VBSyntax.ExpressionSyntax expression, SemanticModel semanticModel) =>
        semanticModel.GetSymbolInfo(expression).Symbol is ILocalSymbol;

    private static bool IsSafeValue(VBSyntax.ExpressionSyntax expression, SemanticModel semanticModel)
    {
        if (expression is VBSyntax.LambdaExpressionSyntax || semanticModel.GetConstantValue(expression).HasValue) return true;
        var typeInfo = semanticModel.GetTypeInfo(expression);
        return semanticModel.GetSymbolInfo(expression).Symbol is ILocalSymbol or IParameterSymbol &&
               SymbolEqualityComparer.Default.Equals(typeInfo.Type, typeInfo.ConvertedType);
    }
}
