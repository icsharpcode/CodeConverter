using System.Xml.Linq;
using ICSharpCode.CodeConverter.Util.FromRoslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp;
#nullable enable

/// <summary>
/// Use as `KnownNullability?` where null means nullability is not known, the expression may or may not be null
/// </summary>
public enum KnownNullability : byte
{
    Null,
    NotNull
}

internal class VisualBasicNullableExpressionsConverter
{
    private readonly SemanticModel _semanticModel;
    private static readonly IsPatternExpressionSyntax NotFormattedIsPattern = ((IsPatternExpressionSyntax)SyntaxFactory.ParseExpression("is {}"));
    private static readonly IsPatternExpressionSyntax NotFormattedNegatedIsPattern = ((IsPatternExpressionSyntax)SyntaxFactory.ParseExpression("is not {}"));

    private static readonly NullableTypeSyntax NullableBoolType = SyntaxFactory.NullableType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)));
    private static readonly ExpressionSyntax Null = ValidSyntaxFactory.CastExpression(NullableBoolType, ValidSyntaxFactory.NullExpression);
    private static readonly ExpressionSyntax False = ValidSyntaxFactory.CastExpression(NullableBoolType, ValidSyntaxFactory.FalseExpression);
    private static readonly ExpressionSyntax True = ValidSyntaxFactory.CastExpression(NullableBoolType, ValidSyntaxFactory.TrueExpression);

    private int _argCounter;
    /// <summary>
    /// The code with this annotation is not nullable (even though the source code that created it is)
    /// </summary>
    private static readonly SyntaxAnnotation IsNotNullableAnnotation = new("CodeConverter.Nullable", false.ToString());

    public VisualBasicNullableExpressionsConverter(SemanticModel semanticModel) => _semanticModel = semanticModel;

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
        var isLhsNullable = IsNullable(vbNode.Left, lhs, lhsTypeInfo);
        var isRhsNullable = IsNullable(vbNode.Right, rhs, rhsTypeInfo);
        if (!isLhsNullable && !isRhsNullable) return csBinExp.WithAdditionalAnnotations(IsNotNullableAnnotation);

        return WithBinaryExpressionLogicForNullableTypes(vbNode, csBinExp, lhs, rhs, isLhsNullable, isRhsNullable);
    }

    private ExpressionSyntax WithBinaryExpressionLogicForNullableTypes(Microsoft.CodeAnalysis.VisualBasic.Syntax.BinaryExpressionSyntax vbNode, BinaryExpressionSyntax csBinExp, ExpressionSyntax lhs, ExpressionSyntax rhs, bool isLhsNullable,
        bool isRhsNullable)
    {
        lhs = lhs.AddParens();
        rhs = rhs.AddParens();

        if (vbNode.IsKind(VBasic.SyntaxKind.AndAlsoExpression))
        {
            return ForAndAlsoOperator(vbNode, lhs, rhs, isLhsNullable, isRhsNullable).AddParens();
        }

        if (vbNode.IsKind(VBasic.SyntaxKind.OrElseExpression))
        {
            return ForOrElseOperator(vbNode, lhs, rhs, isLhsNullable, isRhsNullable).AddParens();
        }

        return ForRelationalOperators(vbNode, csBinExp, lhs, rhs, isLhsNullable, isRhsNullable).AddParens();
    }

    private bool IsNullable(VBSyntax.ExpressionSyntax vbNode, ExpressionSyntax csNode, TypeInfo lhsTypeInfo)
    {
        return lhsTypeInfo.Type.IsNullable() && !csNode.AnyInParens(x => x.HasAnnotation(IsNotNullableAnnotation)) && GetNullabilityWithinBooleanExpression(vbNode) != KnownNullability.NotNull;
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

    private ExpressionSyntax ForRelationalOperators(VBSyntax.BinaryExpressionSyntax vbNode,
        BinaryExpressionSyntax csNode, ExpressionSyntax lhs, ExpressionSyntax rhs,
        bool isLhsNullable, bool isRhsNullable)
    {
        ExpressionSyntax GetCondition(ref ExpressionSyntax csName, bool reusable) =>
            reusable ? csName.NullableHasValueExpression() : PatternObject(csName, out csName);

        ExpressionSyntax? lhsName = lhs, rhsName = rhs;
        var lhsReusable = IsSafelyReusable(vbNode.Left);
        var rhsReusable = IsSafelyReusable(vbNode.Right);
        List<(bool ExecutionOptional, ExpressionSyntax Expr)> conditions = new(3);
        if (!lhsReusable && !rhsReusable) {
            // This is the worst case, where everything's nullable but the names aren't reusable (might have side effects) so we need to use an extra var pattern to save it away first, then we can reuse that name
            conditions.Add((false, PatternVar(rhsName, out rhsName)));
            rhsReusable = true;
        }

        if (isLhsNullable || !lhsReusable) {
            var lhsCondition = GetCondition(ref lhsName, lhsReusable);
            conditions.Add((lhsReusable, lhsCondition));
        }
        if (isRhsNullable || !rhsReusable) {
            var rhsCondition = GetCondition(ref rhsName, rhsReusable);
            conditions.Add((rhsReusable, rhsCondition));
        }

        // Ensure expressions/properties with side effects are evaluated the same number of times as before
        conditions.Sort((a, b) => a.ExecutionOptional.CompareTo(b.ExecutionOptional));
        var notNullCondition = conditions.Skip(1)
            .Aggregate(conditions.ElementAtOrDefault(0).Expr, (current, condition) => current.And(condition.Expr));

        var bin = lhsName.Bin(rhsName, csNode.Kind());

        if (vbNode.AlwaysHasBooleanTypeInCSharp()) {
            return notNullCondition is null ? bin : notNullCondition.And(bin);
        }

        return notNullCondition is null ? bin : notNullCondition.Conditional(bin, Null);
    }

    private bool IsSafelyReusable(VBasic.Syntax.ExpressionSyntax e)
    {
        e = e.SkipIntoParens();
        if (e is VBSyntax.LiteralExpressionSyntax) return true;
        var symbolInfo = VBasic.VisualBasicExtensions.GetSymbolInfo(_semanticModel, e);
        if (symbolInfo.Symbol is not { } s) return false;
        return s.IsKind(SymbolKind.Local) || s.IsKind(SymbolKind.Field) || s.IsKind(SymbolKind.Parameter);
    }

    private KnownNullability? GetNullabilityWithinBooleanExpression(VBSyntax.ExpressionSyntax original)
    {
        if (original.SkipIntoParens() is not VBSyntax.IdentifierNameSyntax {Identifier.Text: { } nameText}) return null;
        for (VBSyntax.ExpressionSyntax? currentNode = original.Parent?.Parent as VBSyntax.ExpressionSyntax, childNode = original.Parent as VBSyntax.ExpressionSyntax;
             currentNode is VBSyntax.BinaryExpressionSyntax {Left: var l, OperatorToken: var op, Right: var r};
             currentNode = currentNode?.Parent as VBSyntax.ExpressionSyntax) {

            if (r == childNode) {
                if (op.IsKind(VBasic.SyntaxKind.AndAlsoKeyword)) {
                    // Look inside left knowing it'd be true if we evaluate the right
                    if (GetNullabilityWithinBooleanExpression(nameText, l, true) is {} knownNullability) {
                        return knownNullability;
                    }
                }

                if (op.IsKind(VBasic.SyntaxKind.OrElseKeyword)) {
                    // Look inside left knowing it'd be false if we evaluate the right
                    if (GetNullabilityWithinBooleanExpression(nameText, l, false) is { } knownNullability) {
                        return knownNullability;
                    }
                }
            }

            childNode = currentNode;
        }

        return null;
    }

    private KnownNullability? GetNullabilityWithinBooleanExpression(string identifierText, VBSyntax.ExpressionSyntax expression, bool expressionResult)
    {
        return expression switch {
            VBSyntax.MemberAccessExpressionSyntax {Name.Identifier.Text: { } memberText, Expression: VBSyntax.IdentifierNameSyntax {Identifier.Text: { } checkedIdentifierText}}
                when memberText.Equals("HasValue", StringComparison.OrdinalIgnoreCase) && checkedIdentifierText.Equals(identifierText, StringComparison.OrdinalIgnoreCase) =>
                expressionResult ? KnownNullability.NotNull : KnownNullability.Null,
            VBSyntax.BinaryExpressionSyntax bin => GetNullabilityWithinBinaryExpression(identifierText, bin, expressionResult),
            VBSyntax.UnaryExpressionSyntax un when un.OperatorToken.IsKind(VBasic.SyntaxKind.NotKeyword) => GetNullabilityWithinBooleanExpression(identifierText, expression, !expressionResult),
            _ => null
        };
    }

    private KnownNullability? GetNullabilityWithinBinaryExpression(string identifierText, VBSyntax.BinaryExpressionSyntax bin, bool expressionResult)
    {
        if (bin.OperatorToken.IsKind(VBasic.SyntaxKind.IsKeyword) && ContainsIdentifierAndNothing(bin.Left, bin.Right, identifierText)) {
            return expressionResult ? KnownNullability.Null : KnownNullability.NotNull;
        } else if (bin.OperatorToken.IsKind(VBasic.SyntaxKind.IsNotKeyword) && ContainsIdentifierAndNothing(bin.Left, bin.Right, identifierText)) {
            return expressionResult ? KnownNullability.NotNull : KnownNullability.Null;
        } else if (bin.OperatorToken.IsKind(VBasic.SyntaxKind.AndAlsoKeyword) || bin.OperatorToken.IsKind(VBasic.SyntaxKind.AndKeyword)) {
            return GetNullabilityWithinBooleanExpression(identifierText, bin.Left, expressionResult) ?? GetNullabilityWithinBooleanExpression(identifierText, bin.Right, expressionResult);
        } else if (bin.OperatorToken.IsKind(VBasic.SyntaxKind.OrElseKeyword) || bin.OperatorToken.IsKind(VBasic.SyntaxKind.OrKeyword)) {
            var left = GetNullabilityWithinBooleanExpression(identifierText, bin.Left, expressionResult);
            var right = GetNullabilityWithinBooleanExpression(identifierText, bin.Right, expressionResult);
            return left == right ? left : null;
        }
        return null;
    }

    private static bool ContainsIdentifierAndNothing(VBSyntax.ExpressionSyntax l, VBSyntax.ExpressionSyntax r, string requiredIdentifierText)
    {
        return MatchesIdentifier(l) && MatchesNothing(r) || MatchesNothing(l) && MatchesIdentifier(r);

        bool MatchesNothing(VBSyntax.ExpressionSyntax expr) =>
            expr.SkipIntoParens().IsKind(VBasic.SyntaxKind.NothingLiteralExpression);

        bool MatchesIdentifier(VBSyntax.ExpressionSyntax expr) =>
            expr.SkipIntoParens() is VBSyntax.IdentifierNameSyntax {Identifier.Text: { } identifierTextToCheck} && requiredIdentifierText.Equals(identifierTextToCheck, StringComparison.OrdinalIgnoreCase);
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