using Microsoft.CodeAnalysis.VisualBasic;

namespace ICSharpCode.CodeConverter.Util;

internal static class VBUtil
{
    public static SyntaxKind GetExpressionOperatorTokenKind(this SyntaxKind op)
    {
        return op switch {
            SyntaxKind.EqualsExpression => SyntaxKind.EqualsToken,
            SyntaxKind.NotEqualsExpression => SyntaxKind.LessThanGreaterThanToken,
            SyntaxKind.GreaterThanExpression => SyntaxKind.GreaterThanToken,
            SyntaxKind.GreaterThanOrEqualExpression => SyntaxKind.GreaterThanEqualsToken,
            SyntaxKind.LessThanExpression => SyntaxKind.LessThanToken,
            SyntaxKind.LessThanOrEqualExpression => SyntaxKind.LessThanEqualsToken,
            SyntaxKind.OrExpression => SyntaxKind.OrKeyword,
            SyntaxKind.ExclusiveOrExpression => SyntaxKind.XorKeyword,
            SyntaxKind.OrElseExpression => SyntaxKind.OrElseKeyword,
            SyntaxKind.AndExpression => SyntaxKind.AndKeyword,
            SyntaxKind.AndAlsoExpression => SyntaxKind.AndAlsoKeyword,
            SyntaxKind.AddExpression => SyntaxKind.PlusToken,
            SyntaxKind.ConcatenateExpression => SyntaxKind.AmpersandToken,
            SyntaxKind.SubtractExpression => SyntaxKind.MinusToken,
            SyntaxKind.MultiplyExpression => SyntaxKind.AsteriskToken,
            SyntaxKind.DivideExpression => SyntaxKind.SlashToken,
            SyntaxKind.ModuloExpression => SyntaxKind.ModKeyword,
            SyntaxKind.LeftShiftExpression => SyntaxKind.LessThanLessThanToken,
            SyntaxKind.RightShiftExpression => SyntaxKind.GreaterThanGreaterThanToken,
            // assignments
            SyntaxKind.SimpleAssignmentStatement => SyntaxKind.EqualsToken,
            SyntaxKind.AddAssignmentStatement => SyntaxKind.PlusEqualsToken,
            SyntaxKind.SubtractAssignmentStatement => SyntaxKind.MinusEqualsToken,
            SyntaxKind.LeftShiftAssignmentStatement => SyntaxKind.LessThanLessThanEqualsToken,
            SyntaxKind.RightShiftAssignmentStatement => SyntaxKind.GreaterThanGreaterThanEqualsToken,
            SyntaxKind.ConcatenateAssignmentStatement => SyntaxKind.AmpersandEqualsToken,
            SyntaxKind.MultiplyAssignmentStatement => SyntaxKind.AsteriskEqualsToken,
            SyntaxKind.DivideAssignmentStatement => SyntaxKind.SlashEqualsToken,
            SyntaxKind.ExponentiateAssignmentStatement => SyntaxKind.CaretEqualsToken,
            // unary
            SyntaxKind.UnaryPlusExpression => SyntaxKind.PlusToken,
            SyntaxKind.UnaryMinusExpression => SyntaxKind.MinusToken,
            SyntaxKind.NotExpression => SyntaxKind.NotKeyword,
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
        };
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
            case CS.SyntaxKind.None: return VB.Trivia.VisualBasicSyntaxFactory.EmptyToken;
            case CS.SyntaxKind.BoolKeyword: return VB.Trivia.VisualBasicSyntaxFactory.BooleanKeyword;
            case CS.SyntaxKind.ByteKeyword: return VB.Trivia.VisualBasicSyntaxFactory.ByteKeyword;
            case CS.SyntaxKind.SByteKeyword: return VB.Trivia.VisualBasicSyntaxFactory.SByteKeyword;
            case CS.SyntaxKind.ShortKeyword: return VB.Trivia.VisualBasicSyntaxFactory.ShortKeyword;
            case CS.SyntaxKind.UShortKeyword: return VB.Trivia.VisualBasicSyntaxFactory.UShortKeyword;
            case CS.SyntaxKind.IntKeyword: return VB.Trivia.VisualBasicSyntaxFactory.IntegerKeyword;
            case CS.SyntaxKind.UIntKeyword: return VB.Trivia.VisualBasicSyntaxFactory.UIntegerKeyword;
            case CS.SyntaxKind.LongKeyword: return VB.Trivia.VisualBasicSyntaxFactory.LongKeyword;
            case CS.SyntaxKind.ULongKeyword: return VB.Trivia.VisualBasicSyntaxFactory.ULongKeyword;
            case CS.SyntaxKind.DoubleKeyword: return VB.Trivia.VisualBasicSyntaxFactory.DoubleKeyword;
            case CS.SyntaxKind.FloatKeyword: return VB.Trivia.VisualBasicSyntaxFactory.SingleKeyword;
            case CS.SyntaxKind.DecimalKeyword: return VB.Trivia.VisualBasicSyntaxFactory.DecimalKeyword;
            case CS.SyntaxKind.StringKeyword: return VB.Trivia.VisualBasicSyntaxFactory.StringKeyword;
            case CS.SyntaxKind.CharKeyword: return VB.Trivia.VisualBasicSyntaxFactory.CharKeyword;
            case CS.SyntaxKind.VoidKeyword:                         // not supported
                if (isXml) {
                    return VB.Trivia.VisualBasicSyntaxFactory.NothingKeyword;
                }
                return VB.Trivia.VisualBasicSyntaxFactory.EmptyToken;
            case CS.SyntaxKind.ObjectKeyword: return VB.Trivia.VisualBasicSyntaxFactory.ObjectKeyword;
        }

        throw new NotSupportedException($"Type.Kind {t} is not supported!");
    }
}