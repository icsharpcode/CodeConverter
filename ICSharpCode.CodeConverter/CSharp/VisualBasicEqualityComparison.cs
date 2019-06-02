using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualBasic.CompilerServices;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;

namespace ICSharpCode.CodeConverter.CSharp
{

    /// <summary>
    /// The equals and not equals operators in Visual Basic call ConditionalCompareObjectEqual.
    /// This method allows a sort of best effort comparison of different types.
    /// There are therefore some surprising results such as "" = Nothing being true.
    /// Here we try to coerce the arguments for the CSharp equals method to get as close to the runtime behaviour as possible without inlining hundreds of lines of code.
    /// </summary>
    internal class VisualBasicEqualityComparison
    {
        private readonly SemanticModel _semanticModel;
        private readonly HashSet<string> _extraUsingDirectives;

        public VisualBasicEqualityComparison(SemanticModel semanticModel, HashSet<string> extraUsingDirectives, bool optionCompareTextCaseInsensitive)
        {
            OptionCompareTextCaseInsensitive = optionCompareTextCaseInsensitive;
            _extraUsingDirectives = extraUsingDirectives;
            _semanticModel = semanticModel;
        }

        public enum RequiredType
        {
            None,
            StringOnly,
            Object
        }

        public bool OptionCompareTextCaseInsensitive { get; }
        
        public RequiredType GetObjectEqualityType(VBSyntax.BinaryExpressionSyntax node, TypeInfo leftType, TypeInfo rightType)
        {
            var typeInfos = new[] {leftType, rightType};
            if (!node.IsKind(VBasic.SyntaxKind.EqualsExpression, VBasic.SyntaxKind.NotEqualsExpression) ||
                typeInfos.Any(t => t.Type == null)) {
                return RequiredType.None;
            }

            if (typeInfos.All(t => t.Type.SpecialType == SpecialType.System_Object)) return RequiredType.Object;

            if (typeInfos.All(
                t => t.Type.SpecialType == SpecialType.System_String ||
                     t.Type.SpecialType == SpecialType.System_Object ||
                     t.Type.IsArrayOf(SpecialType.System_Char))) {
                return RequiredType.StringOnly;
            };

            return RequiredType.None;
        }

        public ExpressionSyntax VbCoerceToString(ExpressionSyntax baseExpression, TypeInfo typeInfo)
        {
            bool isStringType = typeInfo.Type.SpecialType == SpecialType.System_String;
            bool isCharArray = typeInfo.Type.IsArrayOf(SpecialType.System_Char);
            var constantValue = _semanticModel.GetConstantValue(baseExpression);
            if (!constantValue.HasValue) {
                if (isStringType) {
                    baseExpression = SyntaxFactory.ParenthesizedExpression(Coalesce(baseExpression, EmptyStringExpression()));
                } else
                {
                    var baseExpressionAsCharArray = SyntaxFactory.BinaryExpression(
                        SyntaxKind.AsExpression,
                        SyntaxFactory.ParenthesizedExpression(baseExpression),
                        CharArrayType().WithRankSpecifiers(ArrayRankSpecifier(SyntaxFactory.OmittedArraySizeExpression())));
                    baseExpression = Coalesce(baseExpressionAsCharArray, EmptyCharArrayExpression());
                }
            } else if (constantValue.Value == null) {
                baseExpression = EmptyStringExpression();
            }

            return isStringType || !isCharArray ? baseExpression : NewStringFromArg(baseExpression);
        }


        private static ExpressionSyntax Coalesce(ExpressionSyntax lhs, ExpressionSyntax emptyString)
        {
            return SyntaxFactory.BinaryExpression(SyntaxKind.CoalesceExpression, lhs, emptyString);
        }

        private static ArrayCreationExpressionSyntax EmptyCharArrayExpression()
        {
            var literalExpressionSyntax =
                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0));
            var arrayRankSpecifierSyntax = ArrayRankSpecifier(literalExpressionSyntax);
            var arrayTypeSyntax = CharArrayType().WithRankSpecifiers(arrayRankSpecifierSyntax);
            return SyntaxFactory.ArrayCreationExpression(arrayTypeSyntax);
        }

        private static SyntaxList<ArrayRankSpecifierSyntax> ArrayRankSpecifier(ExpressionSyntax expressionSyntax)
        {
            var literalExpressionSyntaxList = SyntaxFactory.SingletonSeparatedList(expressionSyntax);
            var arrayRankSpecifierSyntax = SyntaxFactory.ArrayRankSpecifier(literalExpressionSyntaxList);
            return SyntaxFactory.SingletonList(arrayRankSpecifierSyntax);
        }

        private static ArrayTypeSyntax CharArrayType()
        {
            return SyntaxFactory.ArrayType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.CharKeyword)));
        }

        private static LiteralExpressionSyntax EmptyStringExpression()
        {
            return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(""));
        }

        private static ObjectCreationExpressionSyntax NewStringFromArg(ExpressionSyntax lhs)
        {
            return SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                ExpressionSyntaxExtensions.CreateArgList(lhs), default(InitializerExpressionSyntax));
        }

        public static bool TryConvertToNullOrEmptyCheck(VBSyntax.BinaryExpressionSyntax node, ExpressionSyntax lhs,
            ExpressionSyntax rhs, out CSharpSyntaxNode visitBinaryExpression)
        {
            bool lhsEmpty = lhs is LiteralExpressionSyntax les &&
                            (les.IsKind(SyntaxKind.NullLiteralExpression) ||
                             (les.IsKind(SyntaxKind.StringLiteralExpression) &&
                              String.IsNullOrEmpty(les.Token.ValueText)));
            bool rhsEmpty = rhs is LiteralExpressionSyntax res &&
                            (res.IsKind(SyntaxKind.NullLiteralExpression) ||
                             (res.IsKind(SyntaxKind.StringLiteralExpression) &&
                              String.IsNullOrEmpty(res.Token.ValueText)));

            if (lhsEmpty || rhsEmpty)
            {
                var arg = lhsEmpty ? rhs : lhs;
                var nullOrEmpty = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("string"),
                        SyntaxFactory.IdentifierName("IsNullOrEmpty")),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[]
                        {SyntaxFactory.Argument(arg)})));
                {
                    visitBinaryExpression = NegateIfNeeded(node, nullOrEmpty);
                    return true;
                }
            }

            visitBinaryExpression = null;
            return false;
        }

        private static ExpressionSyntax NegateIfNeeded(VBSyntax.BinaryExpressionSyntax node, InvocationExpressionSyntax positiveExpression)
        {
            return node.IsKind(VBasic.SyntaxKind.EqualsExpression)
                ? (ExpressionSyntax) positiveExpression
                : SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, positiveExpression);
        }

        public (ExpressionSyntax lhs, ExpressionSyntax rhs) AdjustForVbStringComparison(ExpressionSyntax lhs, ExpressionSyntax rhs)
        {
            if (OptionCompareTextCaseInsensitive) {
                _extraUsingDirectives.Add("System.Globalization");
                var compareOptions = SyntaxFactory.Argument(GetCompareTextCaseInsensitiveCompareOptions());
                var compareString = SyntaxFactory.InvocationExpression(
                    MemberAccess(nameof(CultureInfo), nameof(CultureInfo.CurrentCulture),
                        nameof(CultureInfo.CompareInfo), nameof(CompareInfo.Compare)),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[]
                        {SyntaxFactory.Argument(lhs), SyntaxFactory.Argument(rhs), compareOptions})));
                lhs = compareString;
                rhs = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(0));
            }

            return (lhs, rhs);
        }

        public ExpressionSyntax GetFullExpressionForVbObjectComparison(VBSyntax.BinaryExpressionSyntax node, ExpressionSyntax lhs, ExpressionSyntax rhs)
        {
                _extraUsingDirectives.Add("Microsoft.VisualBasic.CompilerServices");
                var optionCompareTextCaseInsensitive = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(OptionCompareTextCaseInsensitive ? SyntaxKind.TrueKeyword : SyntaxKind.FalseLiteralExpression));
                var compareObject = SyntaxFactory.InvocationExpression(
                    MemberAccess(nameof(Operators), nameof(Operators.ConditionalCompareObjectEqual)),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[]
                        {SyntaxFactory.Argument(lhs), SyntaxFactory.Argument(rhs), optionCompareTextCaseInsensitive})));
                return NegateIfNeeded(node, compareObject);
        }

        private static BinaryExpressionSyntax GetCompareTextCaseInsensitiveCompareOptions()
        {
            return SyntaxFactory.BinaryExpression(SyntaxKind.BitwiseOrExpression,
                SyntaxFactory.BinaryExpression(SyntaxKind.BitwiseOrExpression,
                    MemberAccess(nameof(CompareOptions), nameof(CompareOptions.IgnoreCase)),
                    MemberAccess(nameof(CompareOptions), nameof(CompareOptions.IgnoreKanaType))),
                MemberAccess(nameof(CompareOptions), nameof(CompareOptions.IgnoreWidth))
            );
        }

        private static ExpressionSyntax MemberAccess(params string[] nameParts)
        {
            ExpressionSyntax lhs = null;
            foreach (var namePart in nameParts) {
                if (lhs == null) lhs = SyntaxFactory.IdentifierName(namePart);
                else {
                    lhs = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        lhs, SyntaxFactory.IdentifierName(namePart));
                }
            }

            return lhs;
        }

    }
}