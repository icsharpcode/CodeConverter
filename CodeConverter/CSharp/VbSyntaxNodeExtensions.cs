﻿using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

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
        bool parentIsNonArgumentExpression = node.Parent is VBSyntax.ExpressionSyntax && !(node.Parent is VBSyntax.ArgumentSyntax);
        bool parentIsParenthesis = node.Parent is VBSyntax.ParenthesizedExpressionSyntax;
        bool parentIsMemberAccessExpression = node.Parent is VBSyntax.MemberAccessExpressionSyntax;

        return parentIsMemberAccessExpression || parentIsNonArgumentExpression && !parentIsBinaryExpression && !parentIsLambda && !parentIsParenthesis;
    }

    public static bool AlwaysHasBooleanTypeInCSharp(this VBSyntax.ExpressionSyntax node) =>
        node.SkipIntoParens().IsKind(VBasic.SyntaxKind.AndAlsoExpression, VBasic.SyntaxKind.AndExpression, VBasic.SyntaxKind.OrElseExpression, VBasic.SyntaxKind.OrExpression, VBasic.SyntaxKind.NotExpression);
}