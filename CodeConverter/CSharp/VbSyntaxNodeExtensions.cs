using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ICSharpCode.CodeConverter.CSharp;

internal static class VbSyntaxNodeExtensions
{
    public static CSSyntax.ExpressionSyntax ParenthesizeIfPrecedenceCouldChange(this VBasic.VisualBasicSyntaxNode node, CSSyntax.ExpressionSyntax expression)
    {
        return PrecedenceCouldChange(node) ? SyntaxFactory.ParenthesizedExpression(expression) : expression;
    }

    public static bool PrecedenceCouldChange(this VBasic.VisualBasicSyntaxNode node)
    {
        bool parentIsBinaryExpression = node is VBSyntax.BinaryExpressionSyntax;
        bool parentIsLambda = node.Parent is VBSyntax.LambdaExpressionSyntax;
        bool parentIsNonArgumentExpression = node.Parent is VBSyntax.ExpressionSyntax && node.Parent is not VBSyntax.ArgumentSyntax;
        bool parentIsParenthesis = node.Parent is VBSyntax.ParenthesizedExpressionSyntax;
        bool parentIsMemberAccessExpression = node.Parent is VBSyntax.MemberAccessExpressionSyntax;

        return parentIsMemberAccessExpression || parentIsNonArgumentExpression && !parentIsBinaryExpression && !parentIsLambda && !parentIsParenthesis;
    }

    /// <summary>
    /// Where VB binds a condition as Boolean (e.g. Nothing becomes False), the C# condition must be bool.
    /// VB's binder passes the condition context through parentheses (isOperandOfConditionalBranch in Binder_Expressions.vb), so this does too.
    /// </summary>
    public static bool AlwaysHasBooleanTypeInCSharp(this VBSyntax.ExpressionSyntax vbNode)
    {
        var condition = vbNode.OutermostParenthesizedOrSelf();

        return condition.Parent switch {
            VBSyntax.SingleLineIfStatementSyntax singleLine => singleLine.Condition == condition,
            VBSyntax.IfStatementSyntax ifStatement => ifStatement.Condition == condition,
            VBSyntax.ElseIfStatementSyntax elseIfStatement => elseIfStatement.Condition == condition,
            VBSyntax.TernaryConditionalExpressionSyntax ternary => ternary.Condition == condition,
            VBSyntax.WhileStatementSyntax whileStatement => whileStatement.Condition == condition,
            VBSyntax.WhileOrUntilClauseSyntax or VBSyntax.CatchFilterClauseSyntax => true,
            VBSyntax.WhereClauseSyntax or VBSyntax.PartitionWhileClauseSyntax => true,
            _ => false
        };
    }

    public static VBSyntax.ExpressionSyntax OutermostParenthesizedOrSelf(this VBSyntax.ExpressionSyntax vbNode)
    {
        while (vbNode.Parent is VBSyntax.ParenthesizedExpressionSyntax parenthesized) vbNode = parenthesized;
        return vbNode;
    }

    /// <summary>
    /// Where only whether the value is True matters: VB treats Nothing like False.
    /// That's the case for an operand of OrElse in such a context, and of AndAlso.
    /// The left of AndAlso only counts if the right is free of calls, since VB evaluates the right when the left is Nothing.
    /// </summary>
    public static bool IsOnlyTestedForTrue(this VBSyntax.ExpressionSyntax vbNode, SemanticModel semanticModel)
    {
        var node = vbNode.SkipOutOfParens();
        if (node.AlwaysHasBooleanTypeInCSharp()) return true;
        return node.Parent is VBSyntax.BinaryExpressionSyntax bin &&
               (bin.IsKind(VBasic.SyntaxKind.OrElseExpression) || bin.IsKind(VBasic.SyntaxKind.AndAlsoExpression) &&
                   (bin.Right == node || !bin.Right.DescendantNodesAndSelf().OfType<VBSyntax.ExpressionSyntax>().Any(e => e.MayThrowOrHaveSideEffects(semanticModel)))) &&
               bin.IsOnlyTestedForTrue(semanticModel);
    }

    /// <summary>
    /// Besides calls, arithmetic can overflow or divide by zero, and narrowing conversions can fail.
    /// Property getters (and member access on a null receiver) are assumed not to.
    /// </summary>
    public static bool MayThrowOrHaveSideEffects(this VBSyntax.ExpressionSyntax expression, SemanticModel semanticModel) =>
        expression.IsCallOrConversion(semanticModel) ||
        expression.IsKind(VBasic.SyntaxKind.AddExpression, VBasic.SyntaxKind.SubtractExpression, VBasic.SyntaxKind.MultiplyExpression, VBasic.SyntaxKind.DivideExpression,
            VBasic.SyntaxKind.IntegerDivideExpression, VBasic.SyntaxKind.ModuloExpression, VBasic.SyntaxKind.ExponentiateExpression, VBasic.SyntaxKind.UnaryMinusExpression) ||
        expression is VBSyntax.BinaryExpressionSyntax or VBSyntax.UnaryExpressionSyntax && semanticModel.GetSymbolInfo(expression).Symbol is IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator } ||
        VBasic.VisualBasicExtensions.GetConversion(semanticModel, expression).IsNarrowing;

    /// <summary>
    /// Could have side effects or throw. VB calls a method referenced without parentheses.
    /// </summary>
    public static bool IsCallOrConversion(this VBSyntax.ExpressionSyntax expression, SemanticModel semanticModel) =>
        expression is VBSyntax.InvocationExpressionSyntax or VBSyntax.ObjectCreationExpressionSyntax or VBSyntax.AwaitExpressionSyntax
            or VBSyntax.CastExpressionSyntax or VBSyntax.PredefinedCastExpressionSyntax ||
        expression is VBSyntax.IdentifierNameSyntax or VBSyntax.MemberAccessExpressionSyntax && semanticModel.GetSymbolInfo(expression).Symbol is IMethodSymbol;

    public static TokenContext GetMemberContext(this VBSyntax.StatementSyntax member)
    {
        var parentType = member.GetAncestorOrThis<VBSyntax.TypeBlockSyntax>();
        var parentTypeKind = parentType?.Kind();
        switch (parentTypeKind) {
            case VBasic.SyntaxKind.ModuleBlock:
                return TokenContext.MemberInModule;
            case VBasic.SyntaxKind.ClassBlock:
                return TokenContext.MemberInClass;
            case VBasic.SyntaxKind.InterfaceBlock:
                return TokenContext.MemberInInterface;
            case VBasic.SyntaxKind.StructureBlock:
                return TokenContext.MemberInStruct;
            default:
                throw new ArgumentOutOfRangeException(nameof(member));
        }
    }
}