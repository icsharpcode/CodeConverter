using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;

namespace ICSharpCode.CodeConverter.Util
{
    internal static class CSharpUtil
    {
        /// <summary>
        /// Inverts a boolean condition. Note: The condition object can be frozen (from AST) it's cloned internally.
        /// </summary>
        /// <param name="condition">The condition to invert.</param>
        public static ExpressionSyntax InvertCondition(this ExpressionSyntax condition)
        {
            return InvertConditionInternal(condition);
        }

        private static ExpressionSyntax InvertConditionInternal(ExpressionSyntax condition)
        {
            if (condition is ParenthesizedExpressionSyntax) {
                return SyntaxFactory.ParenthesizedExpression(InvertCondition(((ParenthesizedExpressionSyntax)condition).Expression));
            }

            if (condition is PrefixUnaryExpressionSyntax) {
                var uOp = (PrefixUnaryExpressionSyntax)condition;
                if (uOp.IsKind(SyntaxKind.LogicalNotExpression)) {
                    if (!(uOp.Parent is ExpressionSyntax))
                        return uOp.Operand.SkipIntoParens();
                    return uOp.Operand;
                }
                return SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, uOp);
            }

            if (condition is BinaryExpressionSyntax) {
                var bOp = (BinaryExpressionSyntax)condition;

                if (bOp.IsKind(SyntaxKind.LogicalAndExpression) || bOp.IsKind(SyntaxKind.LogicalOrExpression))
                    return SyntaxFactory.BinaryExpression(NegateConditionOperator(bOp.Kind()), InvertCondition(bOp.Left), InvertCondition(bOp.Right));

                if (bOp.IsKind(SyntaxKind.EqualsExpression) ||
                    bOp.IsKind(SyntaxKind.NotEqualsExpression) ||
                    bOp.IsKind(SyntaxKind.GreaterThanExpression) ||
                    bOp.IsKind(SyntaxKind.GreaterThanOrEqualExpression) ||
                    bOp.IsKind(SyntaxKind.LessThanExpression) ||
                    bOp.IsKind(SyntaxKind.LessThanOrEqualExpression))
                    return SyntaxFactory.BinaryExpression(NegateRelationalOperator(bOp.Kind()), bOp.Left, bOp.Right);

                return SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, SyntaxFactory.ParenthesizedExpression(condition));
            }

            if (condition is ConditionalExpressionSyntax cEx) {
                return cEx.WithCondition(InvertCondition(cEx.Condition));
            }

            if (condition is LiteralExpressionSyntax) {
                if (condition.Kind() == SyntaxKind.TrueLiteralExpression)
                    return SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
                if (condition.Kind() == SyntaxKind.FalseLiteralExpression)
                    return SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);
            }

            return SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, condition.AddParens());
        }

        public static SyntaxKind GetExpressionOperatorTokenKind(SyntaxKind op)
        {
            switch (op) {
                case SyntaxKind.EqualsExpression:
                    return SyntaxKind.EqualsEqualsToken;
                case SyntaxKind.NotEqualsExpression:
                    return SyntaxKind.ExclamationEqualsToken;
                case SyntaxKind.GreaterThanExpression:
                    return SyntaxKind.GreaterThanToken;
                case SyntaxKind.GreaterThanOrEqualExpression:
                    return SyntaxKind.GreaterThanEqualsToken;
                case SyntaxKind.LessThanExpression:
                    return SyntaxKind.LessThanToken;
                case SyntaxKind.LessThanOrEqualExpression:
                    return SyntaxKind.LessThanEqualsToken;
                case SyntaxKind.BitwiseOrExpression:
                    return SyntaxKind.BarToken;
                case SyntaxKind.LogicalOrExpression:
                    return SyntaxKind.BarBarToken;
                case SyntaxKind.BitwiseAndExpression:
                    return SyntaxKind.AmpersandToken;
                case SyntaxKind.LogicalAndExpression:
                    return SyntaxKind.AmpersandAmpersandToken;
                case SyntaxKind.AddExpression:
                    return SyntaxKind.PlusToken;
                case SyntaxKind.ExclusiveOrExpression:
                    return SyntaxKind.CaretToken;
                case SyntaxKind.SubtractExpression:
                    return SyntaxKind.MinusToken;
                case SyntaxKind.MultiplyExpression:
                    return SyntaxKind.AsteriskToken;
                case SyntaxKind.DivideExpression:
                    return SyntaxKind.SlashToken;
                case SyntaxKind.ModuloExpression:
                    return SyntaxKind.PercentToken;
                case SyntaxKind.LeftShiftExpression:
                    return SyntaxKind.LessThanLessThanToken;
                case SyntaxKind.RightShiftExpression:
                    return SyntaxKind.GreaterThanGreaterThanToken;
                // assignments
                case SyntaxKind.SimpleAssignmentExpression:
                    return SyntaxKind.EqualsToken;
                case SyntaxKind.AddAssignmentExpression:
                    return SyntaxKind.PlusEqualsToken;
                case SyntaxKind.SubtractAssignmentExpression:
                    return SyntaxKind.MinusEqualsToken;
                case SyntaxKind.LeftShiftAssignmentExpression:
                    return SyntaxKind.LessThanLessThanEqualsToken;
                case SyntaxKind.RightShiftAssignmentExpression:
                    return SyntaxKind.GreaterThanGreaterThanEqualsToken;
                // unary
                case SyntaxKind.UnaryPlusExpression:
                    return SyntaxKind.PlusToken;
                case SyntaxKind.UnaryMinusExpression:
                    return SyntaxKind.MinusToken;
                case SyntaxKind.LogicalNotExpression:
                    return SyntaxKind.ExclamationToken;
                case SyntaxKind.BitwiseNotExpression:
                    return SyntaxKind.TildeToken;
            }
            throw new ArgumentOutOfRangeException(nameof(op), op, null);
        }

        /// <summary>
        /// When negating an expression this is required, otherwise you would end up with
        /// a or b -> !a or b
        /// If they end up being redundant, they get automatically removed by <see cref="DocumentExtensions.SimplifyStatements(Document, string, CancellationToken)"/>
        /// </summary>
        public static ExpressionSyntax AddParens(this ExpressionSyntax expression)
        {
            return SyntaxFactory.ParenthesizedExpression(expression);
        }

        /// <summary>
        /// Get negation of the specified relational operator
        /// </summary>
        /// <returns>
        /// negation of the specified relational operator, or BinaryOperatorType.Any if it's not a relational operator
        /// </returns>
        public static SyntaxKind NegateRelationalOperator(SyntaxKind op)
        {
            switch (op) {
                case SyntaxKind.EqualsExpression:
                    return SyntaxKind.NotEqualsExpression;
                case SyntaxKind.NotEqualsExpression:
                    return SyntaxKind.EqualsExpression;
                case SyntaxKind.GreaterThanExpression:
                    return SyntaxKind.LessThanOrEqualExpression;
                case SyntaxKind.GreaterThanOrEqualExpression:
                    return SyntaxKind.LessThanExpression;
                case SyntaxKind.LessThanExpression:
                    return SyntaxKind.GreaterThanOrEqualExpression;
                case SyntaxKind.LessThanOrEqualExpression:
                    return SyntaxKind.GreaterThanExpression;
                case SyntaxKind.LogicalOrExpression:
                    return SyntaxKind.LogicalAndExpression;
                case SyntaxKind.LogicalAndExpression:
                    return SyntaxKind.LogicalOrExpression;
            }
            throw new ArgumentOutOfRangeException(nameof(op));
        }

        /// <summary>
        /// Get negation of the condition operator
        /// </summary>
        /// <returns>
        /// negation of the specified condition operator, or BinaryOperatorType.Any if it's not a condition operator
        /// </returns>
        public static SyntaxKind NegateConditionOperator(SyntaxKind op)
        {
            switch (op) {
                case SyntaxKind.LogicalOrExpression:
                    return SyntaxKind.LogicalAndExpression;
                case SyntaxKind.LogicalAndExpression:
                    return SyntaxKind.LogicalOrExpression;
            }
            throw new ArgumentOutOfRangeException(nameof(op));
        }

        public static T WithBody<T>(this T method, BlockSyntax body) where T : BaseMethodDeclarationSyntax
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            var m = method as MethodDeclarationSyntax;
            if (m != null)
                return (T)((BaseMethodDeclarationSyntax)m.WithBody(body));
            var d = method as DestructorDeclarationSyntax;
            if (d != null)
                return (T)((BaseMethodDeclarationSyntax)d.WithBody(body));
            throw new NotSupportedException();
        }

        public static Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeSyntax ToVbSyntax(this ITypeSymbol type, SemanticModel model, TypeSyntax typeSyntax)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            return VBasic.SyntaxFactory.ParseTypeName(type.ToMinimalDisplayString(model, typeSyntax.SpanStart))
                .WithLeadingTrivia(typeSyntax.GetLeadingTrivia().ConvertTrivia())
                .WithTrailingTrivia(typeSyntax.GetTrailingTrivia().ConvertTrivia());
        }

        public static IEnumerable<TOut> FollowProperty<TOut>(this TOut start, Func<TOut, TOut> getProperty) where TOut : class
        {
            return FollowProperty<TOut, TOut>(start, getProperty);
        }

        public static IEnumerable<TOut> FollowProperty<TIn, TOut>(this TIn start, Func<TOut, TOut> getProperty) where TOut : class where TIn: TOut
        {
            for (TOut current = start; current != null; current = getProperty(current)) {
                yield return current;
            }
        }

        public static IEnumerable<TOut> FollowProperty<TOut>(this TOut start,
            Func<TOut, IEnumerable<TOut>> getProperty) where TOut : class
        {
            return FollowProperty<TOut, TOut>(start, getProperty);
        }

        public static IEnumerable<TOut> FollowProperty<TIn, TOut>(this TIn start, Func<TOut, IEnumerable<TOut>> getProperty) where TOut : class where TIn : TOut
        {
            var queue = new Queue<TOut>(new TOut[]{start});
            while (queue.Any()) {
                var current = queue.Dequeue();
                yield return current;
                EnqueueNonNull(queue, getProperty(current));
            }
        }

        private static void EnqueueNonNull<T>(Queue<T> queue, IEnumerable<T> range) where T : class
        {
            foreach (T r in range.Where(r => r != null)) {
                queue.Enqueue(r);
            }
        }
    }
}
