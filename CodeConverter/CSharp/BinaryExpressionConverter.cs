using ICSharpCode.CodeConverter.Util.FromRoslyn;

namespace ICSharpCode.CodeConverter.CSharp;

internal class BinaryExpressionConverter
{
    private readonly SemanticModel _semanticModel;
    private readonly IOperatorConverter _operatorConverter;
    private readonly VisualBasicEqualityComparison _visualBasicEqualityComparison;
    private readonly VisualBasicNullableExpressionsConverter _visualBasicNullableTypesConverter;
    public CommonConversions CommonConversions { get; }

    public CommentConvertingVisitorWrapper TriviaConvertingExpressionVisitor { get; }

    public BinaryExpressionConverter(SemanticModel semanticModel, IOperatorConverter operatorConverter, VisualBasicEqualityComparison visualBasicEqualityComparison,
        VisualBasicNullableExpressionsConverter visualBasicNullableTypesConverter, CommonConversions commonConversions)
    {
        CommonConversions = commonConversions;
        _semanticModel = semanticModel;
        _operatorConverter = operatorConverter;
        _visualBasicEqualityComparison = visualBasicEqualityComparison;
        _visualBasicNullableTypesConverter = visualBasicNullableTypesConverter;
        TriviaConvertingExpressionVisitor = commonConversions.TriviaConvertingExpressionVisitor;
    }

    public async Task<CSharpSyntaxNode> ConvertBinaryExpressionAsync(VBSyntax.BinaryExpressionSyntax entryNode)
    {
        // Walk down the syntax tree for deeply nested binary expressions to avoid stack overflow
        // e.g. 3 + 4 + 5 + ...
        // Test "DeeplyNestedBinaryExpressionShouldNotStackOverflowAsync()" skipped because it's too slow

        CSSyntax.ExpressionSyntax csLhs = null;
        int levelsToConvert = 0;
        VBSyntax.BinaryExpressionSyntax currentNode = entryNode;

        // Walk down the nested levels to count them
        for (var nextNode = entryNode; nextNode != null; currentNode = nextNode, nextNode = currentNode.Left as VBSyntax.BinaryExpressionSyntax, levelsToConvert++) {
            // Don't go beyond a rewritten operator because that code has many paths that can call VisitBinaryExpression. Passing csLhs through all of that would harm the code quality more than it's worth to help that edge case.
            if (await RewriteBinaryOperatorOrNullAsync(nextNode) is { } operatorNode) {
                csLhs = operatorNode;
                break;
            }
        }

        // Walk back up the same levels converting as we go.
        for (; levelsToConvert > 0; currentNode = currentNode!.Parent as VBSyntax.BinaryExpressionSyntax, levelsToConvert--) {
            csLhs = (CSSyntax.ExpressionSyntax)await ConvertBinaryExpressionAsync(currentNode, csLhs);
        }

        return csLhs;
    }

    private async Task<CSharpSyntaxNode> ConvertBinaryExpressionAsync(VBasic.Syntax.BinaryExpressionSyntax node, CSSyntax.ExpressionSyntax lhs, CSSyntax.ExpressionSyntax rhs = null)
    {
        lhs ??= await node.Left.AcceptAsync<CSSyntax.ExpressionSyntax>(TriviaConvertingExpressionVisitor);
        rhs ??= await node.Right.AcceptAsync<CSSyntax.ExpressionSyntax>(TriviaConvertingExpressionVisitor);

        var lhsTypeInfo = _semanticModel.GetTypeInfo(node.Left);
        var rhsTypeInfo = _semanticModel.GetTypeInfo(node.Right);

        ITypeSymbol forceLhsTargetType = null;
        bool omitRightConversion = false;
        bool omitConversion = false;
        if (lhsTypeInfo.Type != null && rhsTypeInfo.Type != null)
        {
            if (node.IsKind(VBasic.SyntaxKind.ConcatenateExpression) && 
                !lhsTypeInfo.Type.IsEnumType() && !rhsTypeInfo.Type.IsEnumType() && 
                !lhsTypeInfo.Type.IsDateType() && !rhsTypeInfo.Type.IsDateType())
            {
                omitRightConversion = true;
                omitConversion = lhsTypeInfo.Type.SpecialType == SpecialType.System_String ||
                                 rhsTypeInfo.Type.SpecialType == SpecialType.System_String;
                if (lhsTypeInfo.ConvertedType.SpecialType != SpecialType.System_String) {
                    forceLhsTargetType = CommonConversions.KnownTypes.String;
                }
            }
        }

        var objectEqualityType = _visualBasicEqualityComparison.GetObjectEqualityType(node, lhsTypeInfo, rhsTypeInfo);

        switch (objectEqualityType) {
            case VisualBasicEqualityComparison.RequiredType.StringOnly:
                if (lhsTypeInfo.ConvertedType?.SpecialType == SpecialType.System_String &&
                    rhsTypeInfo.ConvertedType?.SpecialType == SpecialType.System_String &&
                    _visualBasicEqualityComparison.TryConvertToNullOrEmptyCheck(node, lhs, rhs, out CSharpSyntaxNode visitBinaryExpression)) {
                    return visitBinaryExpression;
                }
                (lhs, rhs) = _visualBasicEqualityComparison.AdjustForVbStringComparison(node.Left, lhs, lhsTypeInfo, false, node.Right, rhs, rhsTypeInfo, false);
                omitConversion = true; // Already handled within for the appropriate types (rhs can become int in comparison)
                break;
            case VisualBasicEqualityComparison.RequiredType.Object:
                return _visualBasicEqualityComparison.GetFullExpressionForVbObjectComparison(lhs, rhs, VisualBasicEqualityComparison.ComparisonKind.Equals, node.IsKind(VBasic.SyntaxKind.NotEqualsExpression));
        }

        var lhsTypeIgnoringNullable = lhsTypeInfo.Type.GetNullableUnderlyingType() ?? lhsTypeInfo.Type;
        var rhsTypeIgnoringNullable = rhsTypeInfo.Type.GetNullableUnderlyingType() ?? rhsTypeInfo.Type;
        omitConversion |= lhsTypeIgnoringNullable != null && rhsTypeIgnoringNullable != null &&
                          lhsTypeIgnoringNullable.IsEnumType() && SymbolEqualityComparer.Default.Equals(lhsTypeIgnoringNullable, rhsTypeIgnoringNullable)
                          && !node.IsKind(VBasic.SyntaxKind.AddExpression, VBasic.SyntaxKind.SubtractExpression, VBasic.SyntaxKind.MultiplyExpression, VBasic.SyntaxKind.DivideExpression, VBasic.SyntaxKind.IntegerDivideExpression, VBasic.SyntaxKind.ModuloExpression)
                          && forceLhsTargetType == null;
        lhs = omitConversion ? lhs : CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Left, lhs, forceTargetType: forceLhsTargetType);
        rhs = omitConversion || omitRightConversion ? rhs : CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Right, rhs);

        var kind = VBasic.VisualBasicExtensions.Kind(node).ConvertToken();
        var op = CS.SyntaxFactory.Token(CSharpUtil.GetExpressionOperatorTokenKind(kind));

        var csBinExp = CS.SyntaxFactory.BinaryExpression(kind, lhs, op, rhs);
        var exp = _visualBasicNullableTypesConverter.WithBinaryExpressionLogicForNullableTypes(node, lhsTypeInfo, rhsTypeInfo, csBinExp, lhs, rhs);
        return node.Parent.IsKind(VBasic.SyntaxKind.SimpleArgument) ? exp : exp.AddParens();
    }

    private async Task<CSSyntax.ExpressionSyntax> RewriteBinaryOperatorOrNullAsync(VBSyntax.BinaryExpressionSyntax node) =>
        await _operatorConverter.ConvertRewrittenBinaryOperatorOrNullAsync(node, TriviaConvertingExpressionVisitor.IsWithinQuery);
}