using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualBasic.CompilerServices;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using VBSyntaxKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;

namespace ICSharpCode.CodeConverter.CSharp;

/// <summary>
/// The equals and not equals operators in Visual Basic call ConditionalCompareObjectEqual.
/// This method allows a sort of best effort comparison of different types.
/// There are therefore some surprising results such as "" = Nothing being true.
/// Here we try to coerce the arguments for the CSharp equals method to get as close to the runtime behaviour as possible without inlining hundreds of lines of code.
/// </summary>
internal class VisualBasicEqualityComparison
{
    private readonly SemanticModel _semanticModel;

    public VisualBasicEqualityComparison(SemanticModel semanticModel, HashSet<string> extraUsingDirectives)
    {
        ExtraUsingDirectives = extraUsingDirectives;
        _semanticModel = semanticModel;
    }

    public enum RequiredType
    {
        None,
        StringOnly,
        Object
    }

    public bool OptionCompareTextCaseInsensitive { get; set; }

    public LiteralExpressionSyntax OptionCompareTextCaseInsensitiveBoolExpression {
        get {
            var compareTextKind = OptionCompareTextCaseInsensitive ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression;
            return SyntaxFactory.LiteralExpression(compareTextKind);
        }
    }

    public ExpressionSyntax CompareMethodExpression {
        get {
            var compareTextKind = OptionCompareTextCaseInsensitive ? "Text" : "Binary";
            var compareMethod = ValidSyntaxFactory.MemberAccess(nameof(Microsoft), nameof(Microsoft.VisualBasic), "CompareMethod", compareTextKind);
            return compareMethod;
        }
    }

    public HashSet<string> ExtraUsingDirectives { get; }

    public static RequiredType GetObjectEqualityType(VBSyntax.BinaryExpressionSyntax node, TypeInfo leftType, TypeInfo rightType)
    {
        var typeInfos = new[] { leftType, rightType };
        if (!node.IsKind(VBasic.SyntaxKind.EqualsExpression, VBasic.SyntaxKind.NotEqualsExpression)) {
            return RequiredType.None;
        }
        return GetObjectEqualityType(typeInfos);
    }

    public static RequiredType GetObjectEqualityType(params TypeInfo[] typeInfos)
    {
        bool requiresVbEqualityCheck = typeInfos.Any(t => t.Type?.SpecialType == SpecialType.System_Object);

        if (typeInfos.All(
                t => t.Type == null || t.Type.SpecialType == SpecialType.System_String ||
                     t.Type.IsArrayOf(SpecialType.System_Char) ) ) {
            return RequiredType.StringOnly;
        };

        return requiresVbEqualityCheck ? RequiredType.Object : RequiredType.None;
    }


    public (ExpressionSyntax lhs, ExpressionSyntax rhs) VbCoerceToNonNullString(VBSyntax.ExpressionSyntax vbLeft, ExpressionSyntax csLeft, TypeInfo lhsTypeInfo, bool leftKnownNotNull, VBSyntax.ExpressionSyntax vbRight, ExpressionSyntax csRight, TypeInfo rhsTypeInfo, bool rightKnownNotNull)
    {
        if (IsNonEmptyStringLiteral(vbLeft) || IsNonEmptyStringLiteral(vbRight)) {
            // If one of the strings is "foo", the other string won't equal it if it's null or empty, so it doesn't matter which one we use in comparison
            return (csLeft, csRight);
        }
        return (VbCoerceToNonNullString(vbLeft, csLeft, lhsTypeInfo, leftKnownNotNull), VbCoerceToNonNullString(vbRight, csRight, rhsTypeInfo, rightKnownNotNull));
    }

    private static bool IsNonEmptyStringLiteral(VBSyntax.ExpressionSyntax vbExpr)
    {
        vbExpr = vbExpr.SkipIntoParens();
        return vbExpr.IsKind(VBSyntaxKind.StringLiteralExpression) && vbExpr is VBSyntax.LiteralExpressionSyntax literal && !IsEmptyString(literal);
    }

    public ExpressionSyntax VbCoerceToNonNullString(VBSyntax.ExpressionSyntax vbNode, ExpressionSyntax csNode, TypeInfo typeInfo, bool knownNotNull = false)
    {
        if (IsNothingOrEmpty(vbNode)) {
            return EmptyStringExpression();
        }

        if (knownNotNull || !CanBeNull(vbNode)) {
            return csNode;
        }

        csNode = typeInfo.Type?.SpecialType == SpecialType.System_String
            ? Coalesce(csNode, EmptyStringExpression()).AddParens()
            : Coalesce(csNode, EmptyCharArrayExpression());

        return VbCoerceToString(csNode, typeInfo);
    }

    public static ExpressionSyntax VbCoerceToString(ExpressionSyntax csNode, TypeInfo typeInfo)
    {
        return typeInfo.Type?.SpecialType switch
        {
            SpecialType.System_String => csNode,
            SpecialType.System_Char => SyntaxFactory.InvocationExpression(ValidSyntaxFactory.MemberAccess(csNode, nameof(ToString))),
            _ => NewStringFromArg(csNode)
        };
    }

    private bool CanBeNull(VBSyntax.ExpressionSyntax vbNode)
    {
        if (vbNode.IsKind(VBSyntaxKind.StringLiteralExpression)) return false;
        var constantAnalysis = _semanticModel.GetConstantValue(vbNode);
        if (constantAnalysis.HasValue && constantAnalysis.Value != null) return false;
        return true;
    }


    private static ExpressionSyntax Coalesce(ExpressionSyntax lhs, ExpressionSyntax emptyString)
    {
        return SyntaxFactory.BinaryExpression(SyntaxKind.CoalesceExpression, lhs, emptyString);
    }

    private static ExpressionSyntax EmptyCharArrayExpression()
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

    private static ExpressionSyntax EmptyStringExpression()
    {
        return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(""));
    }

    private static ObjectCreationExpressionSyntax NewStringFromArg(ExpressionSyntax lhs)
    {
        return SyntaxFactory.ObjectCreationExpression(
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
            ExpressionSyntaxExtensions.CreateArgList(lhs), default(InitializerExpressionSyntax));
    }

    public bool TryConvertToNullOrEmptyCheck(VBSyntax.BinaryExpressionSyntax node, ExpressionSyntax lhs,
        ExpressionSyntax rhs, out CSharpSyntaxNode visitBinaryExpression)
    {
        bool lhsEmpty = IsNothingOrEmpty(node.Left);
        bool rhsEmpty = IsNothingOrEmpty(node.Right);

        if (lhsEmpty || rhsEmpty)
        {
            var arg = lhsEmpty ? rhs : lhs;
            var nullOrEmpty = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
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

    private bool IsNothingOrEmpty(VBSyntax.ExpressionSyntax expressionSyntax)
    {
        expressionSyntax = expressionSyntax.SkipIntoParens();

        if (expressionSyntax is VBSyntax.LiteralExpressionSyntax les &&
            (les.IsKind(VBSyntaxKind.NothingLiteralExpression) || IsEmptyString(les))) {
            return true;
        }

        bool IsStringEmptyExpression() =>
            expressionSyntax is VBSyntax.MemberAccessExpressionSyntax memberAccess && memberAccess.IsKind(VBSyntaxKind.SimpleMemberAccessExpression) &&
            memberAccess.Expression is VBSyntax.PredefinedTypeSyntax predefinedType && predefinedType.Keyword.IsKind(VBSyntaxKind.StringKeyword) &&
            memberAccess.Name.Identifier.ValueText.Equals("Empty", StringComparison.OrdinalIgnoreCase);

        if (IsStringEmptyExpression()) {
            return true;
        }

        var constantAnalysis = _semanticModel.GetConstantValue(expressionSyntax);
        return constantAnalysis.HasValue && (constantAnalysis.Value == null || constantAnalysis.Value as string == "");


    }

    private static bool IsEmptyString(VBSyntax.LiteralExpressionSyntax les)
    {
        return les.IsKind(VBSyntaxKind.StringLiteralExpression) && string.IsNullOrEmpty(les.Token.ValueText);
    }

    private static ExpressionSyntax NegateIfNeeded(VBSyntax.BinaryExpressionSyntax node, InvocationExpressionSyntax positiveExpression)
    {
        return node.IsKind(VBasic.SyntaxKind.NotEqualsExpression)
            ? Negate(positiveExpression)
            : positiveExpression;
    }

    private static PrefixUnaryExpressionSyntax Negate(InvocationExpressionSyntax positiveExpression)
    {
        return SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, positiveExpression);
    }

    public (ExpressionSyntax csLeft, ExpressionSyntax csRight) AdjustForVbStringComparison(VBSyntax.ExpressionSyntax vbLeft, ExpressionSyntax csLeft, TypeInfo lhsTypeInfo, bool leftKnownNotNull, VBSyntax.ExpressionSyntax vbRight, ExpressionSyntax csRight, TypeInfo rhsTypeInfo, bool rightKnownNotNull)
    {
        (csLeft, csRight) = VbCoerceToNonNullString(vbLeft, csLeft, lhsTypeInfo, leftKnownNotNull, vbRight, csRight, rhsTypeInfo, rightKnownNotNull);
        if (OptionCompareTextCaseInsensitive) {
            ExtraUsingDirectives.Add("System.Globalization");
            var compareOptions = SyntaxFactory.Argument(GetCompareTextCaseInsensitiveCompareOptions());
            var compareString = SyntaxFactory.InvocationExpression(ValidSyntaxFactory.MemberAccess(nameof(CultureInfo), nameof(CultureInfo.CurrentCulture),
                    nameof(CultureInfo.CompareInfo), nameof(CompareInfo.Compare)),
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[]
                    {SyntaxFactory.Argument(csLeft), SyntaxFactory.Argument(csRight), compareOptions})));

            csLeft = compareString;
            csRight = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                SyntaxFactory.Literal(0));
        }

        return (csLeft, csRight);
    }

    public ExpressionSyntax GetFullExpressionForVbObjectComparison(ExpressionSyntax lhs, ExpressionSyntax rhs, bool negate = false)
    {
        ExtraUsingDirectives.Add("Microsoft.VisualBasic.CompilerServices");
        var optionCompareTextCaseInsensitive = SyntaxFactory.Argument(OptionCompareTextCaseInsensitiveBoolExpression);
        var compareObject = SyntaxFactory.InvocationExpression(ValidSyntaxFactory.MemberAccess(nameof(Operators), nameof(Operators.ConditionalCompareObjectEqual)),
            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[]
                {SyntaxFactory.Argument(lhs), SyntaxFactory.Argument(rhs), optionCompareTextCaseInsensitive})));
        return negate ? Negate(compareObject) : compareObject;
    }

    private static BinaryExpressionSyntax GetCompareTextCaseInsensitiveCompareOptions()
    {
        return SyntaxFactory.BinaryExpression(SyntaxKind.BitwiseOrExpression,
            SyntaxFactory.BinaryExpression(SyntaxKind.BitwiseOrExpression, ValidSyntaxFactory.MemberAccess(nameof(CompareOptions), nameof(CompareOptions.IgnoreCase)), ValidSyntaxFactory.MemberAccess(nameof(CompareOptions), nameof(CompareOptions.IgnoreKanaType))), ValidSyntaxFactory.MemberAccess(nameof(CompareOptions), nameof(CompareOptions.IgnoreWidth))
        );
    }
}