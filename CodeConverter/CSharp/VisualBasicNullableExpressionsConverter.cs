using ICSharpCode.CodeConverter.Util.FromRoslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp;
#nullable enable

internal class VisualBasicNullableExpressionsConverter
{
    private static readonly IsPatternExpressionSyntax NotFormattedIsPattern = ((IsPatternExpressionSyntax)SyntaxFactory.ParseExpression("is {}"));
    private static readonly IsPatternExpressionSyntax NotFormattedNegatedIsPattern = ((IsPatternExpressionSyntax)SyntaxFactory.ParseExpression("is not {}"));

    private static readonly NullableTypeSyntax NullableBoolType = SyntaxFactory.NullableType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)));
    private static readonly ExpressionSyntax Null = ValidSyntaxFactory.CastExpression(NullableBoolType, ValidSyntaxFactory.NullExpression);
    private static readonly ExpressionSyntax False = ValidSyntaxFactory.CastExpression(NullableBoolType, ValidSyntaxFactory.FalseExpression);
    private static readonly ExpressionSyntax True = ValidSyntaxFactory.CastExpression(NullableBoolType, ValidSyntaxFactory.TrueExpression);

    private int _argCounter;

    public ExpressionSyntax InvokeConversionWhenNotNull(ExpressionSyntax expression, MemberAccessExpressionSyntax conversionMethod, TypeSyntax castType)
    {
        var pattern = PatternObject(expression, out var argName);
        var arguments = SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(argName)));
        ExpressionSyntax invocation = SyntaxFactory.InvocationExpression(conversionMethod, arguments);
        invocation = ValidSyntaxFactory.CastExpression(castType, invocation);

        return pattern.Conditional(invocation, ValidSyntaxFactory.NullExpression).AddParens();
    }

    public ExpressionSyntax WithBinaryExpressionLogicForNullableTypes(VBSyntax.BinaryExpressionSyntax vbNode, TypeInfo lhsTypeInfo, TypeInfo rhsTypeInfo, BinaryExpressionSyntax csBinExp, ExpressionSyntax lhs, ExpressionSyntax rhs)
    {
        if (!IsSupported(vbNode.Kind()) || 
            !lhsTypeInfo.ConvertedType.IsNullable() ||
            !rhsTypeInfo.ConvertedType.IsNullable()) {
            return csBinExp;
        }

        var isLhsNullable = lhsTypeInfo.Type.IsNullable();
        var isRhsNullable = rhsTypeInfo.Type.IsNullable();
        lhs = lhs.AddParens();
        rhs = rhs.AddParens();

        if (vbNode.IsKind(VBasic.SyntaxKind.AndAlsoExpression)) {
            return ForAndAlsoOperator(vbNode, lhs, rhs, isLhsNullable, isRhsNullable).AddParens();
        } 
        if (vbNode.IsKind(VBasic.SyntaxKind.OrElseExpression)) {
            return ForOrElseOperator(vbNode, lhs, rhs, isLhsNullable, isRhsNullable).AddParens();
        }

        return ForRelationalOperators(vbNode, csBinExp, lhs, rhs, isLhsNullable, isRhsNullable).AddParens();
    }

    private string GetArgName() => $"arg{Interlocked.Increment(ref _argCounter)}";

    private ExpressionSyntax PatternVar(ExpressionSyntax expr, out ExpressionSyntax name)
    {
        var arg = GetArgName();
        var identifier = SyntaxFactory.Identifier(arg);
        name = SyntaxFactory.IdentifierName(identifier);

        return SyntaxFactory.IsPatternExpression(expr, SyntaxFactory.VarPattern(SyntaxFactory.SingleVariableDesignation(identifier)));
    }

    private ExpressionSyntax PatternObject(ExpressionSyntax expr, out ExpressionSyntax name)
    {
        var identifier = SyntaxFactory.Identifier(GetArgName());
        name = SyntaxFactory.IdentifierName(identifier);
        var variable = SyntaxFactory.SingleVariableDesignation(identifier);

        var recursivePattern = (RecursivePatternSyntax)NotFormattedIsPattern.Pattern;
        return NotFormattedIsPattern.WithExpression(expr).WithPattern(recursivePattern.WithDesignation(variable));
    }

    private ExpressionSyntax NegatedPatternObject(ExpressionSyntax expr, out ExpressionSyntax name)
    {
        var identifier = SyntaxFactory.Identifier(GetArgName());
        name = SyntaxFactory.IdentifierName(identifier);
        var variable = SyntaxFactory.SingleVariableDesignation(identifier);
        var unaryPattern = (UnaryPatternSyntax)NotFormattedNegatedIsPattern.Pattern;
        var recursivePattern = (RecursivePatternSyntax)unaryPattern.Pattern;

        unaryPattern = unaryPattern.WithPattern(recursivePattern.WithDesignation(variable));
        return NotFormattedNegatedIsPattern.WithExpression(expr).WithPattern(unaryPattern);
    }

    private static bool IsSupported(VBasic.SyntaxKind op)
    {
        return op switch {
            VBasic.SyntaxKind.EqualsExpression => true,
            VBasic.SyntaxKind.NotEqualsExpression => true,
            VBasic.SyntaxKind.GreaterThanExpression => true,
            VBasic.SyntaxKind.GreaterThanOrEqualExpression => true,
            VBasic.SyntaxKind.LessThanExpression => true,
            VBasic.SyntaxKind.LessThanOrEqualExpression => true,
            VBasic.SyntaxKind.OrElseExpression => true,
            VBasic.SyntaxKind.AndAlsoExpression => true,
            _ => false
        };
    }

    private ExpressionSyntax ForRelationalOperators(VBSyntax.BinaryExpressionSyntax vbNode,
        BinaryExpressionSyntax csNode, ExpressionSyntax lhs, ExpressionSyntax rhs,
        bool isLhsNullable, bool isRhsNullable)
    {
        ExpressionSyntax notNullCondition, lhsName, rhsName;
        if (!isRhsNullable) {
            var lhsPattern = PatternObject(lhs, out lhsName);
            var rhsPattern = PatternVar(rhs, out rhsName);
            notNullCondition = rhsPattern.And(lhsPattern);
        } else {
            var lhsPattern = PatternVar(lhs, out lhsName);
            var rhsPattern = PatternObject(rhs, out rhsName);
            notNullCondition = !isLhsNullable ? lhsPattern.And(rhsPattern) : lhsPattern.And(rhsPattern).AndHasValue(lhsName);
        }

        var bin = lhsName.Bin(rhsName, csNode.Kind());
        return vbNode.AlwaysHasBooleanTypeInCSharp() ?
            notNullCondition.And(bin) :
            notNullCondition.Conditional(bin, Null);
    }

    private ExpressionSyntax ForAndAlsoOperator(VBSyntax.BinaryExpressionSyntax vbNode,
        ExpressionSyntax lhs, ExpressionSyntax rhs,
        bool isLhsNullable, bool isRhsNullable)
    {
        var lhsPattern = PatternVar(lhs, out var lhsName);
        var rhsPattern = PatternObject(rhs, out var rhsName);

        if (vbNode.AlwaysHasBooleanTypeInCSharp()) {
            if (!isLhsNullable) {
                return lhs.And(rhs.GetValueOrDefault());
            }
            if (!isRhsNullable) {
                return lhsPattern.AndHasNoValue(lhsName).OrIsTrue(lhsName).And(rhs).AndHasValue(lhsName);
            }
            return FullAndExpression().EqualsTrue();
        }

        if (!isLhsNullable) {
            return lhs.Conditional(rhs, False);
        }
        if (!isRhsNullable) {
            return lhsPattern.AndHasValue(lhsName).AndIsFalse(lhsName).Conditional(False, rhs.Conditional(lhsName, False));
        }
        return FullAndExpression();

        ExpressionSyntax FullAndExpression() => 
            lhsPattern.AndHasValue(lhsName).AndIsFalse(lhsName)
                .Conditional(False, rhsPattern.Negate().Conditional(Null, rhsName.Conditional(lhsName, False)));
    }

    private ExpressionSyntax ForOrElseOperator(VBSyntax.BinaryExpressionSyntax vbNode,
        ExpressionSyntax lhs, ExpressionSyntax rhs, bool isLhsNullable, bool isRhsNullable)
    {
        if (vbNode.AlwaysHasBooleanTypeInCSharp()) {
            if (!isLhsNullable) {
                return lhs.Or(rhs.GetValueOrDefault());
            }
            return !isRhsNullable ?
                lhs.GetValueOrDefault().Or(rhs) :
                lhs.GetValueOrDefault().Or(rhs.GetValueOrDefault());
        }

        if (!isLhsNullable) {
            return lhs.Conditional(True, rhs);
        }

        var lhsPattern = PatternVar(lhs, out var lhsName);
        var rhsPattern = NegatedPatternObject(rhs, out var rhsName);

        var whenFalse = !isRhsNullable ?
            rhs.Conditional(True, lhsName) :
            rhsPattern.Conditional(Null, rhsName.Conditional(True, lhsName));

        return lhsPattern.And(lhsName.GetValueOrDefault()).Conditional(True, whenFalse);
    }
}

internal static class NullableTypesLogicExtensions
{
    public static ExpressionSyntax GetValueOrDefault(this ExpressionSyntax node) => SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, node.AddParens(), SyntaxFactory.IdentifierName("GetValueOrDefault")));
    public static ExpressionSyntax Negate(this ExpressionSyntax node) => SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, node.AddParens());

    public static ExpressionSyntax And(this ExpressionSyntax a, ExpressionSyntax b) => SyntaxFactory.BinaryExpression(SyntaxKind.LogicalAndExpression, a.AddParens(), b.AddParens()).AddParens();
    public static ExpressionSyntax AndHasValue(this ExpressionSyntax a, ExpressionSyntax b) => And(a, b.NullableHasValueExpression());
    public static ExpressionSyntax AndHasNoValue(this ExpressionSyntax a, ExpressionSyntax b) => And(a, b.NullableHasValueExpression().Negate());
    public static ExpressionSyntax AndIsFalse(this ExpressionSyntax a, ExpressionSyntax b) => And(a, b.NullableGetValueExpression().Negate());

    public static ExpressionSyntax Or(this ExpressionSyntax a, ExpressionSyntax b) => SyntaxFactory.BinaryExpression(SyntaxKind.LogicalOrExpression, a.AddParens(), b.AddParens()).AddParens();
    public static ExpressionSyntax OrIsTrue(this ExpressionSyntax a, ExpressionSyntax b) => Or(a, b.NullableGetValueExpression());

    public static ExpressionSyntax Conditional(this ExpressionSyntax condition, ExpressionSyntax whenTrue, ExpressionSyntax whenFalse) => SyntaxFactory.ConditionalExpression(condition.AddParens(), whenTrue.AddParens(), whenFalse.AddParens()).AddParens();
    public static ExpressionSyntax Bin(this ExpressionSyntax a, ExpressionSyntax b, SyntaxKind op) => SyntaxFactory.BinaryExpression(op, a.AddParens(), b.AddParens()).AddParens();
    public static ExpressionSyntax EqualsTrue(this ExpressionSyntax a) => SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, a.AddParens(), LiteralConversions.GetLiteralExpression(true)).AddParens();
}