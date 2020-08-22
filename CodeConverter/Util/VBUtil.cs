using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using CS = Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.Util
{
    internal static class VBUtil
    {
        /// <summary>
        /// Inverts a boolean condition. Note: The condition object can be frozen (from AST) it's cloned internally.
        /// </summary>
        /// <param name="condition">The condition to invert.</param>
        public static ExpressionSyntax InvertCondition(ExpressionSyntax condition)
        {
            if (condition is ParenthesizedExpressionSyntax) {
                return SyntaxFactory.ParenthesizedExpression(
                    InvertCondition(((ParenthesizedExpressionSyntax)condition).Expression));
            }

            if (condition is UnaryExpressionSyntax) {
                var uOp = (UnaryExpressionSyntax)condition;
                if (uOp.IsKind(SyntaxKind.NotExpression)) {
                    return uOp.Operand;
                }
                return SyntaxFactory.UnaryExpression(
                    SyntaxKind.NotExpression,
                    uOp.OperatorToken,
                    uOp);
            }

            if (condition is BinaryExpressionSyntax) {
                var bOp = (BinaryExpressionSyntax)condition;

                if (bOp.IsKind(SyntaxKind.AndExpression) ||
                    bOp.IsKind(SyntaxKind.AndAlsoExpression) ||
                    bOp.IsKind(SyntaxKind.OrExpression) ||
                    bOp.IsKind(SyntaxKind.OrElseExpression)) {
                    var kind = NegateConditionOperator(bOp.Kind());
                    return SyntaxFactory.BinaryExpression(
                        kind,
                        InvertCondition(bOp.Left),
                        SyntaxFactory.Token(GetExpressionOperatorTokenKind(kind)),
                        InvertCondition(bOp.Right));
                }

                if (bOp.IsKind(SyntaxKind.EqualsExpression) ||
                    bOp.IsKind(SyntaxKind.NotEqualsExpression) ||
                    bOp.IsKind(SyntaxKind.GreaterThanExpression) ||
                    bOp.IsKind(SyntaxKind.GreaterThanOrEqualExpression) ||
                    bOp.IsKind(SyntaxKind.LessThanExpression) ||
                    bOp.IsKind(SyntaxKind.LessThanOrEqualExpression)) {
                    var kind = NegateRelationalOperator(bOp.Kind());
                    return SyntaxFactory.BinaryExpression(
                        kind,
                        bOp.Left,
                        SyntaxFactory.Token(GetExpressionOperatorTokenKind(kind)),
                        bOp.Right);
                }

                return SyntaxFactory.UnaryExpression(
                    SyntaxKind.NotExpression,
                    SyntaxFactory.Token(SyntaxKind.NotKeyword),
                    SyntaxFactory.ParenthesizedExpression(condition));
            }

            if (condition is TernaryConditionalExpressionSyntax) {
                var cEx = condition as TernaryConditionalExpressionSyntax;
                return cEx.WithCondition(InvertCondition(cEx.Condition));
            }

            if (condition is LiteralExpressionSyntax) {
                if (condition.Kind() == SyntaxKind.TrueLiteralExpression) {
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.FalseLiteralExpression,
                        SyntaxFactory.Token(SyntaxKind.FalseKeyword));
                }
                if (condition.Kind() == SyntaxKind.FalseLiteralExpression) {
                    return SyntaxFactory.LiteralExpression(
                        SyntaxKind.TrueLiteralExpression,
                        SyntaxFactory.Token(SyntaxKind.TrueKeyword));
                }
            }

            return SyntaxFactory.UnaryExpression(
                SyntaxKind.NotExpression,
                SyntaxFactory.Token(SyntaxKind.NotKeyword),
                AddParensForUnaryExpressionIfRequired(condition));
        }

        public static SyntaxKind GetExpressionOperatorTokenKind(this SyntaxKind op)
        {
            switch (op) {
                case SyntaxKind.EqualsExpression:
                    return SyntaxKind.EqualsToken;
                case SyntaxKind.NotEqualsExpression:
                    return SyntaxKind.LessThanGreaterThanToken;
                case SyntaxKind.GreaterThanExpression:
                    return SyntaxKind.GreaterThanToken;
                case SyntaxKind.GreaterThanOrEqualExpression:
                    return SyntaxKind.GreaterThanEqualsToken;
                case SyntaxKind.LessThanExpression:
                    return SyntaxKind.LessThanToken;
                case SyntaxKind.LessThanOrEqualExpression:
                    return SyntaxKind.LessThanEqualsToken;
                case SyntaxKind.OrExpression:
                    return SyntaxKind.OrKeyword;
                case SyntaxKind.ExclusiveOrExpression:
                    return SyntaxKind.XorKeyword;
                case SyntaxKind.OrElseExpression:
                    return SyntaxKind.OrElseKeyword;
                case SyntaxKind.AndExpression:
                    return SyntaxKind.AndKeyword;
                case SyntaxKind.AndAlsoExpression:
                    return SyntaxKind.AndAlsoKeyword;
                case SyntaxKind.AddExpression:
                    return SyntaxKind.PlusToken;
                case SyntaxKind.ConcatenateExpression:
                    return SyntaxKind.AmpersandToken;
                case SyntaxKind.SubtractExpression:
                    return SyntaxKind.MinusToken;
                case SyntaxKind.MultiplyExpression:
                    return SyntaxKind.AsteriskToken;
                case SyntaxKind.DivideExpression:
                    return SyntaxKind.SlashToken;
                case SyntaxKind.ModuloExpression:
                    return SyntaxKind.ModKeyword;
                case SyntaxKind.LeftShiftExpression:
                    return SyntaxKind.LessThanLessThanToken;
                case SyntaxKind.RightShiftExpression:
                    return SyntaxKind.GreaterThanGreaterThanToken;
                // assignments
                case SyntaxKind.SimpleAssignmentStatement:
                    return SyntaxKind.EqualsToken;
                case SyntaxKind.AddAssignmentStatement:
                    return SyntaxKind.PlusEqualsToken;
                case SyntaxKind.SubtractAssignmentStatement:
                    return SyntaxKind.MinusEqualsToken;
                case SyntaxKind.LeftShiftAssignmentStatement:
                    return SyntaxKind.LessThanLessThanEqualsToken;
                case SyntaxKind.RightShiftAssignmentStatement:
                    return SyntaxKind.GreaterThanGreaterThanEqualsToken;
                case SyntaxKind.ConcatenateAssignmentStatement:
                    return SyntaxKind.AmpersandEqualsToken;
                case SyntaxKind.MultiplyAssignmentStatement:
                    return SyntaxKind.AsteriskEqualsToken;
                case SyntaxKind.DivideAssignmentStatement:
                    return SyntaxKind.SlashEqualsToken;
                case SyntaxKind.ExponentiateAssignmentStatement:
                    return SyntaxKind.CaretEqualsToken;
                // unary
                case SyntaxKind.UnaryPlusExpression:
                    return SyntaxKind.PlusToken;
                case SyntaxKind.UnaryMinusExpression:
                    return SyntaxKind.MinusToken;
                case SyntaxKind.NotExpression:
                    return SyntaxKind.NotKeyword;
            }
            throw new ArgumentOutOfRangeException(nameof(op), op, null);
        }

        /// <summary>
        /// When negating an expression this is required, otherwise you would end up with
        /// a or b -> !a or b
        /// </summary>
        public static ExpressionSyntax AddParensForUnaryExpressionIfRequired(ExpressionSyntax expression)
        {
            if ((expression is BinaryExpressionSyntax) ||
                (expression is CastExpressionSyntax) ||
                (expression is LambdaExpressionSyntax)) {
                return SyntaxFactory.ParenthesizedExpression(expression);
            }

            return expression;
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
                case SyntaxKind.OrExpression:
                    return SyntaxKind.AndExpression;
                case SyntaxKind.OrElseExpression:
                    return SyntaxKind.AndAlsoExpression;
                case SyntaxKind.AndExpression:
                    return SyntaxKind.OrExpression;
                case SyntaxKind.AndAlsoExpression:
                    return SyntaxKind.OrElseExpression;
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
                case SyntaxKind.OrExpression:
                    return SyntaxKind.AndExpression;
                case SyntaxKind.OrElseExpression:
                    return SyntaxKind.AndAlsoExpression;
                case SyntaxKind.AndExpression:
                    return SyntaxKind.OrExpression;
                case SyntaxKind.AndAlsoExpression:
                    return SyntaxKind.OrElseExpression;
            }
            throw new ArgumentOutOfRangeException(nameof(op));
        }

        /// <summary>
        /// Returns true, if the specified operator is a relational operator
        /// </summary>
        public static bool IsRelationalOperator(SyntaxKind op)
        {
            switch (op) {
                case SyntaxKind.EqualsExpression:
                case SyntaxKind.NotEqualsExpression:
                case SyntaxKind.GreaterThanExpression:
                case SyntaxKind.GreaterThanOrEqualExpression:
                case SyntaxKind.LessThanExpression:
                case SyntaxKind.LessThanOrEqualExpression:
                case SyntaxKind.OrExpression:
                case SyntaxKind.OrElseExpression:
                case SyntaxKind.AndExpression:
                case SyntaxKind.AndAlsoExpression:
                    return true;
            }
            return false;
        }

        public static bool IsKind(this SyntaxNode node, params SyntaxKind[] kinds)
        {
            if (node == null) {
                return false;
            }

            var vbKind = node.Kind();
            return kinds.Any(k => vbKind == k);
        }

        public static SyntaxToken ConvertTypesTokenToKind(CS.SyntaxKind t, bool isXml = false)
        {
            switch (t) {
                case CS.SyntaxKind.None: return global::VisualBasicSyntaxFactory.EmptyToken;
                case CS.SyntaxKind.BoolKeyword: return global::VisualBasicSyntaxFactory.BooleanKeyword;
                case CS.SyntaxKind.ByteKeyword: return global::VisualBasicSyntaxFactory.ByteKeyword;
                case CS.SyntaxKind.SByteKeyword: return global::VisualBasicSyntaxFactory.SByteKeyword;
                case CS.SyntaxKind.ShortKeyword: return global::VisualBasicSyntaxFactory.ShortKeyword;
                case CS.SyntaxKind.UShortKeyword: return global::VisualBasicSyntaxFactory.UShortKeyword;
                case CS.SyntaxKind.IntKeyword: return global::VisualBasicSyntaxFactory.IntegerKeyword;
                case CS.SyntaxKind.UIntKeyword: return global::VisualBasicSyntaxFactory.UIntegerKeyword;
                case CS.SyntaxKind.LongKeyword: return global::VisualBasicSyntaxFactory.LongKeyword;
                case CS.SyntaxKind.ULongKeyword: return global::VisualBasicSyntaxFactory.ULongKeyword;
                case CS.SyntaxKind.DoubleKeyword: return global::VisualBasicSyntaxFactory.DoubleKeyword;
                case CS.SyntaxKind.FloatKeyword: return global::VisualBasicSyntaxFactory.SingleKeyword;
                case CS.SyntaxKind.DecimalKeyword: return global::VisualBasicSyntaxFactory.DecimalKeyword;
                case CS.SyntaxKind.StringKeyword: return global::VisualBasicSyntaxFactory.StringKeyword;
                case CS.SyntaxKind.CharKeyword: return global::VisualBasicSyntaxFactory.CharKeyword;
                case CS.SyntaxKind.VoidKeyword:                         // not supported
                    if (isXml) {
                        return global::VisualBasicSyntaxFactory.NothingKeyword;
                    }
                    return global::VisualBasicSyntaxFactory.EmptyToken;
                case CS.SyntaxKind.ObjectKeyword: return global::VisualBasicSyntaxFactory.ObjectKeyword;
            }

            throw new NotSupportedException($"Type.Kind {t} is not supported!");
        }
    }
}
