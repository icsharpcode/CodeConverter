using System.Collections.Immutable;
using System.Data;
using System.Globalization;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using ICSharpCode.CodeConverter.CSharp.Replacements;
using ICSharpCode.CodeConverter.Util.FromRoslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using ComparisonKind = ICSharpCode.CodeConverter.CSharp.VisualBasicEqualityComparison.ComparisonKind;
// Required for Task<CSharpSyntaxNode>
using System.Threading.Tasks;
// Required for NotImplementedException
using System;
using System.Linq; // Added for Enumerable.ElementAtOrDefault
using System.Collections.Generic; // Required for Stack, Dictionary, HashSet

namespace ICSharpCode.CodeConverter.CSharp;

internal partial class ExpressionNodeVisitor
{
    private readonly IOperatorConverter _operatorConverter;
    private readonly VisualBasicEqualityComparison _visualBasicEqualityComparison;
    private readonly Stack<ExpressionSyntax> _withBlockLhs = new();
    private readonly QueryConverter _queryConverter;
    private readonly Lazy<IReadOnlyDictionary<ITypeSymbol, string>> _convertMethodsLookupByReturnType;
    private readonly VisualBasicNullableExpressionsConverter _visualBasicNullableTypesConverter;
    private readonly Dictionary<string, Stack<(SyntaxNode Scope, string TempName)>> _tempNameForAnonymousScope = new();
    private readonly HashSet<string> _generatedNames = new(StringComparer.OrdinalIgnoreCase);

    public override async Task<CSharpSyntaxNode> VisitLiteralExpression(VBasic.Syntax.LiteralExpressionSyntax node)
    {
        var typeInfo = _semanticModel.GetTypeInfo(node);
        var convertedType = typeInfo.ConvertedType;
        if (node.Token.Value == null) {
            if (convertedType == null) {
                return SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression);
            }

            return !convertedType.IsReferenceType ? SyntaxFactory.DefaultExpression(CommonConversions.GetTypeSyntax(convertedType)) : CommonConversions.Literal(null);
        }

        if (TypeConversionAnalyzer.ConvertStringToCharLiteral(node, convertedType, out char chr)) {
            return CommonConversions.Literal(chr);
        }


        var val = node.Token.Value;
        var text = node.Token.Text;
        if (_typeContext.Any() && CommonConversions.WinformsConversions.ShouldPrefixAssignedNameWithUnderscore(node.Parent as VBSyntax.AssignmentStatementSyntax) && val is string valStr) {
            val = "_" + valStr;
            text = "\"_" + valStr + "\"";
        }

        return CommonConversions.Literal(val, text, convertedType);
    }

    public override async Task<CSharpSyntaxNode> VisitInterpolation(VBasic.Syntax.InterpolationSyntax node)
    {
        return SyntaxFactory.Interpolation(await node.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor), await node.AlignmentClause.AcceptAsync<InterpolationAlignmentClauseSyntax>(TriviaConvertingExpressionVisitor), await node.FormatClause.AcceptAsync<InterpolationFormatClauseSyntax>(TriviaConvertingExpressionVisitor));
    }

    public override async Task<CSharpSyntaxNode> VisitInterpolatedStringExpression(VBasic.Syntax.InterpolatedStringExpressionSyntax node)
    {
        var useVerbatim = node.DescendantNodes().OfType<VBasic.Syntax.InterpolatedStringTextSyntax>().Any(c => LiteralConversions.IsWorthBeingAVerbatimString(c.TextToken.Text));
        var startToken = useVerbatim ?
            SyntaxFactory.Token(default(SyntaxTriviaList), SyntaxKind.InterpolatedVerbatimStringStartToken, "$@\"", "$@\"", default(SyntaxTriviaList))
            : SyntaxFactory.Token(default(SyntaxTriviaList), SyntaxKind.InterpolatedStringStartToken, "$\"", "$\"", default(SyntaxTriviaList));
        var contents = await node.Contents.SelectAsync(async c => await c.AcceptAsync<InterpolatedStringContentSyntax>(TriviaConvertingExpressionVisitor));
        InterpolatedStringExpressionSyntax interpolatedStringExpressionSyntax = SyntaxFactory.InterpolatedStringExpression(startToken, SyntaxFactory.List(contents), SyntaxFactory.Token(SyntaxKind.InterpolatedStringEndToken));
        return interpolatedStringExpressionSyntax;
    }

    public override async Task<CSharpSyntaxNode> VisitInterpolatedStringText(VBasic.Syntax.InterpolatedStringTextSyntax node)
    {
        var useVerbatim = node.Parent.DescendantNodes().OfType<VBasic.Syntax.InterpolatedStringTextSyntax>().Any(c => LiteralConversions.IsWorthBeingAVerbatimString(c.TextToken.Text));
        var textForUser = LiteralConversions.EscapeQuotes(node.TextToken.Text, node.TextToken.ValueText, useVerbatim);
        InterpolatedStringTextSyntax interpolatedStringTextSyntax = SyntaxFactory.InterpolatedStringText(SyntaxFactory.Token(default(SyntaxTriviaList), SyntaxKind.InterpolatedStringTextToken, textForUser, node.TextToken.ValueText, default(SyntaxTriviaList)));
        return interpolatedStringTextSyntax;
    }

    public override async Task<CSharpSyntaxNode> VisitInterpolationAlignmentClause(VBasic.Syntax.InterpolationAlignmentClauseSyntax node)
    {
        return SyntaxFactory.InterpolationAlignmentClause(SyntaxFactory.Token(SyntaxKind.CommaToken), await node.Value.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor));
    }

    public override async Task<CSharpSyntaxNode> VisitInterpolationFormatClause(VBasic.Syntax.InterpolationFormatClauseSyntax node)
    {
        var textForUser = LiteralConversions.EscapeEscapeChar(node.FormatStringToken.ValueText);
        SyntaxToken formatStringToken = SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.InterpolatedStringTextToken, textForUser, node.FormatStringToken.ValueText, SyntaxTriviaList.Empty);
        return SyntaxFactory.InterpolationFormatClause(SyntaxFactory.Token(SyntaxKind.ColonToken), formatStringToken);
    }

    public override async Task<CSharpSyntaxNode> VisitMeExpression(VBasic.Syntax.MeExpressionSyntax node)
    {
        return SyntaxFactory.ThisExpression();
    }

    public override async Task<CSharpSyntaxNode> VisitMyBaseExpression(VBasic.Syntax.MyBaseExpressionSyntax node)
    {
        return SyntaxFactory.BaseExpression();
    }

    public override async Task<CSharpSyntaxNode> VisitParenthesizedExpression(VBasic.Syntax.ParenthesizedExpressionSyntax node)
    {
        var cSharpSyntaxNode = await node.Expression.AcceptAsync<CSharpSyntaxNode>(TriviaConvertingExpressionVisitor);
        // If structural changes are necessary the expression may have been lifted a statement (e.g. Type inferred lambda)
        return cSharpSyntaxNode is ExpressionSyntax expr ? SyntaxFactory.ParenthesizedExpression(expr) : cSharpSyntaxNode;
    }

    public override async Task<CSharpSyntaxNode> VisitMemberAccessExpression(VBasic.Syntax.MemberAccessExpressionSyntax node)
    {
        var nodeSymbol = GetSymbolInfoInDocument<ISymbol>(node.Name);

        if (!node.IsParentKind(VBasic.SyntaxKind.InvocationExpression) &&
            SimpleMethodReplacement.TryGet(nodeSymbol, out var methodReplacement) &&
            methodReplacement.ReplaceIfMatches(nodeSymbol, Array.Empty<ArgumentSyntax>(), node.IsParentKind(VBasic.SyntaxKind.AddressOfExpression)) is {} replacement) {
            return replacement;
        }

        var simpleNameSyntax = await node.Name.AcceptAsync<SimpleNameSyntax>(TriviaConvertingExpressionVisitor);

        var isDefaultProperty = nodeSymbol is IPropertySymbol p && VBasic.VisualBasicExtensions.IsDefault(p);
        ExpressionSyntax left = null;
        if (node.Expression is VBasic.Syntax.MyClassExpressionSyntax && nodeSymbol != null) {
            if (nodeSymbol.IsStatic) {
                var typeInfo = _semanticModel.GetTypeInfo(node.Expression);
                left = CommonConversions.GetTypeSyntax(typeInfo.Type);
            } else {
                left = SyntaxFactory.ThisExpression();
                if (nodeSymbol.IsVirtual && !nodeSymbol.IsAbstract ||
                    nodeSymbol.IsImplicitlyDeclared && nodeSymbol is IFieldSymbol { AssociatedSymbol: IPropertySymbol { IsVirtual: true, IsAbstract: false } }) {
                    simpleNameSyntax =
                        ValidSyntaxFactory.IdentifierName(
                            $"MyClass{ConvertIdentifier(node.Name.Identifier).ValueText}");
                }
            }
        }
        if (left == null && nodeSymbol?.IsStatic == true) {
            var type = nodeSymbol.ContainingType;
            if (type != null) {
                left = CommonConversions.GetTypeSyntax(type);
            }
        }
        if (left == null) {
            left = await node.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
            if (left != null && _semanticModel.GetSymbolInfo(node) is {CandidateReason: CandidateReason.LateBound, CandidateSymbols.Length: 0}
                             && _semanticModel.GetSymbolInfo(node.Expression).Symbol is {Kind: var expressionSymbolKind}
                             && expressionSymbolKind != SymbolKind.ErrorType
                             && _semanticModel.GetOperation(node) is IDynamicMemberReferenceOperation) {
                left = SyntaxFactory.ParenthesizedExpression(SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName("dynamic"), left));
            }
        }
        if (left == null) {
            if (IsSubPartOfConditionalAccess(node)) {
                return isDefaultProperty ? SyntaxFactory.ElementBindingExpression()
                    : await AdjustForImplicitInvocationAsync(node, SyntaxFactory.MemberBindingExpression(simpleNameSyntax));
            } else if (node.IsParentKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.NamedFieldInitializer)) {
                return ValidSyntaxFactory.IdentifierName(_tempNameForAnonymousScope[node.Name.Identifier.Text].Peek().TempName);
            }
            left = _withBlockLhs.Peek();
        }

        if (node.IsKind(VBasic.SyntaxKind.DictionaryAccessExpression)) {
            var args = SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(CommonConversions.Literal(node.Name.Identifier.ValueText)));
            var bracketedArgumentListSyntax = SyntaxFactory.BracketedArgumentList(args);
            return SyntaxFactory.ElementAccessExpression(left, bracketedArgumentListSyntax);
        }

        if (node.Expression.IsKind(VBasic.SyntaxKind.GlobalName)) {
            return SyntaxFactory.AliasQualifiedName((IdentifierNameSyntax)left, simpleNameSyntax);
        }

        if (isDefaultProperty && left != null) {
            return left;
        }

        var memberAccessExpressionSyntax = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, left, simpleNameSyntax);
        return await AdjustForImplicitInvocationAsync(node, memberAccessExpressionSyntax);
    }

    public override async Task<CSharpSyntaxNode> VisitConditionalAccessExpression(VBasic.Syntax.ConditionalAccessExpressionSyntax node)
    {
        var leftExpression = await node.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor) ?? _withBlockLhs.Peek();
        return SyntaxFactory.ConditionalAccessExpression(leftExpression, await node.WhenNotNull.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor));
    }

    public override async Task<CSharpSyntaxNode> VisitNameOfExpression(VBasic.Syntax.NameOfExpressionSyntax node)
    {
        return SyntaxFactory.InvocationExpression(ValidSyntaxFactory.NameOf(), SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(await node.Argument.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor)))));
    }

    public override async Task<CSharpSyntaxNode> VisitAnonymousObjectCreationExpression(VBasic.Syntax.AnonymousObjectCreationExpressionSyntax node)
    {
        var vbInitializers = node.Initializer.Initializers;
        try {
            var initializers = await vbInitializers.AcceptSeparatedListAsync<VBSyntax.FieldInitializerSyntax, AnonymousObjectMemberDeclaratorSyntax>(TriviaConvertingExpressionVisitor);
            return SyntaxFactory.AnonymousObjectCreationExpression(initializers);
        } finally {
            var kvpsToPop = _tempNameForAnonymousScope.Where(t => t.Value.Peek().Scope == node).ToArray();
            foreach (var kvp in kvpsToPop) {
                if (kvp.Value.Count == 1) _tempNameForAnonymousScope.Remove(kvp.Key);
                else kvp.Value.Pop();
            }
        }
    }

    public override async Task<CSharpSyntaxNode> VisitInferredFieldInitializer(VBasic.Syntax.InferredFieldInitializerSyntax node)
    {
        return SyntaxFactory.AnonymousObjectMemberDeclarator(await node.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor));
    }

    public override async Task<CSharpSyntaxNode> VisitObjectCreationExpression(VBasic.Syntax.ObjectCreationExpressionSyntax node)
    {
        var objectCreationExpressionSyntax = SyntaxFactory.ObjectCreationExpression(
            await node.Type.AcceptAsync<TypeSyntax>(TriviaConvertingExpressionVisitor),
            await ConvertArgumentListOrEmptyAsync(node, node.ArgumentList),
            null
        );
        async Task<InitializerExpressionSyntax> ConvertInitializer() => await node.Initializer.AcceptAsync<InitializerExpressionSyntax>(TriviaConvertingExpressionVisitor);
        if (node.Initializer is VBSyntax.ObjectMemberInitializerSyntax objectMemberInitializerSyntax && HasInitializersUsingImpliedLhs(objectMemberInitializerSyntax)) {
            var idToUse = _typeContext.PerScopeState.Hoist(new AdditionalDeclaration("init", objectCreationExpressionSyntax, CommonConversions.GetTypeSyntax(_semanticModel.GetTypeInfo(node).Type))).IdentifierName;
            _withBlockLhs.Push(idToUse);
            try {
                var initializer = await ConvertInitializer();
                var originalExpressions = initializer.Expressions.Select(x => x is AssignmentExpressionSyntax e ? e.ReplaceNode(e.Left, SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, idToUse, (SimpleNameSyntax) e.Left)) : null).ToArray<ExpressionSyntax>();
                var expressions = SyntaxFactory.SeparatedList(originalExpressions.Append(idToUse).Select(SyntaxFactory.Argument));
                var tuple = SyntaxFactory.TupleExpression(expressions);
                return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, tuple, idToUse);
            } finally {
                _withBlockLhs.Pop();
            }
        }
        return objectCreationExpressionSyntax.WithInitializer(await ConvertInitializer());
    }

    private static bool HasInitializersUsingImpliedLhs(VBSyntax.ObjectMemberInitializerSyntax objectMemberInitializerSyntax)
    {
        return objectMemberInitializerSyntax.Initializers.SelectMany(i => i.ChildNodes().Skip(1), (_, c) => c.DescendantNodesAndSelf()).SelectMany(d => d).OfType<VBSyntax.MemberAccessExpressionSyntax>().Any(x => x.Expression is null);
    }

    public override async Task<CSharpSyntaxNode> VisitArrayCreationExpression(VBasic.Syntax.ArrayCreationExpressionSyntax node)
    {
        var bounds = await CommonConversions.ConvertArrayRankSpecifierSyntaxesAsync(node.RankSpecifiers, node.ArrayBounds);
        var allowInitializer = node.ArrayBounds?.Arguments.Any() != true ||
                               node.Initializer.Initializers.Any() && node.ArrayBounds.Arguments.All(b => b.IsOmitted || _semanticModel.GetConstantValue(b.GetExpression()).HasValue);
        var initializerToConvert = allowInitializer ? node.Initializer : null;
        return SyntaxFactory.ArrayCreationExpression(
            SyntaxFactory.ArrayType(await node.Type.AcceptAsync<TypeSyntax>(TriviaConvertingExpressionVisitor), bounds),
            await initializerToConvert.AcceptAsync<InitializerExpressionSyntax>(TriviaConvertingExpressionVisitor)
        );
    }

    public override async Task<CSharpSyntaxNode> VisitCollectionInitializer(VBasic.Syntax.CollectionInitializerSyntax node)
    {
        var isExplicitCollectionInitializer = node.Parent is VBasic.Syntax.ObjectCollectionInitializerSyntax
                                              || node.Parent is VBasic.Syntax.CollectionInitializerSyntax
                                              || node.Parent is VBasic.Syntax.ArrayCreationExpressionSyntax;
        var initializerKind = node.IsParentKind(VBasic.SyntaxKind.ObjectCollectionInitializer) || node.IsParentKind(VBasic.SyntaxKind.ObjectCreationExpression) ?
            SyntaxKind.CollectionInitializerExpression :
            node.IsParentKind(VBasic.SyntaxKind.CollectionInitializer) && IsComplexInitializer(node) ? SyntaxKind.ComplexElementInitializerExpression :
                SyntaxKind.ArrayInitializerExpression;
        var initializers = (await node.Initializers.SelectAsync(async i => {
            var convertedInitializer = await i.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
            return CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(i, convertedInitializer, false);
        }));
        var initializer = SyntaxFactory.InitializerExpression(initializerKind, SyntaxFactory.SeparatedList(initializers));
        if (isExplicitCollectionInitializer) return initializer;

        var convertedType = _semanticModel.GetTypeInfo(node).ConvertedType;
        var dimensions = convertedType is IArrayTypeSymbol ats ? ats.Rank : 1;
        if (!(convertedType.GetEnumerableElementTypeOrDefault() is {} elementType)) return SyntaxFactory.ImplicitArrayCreationExpression(initializer);

        if (!initializers.Any() && dimensions == 1) {
            var arrayTypeArgs = SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(CommonConversions.GetTypeSyntax(elementType)));
            var arrayEmpty = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                ValidSyntaxFactory.IdentifierName(nameof(Array)), SyntaxFactory.GenericName(nameof(Array.Empty)).WithTypeArgumentList(arrayTypeArgs));
            return SyntaxFactory.InvocationExpression(arrayEmpty);
        }

        bool hasExpressionToInferTypeFrom = node.Initializers.SelectMany(n => n.DescendantNodesAndSelf()).Any(n => n is not VBasic.Syntax.CollectionInitializerSyntax);
        if (hasExpressionToInferTypeFrom) {
            var commas = Enumerable.Repeat(SyntaxFactory.Token(SyntaxKind.CommaToken), dimensions - 1);
            return SyntaxFactory.ImplicitArrayCreationExpression(SyntaxFactory.TokenList(commas), initializer);
        }

        var arrayType = (ArrayTypeSyntax)CommonConversions.CsSyntaxGenerator.ArrayTypeExpression(CommonConversions.GetTypeSyntax(elementType));
        var sizes = Enumerable.Repeat<ExpressionSyntax>(SyntaxFactory.OmittedArraySizeExpression(), dimensions);
        var arrayRankSpecifierSyntax = SyntaxFactory.SingletonList(SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SeparatedList(sizes)));
        arrayType = arrayType.WithRankSpecifiers(arrayRankSpecifierSyntax);
        return SyntaxFactory.ArrayCreationExpression(arrayType, initializer);
    }

    private bool IsComplexInitializer(VBSyntax.CollectionInitializerSyntax node)
    {
        return _semanticModel.GetOperation(node.Parent.Parent) is IObjectOrCollectionInitializerOperation initializer &&
               initializer.Initializers.OfType<IInvocationOperation>().Any();
    }

    public override async Task<CSharpSyntaxNode> VisitQueryExpression(VBasic.Syntax.QueryExpressionSyntax node)
    {
        return await _queryConverter.ConvertClausesAsync(node.Clauses);
    }

    public override async Task<CSharpSyntaxNode> VisitTypeOfExpression(VBasic.Syntax.TypeOfExpressionSyntax node)
    {
        var expr = SyntaxFactory.BinaryExpression(
            SyntaxKind.IsExpression,
            await node.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor),
            await node.Type.AcceptAsync<TypeSyntax>(TriviaConvertingExpressionVisitor)
        );
        return node.IsKind(VBasic.SyntaxKind.TypeOfIsNotExpression) ? expr.InvertCondition() : expr;
    }

    public override async Task<CSharpSyntaxNode> VisitUnaryExpression(VBasic.Syntax.UnaryExpressionSyntax node)
    {
        var expr = await node.Operand.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
        if (node.IsKind(VBasic.SyntaxKind.AddressOfExpression)) {
            return ConvertAddressOf(node, expr);
        }
        var kind = VBasic.VisualBasicExtensions.Kind(node).ConvertToken();
        SyntaxKind csTokenKind = CSharpUtil.GetExpressionOperatorTokenKind(kind);

        if (kind == SyntaxKind.LogicalNotExpression && _semanticModel.GetTypeInfo(node.Operand).ConvertedType is { } t) {
            if (t.IsNumericType() || t.IsEnumType()) {
                csTokenKind = SyntaxKind.TildeToken;
            } else if (await NegateAndSimplifyOrNullAsync(node, expr, t) is { } simpleNegation) {
                return simpleNegation;
            }
        }

        return SyntaxFactory.PrefixUnaryExpression(
            kind,
            SyntaxFactory.Token(csTokenKind),
            expr.AddParens()
        );
    }

    private async Task<ExpressionSyntax> NegateAndSimplifyOrNullAsync(VBSyntax.UnaryExpressionSyntax node, ExpressionSyntax expr, ITypeSymbol operandConvertedType)
    {
        if (await _operatorConverter.ConvertReferenceOrNothingComparisonOrNullAsync(node.Operand.SkipIntoParens(), TriviaConvertingExpressionVisitor.IsWithinQuery, true) is { } nothingComparison) {
            return nothingComparison;
        }
        if (operandConvertedType.GetNullableUnderlyingType()?.SpecialType == SpecialType.System_Boolean && node.AlwaysHasBooleanTypeInCSharp()) {
            return SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, expr, LiteralConversions.GetLiteralExpression(false));
        }

        if (expr is BinaryExpressionSyntax eq && eq.OperatorToken.IsKind(SyntaxKind.EqualsEqualsToken, SyntaxKind.ExclamationEqualsToken)){
            return eq.WithOperatorToken(SyntaxFactory.Token(eq.OperatorToken.IsKind(SyntaxKind.ExclamationEqualsToken) ? SyntaxKind.EqualsEqualsToken : SyntaxKind.ExclamationEqualsToken));
        }

        return null;
    }

    private CSharpSyntaxNode ConvertAddressOf(VBSyntax.UnaryExpressionSyntax node, ExpressionSyntax expr)
    {
        var typeInfo = _semanticModel.GetTypeInfo(node);
        if (_semanticModel.GetSymbolInfo(node.Operand).Symbol is IMethodSymbol ms && typeInfo.Type is INamedTypeSymbol nt && !ms.CompatibleSignatureToDelegate(nt)) {
            int count = nt.DelegateInvokeMethod.Parameters.Length;
            return CommonConversions.ThrowawayParameters(expr, count);
        }
        return expr;
    }

    public override async Task<CSharpSyntaxNode> VisitBinaryExpression(VBasic.Syntax.BinaryExpressionSyntax entryNode)
    {
        ExpressionSyntax csLhs = null;
        int levelsToConvert = 0;
        VBSyntax.BinaryExpressionSyntax currentNode = entryNode;

        for (var nextNode = entryNode; nextNode != null; currentNode = nextNode, nextNode = currentNode.Left as VBSyntax.BinaryExpressionSyntax, levelsToConvert++) {
            if (await RewriteBinaryOperatorOrNullAsync(nextNode) is { } operatorNode) {
                csLhs = operatorNode;
                break;
            }
        }

        for (; levelsToConvert > 0; currentNode = currentNode!.Parent as VBSyntax.BinaryExpressionSyntax, levelsToConvert--) {
            csLhs = (ExpressionSyntax)await ConvertBinaryExpressionAsync(currentNode, csLhs);
        }

        return csLhs;
    }

    private async Task<CSharpSyntaxNode> ConvertBinaryExpressionAsync(VBasic.Syntax.BinaryExpressionSyntax node, ExpressionSyntax lhs = null, ExpressionSyntax rhs = null)
    {
        lhs ??= await node.Left.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
        rhs ??= await node.Right.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);

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
                omitConversion = true;
                break;
            case VisualBasicEqualityComparison.RequiredType.Object:
                return _visualBasicEqualityComparison.GetFullExpressionForVbObjectComparison(lhs, rhs, ComparisonKind.Equals, node.IsKind(VBasic.SyntaxKind.NotEqualsExpression));
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
        var op = SyntaxFactory.Token(CSharpUtil.GetExpressionOperatorTokenKind(kind));

        var csBinExp = SyntaxFactory.BinaryExpression(kind, lhs, op, rhs);
        var exp = _visualBasicNullableTypesConverter.WithBinaryExpressionLogicForNullableTypes(node, lhsTypeInfo, rhsTypeInfo, csBinExp, lhs, rhs);
        return node.Parent.IsKind(VBasic.SyntaxKind.SimpleArgument) ? exp : exp.AddParens();
    }

    private async Task<ExpressionSyntax> RewriteBinaryOperatorOrNullAsync(VBSyntax.BinaryExpressionSyntax node) =>
        await _operatorConverter.ConvertRewrittenBinaryOperatorOrNullAsync(node, TriviaConvertingExpressionVisitor.IsWithinQuery);

    public override async Task<CSharpSyntaxNode> VisitInvocationExpression(
        VBasic.Syntax.InvocationExpressionSyntax node)
    {
        var invocationSymbol = _semanticModel.GetSymbolInfo(node).ExtractBestMatch<ISymbol>();
        var methodInvocationSymbol = invocationSymbol as IMethodSymbol;
        var withinLocalFunction = methodInvocationSymbol != null && RequiresLocalFunction(node, methodInvocationSymbol);
        if (withinLocalFunction) {
            _typeContext.PerScopeState.PushScope();
        }
        try {

            if (node.Expression is null) {
                var convertArgumentListOrEmptyAsync = await ConvertArgumentsAsync(node.ArgumentList);
                return SyntaxFactory.ElementBindingExpression(SyntaxFactory.BracketedArgumentList(SyntaxFactory.SeparatedList(convertArgumentListOrEmptyAsync)));
            }

            var convertedInvocation = await ConvertOrReplaceInvocationAsync(node, invocationSymbol);
            if (withinLocalFunction) {
                return await HoistAndCallLocalFunctionAsync(node, methodInvocationSymbol, (ExpressionSyntax)convertedInvocation);
            }
            return convertedInvocation;
        } finally {
            if (withinLocalFunction) {
                _typeContext.PerScopeState.PopExpressionScope();
            }
        }
    }

    private async Task<CSharpSyntaxNode> ConvertOrReplaceInvocationAsync(VBSyntax.InvocationExpressionSyntax node, ISymbol invocationSymbol)
    {
        var expressionSymbol = _semanticModel.GetSymbolInfo(node.Expression).ExtractBestMatch<ISymbol>();
        if ((await SubstituteVisualBasicMethodOrNullAsync(node, expressionSymbol) ??
             await WithRemovedRedundantConversionOrNullAsync(node, expressionSymbol)) is { } csEquivalent) {
            return csEquivalent;
        }

        if (invocationSymbol?.Name is "op_Implicit" or "op_Explicit") {
            var vbExpr = node.ArgumentList.Arguments.Single().GetExpression();
            var csExpr = await vbExpr.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
            return CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(vbExpr, csExpr, true, true, false, forceTargetType: invocationSymbol.GetReturnType());
        }

        return await ConvertInvocationAsync(node, invocationSymbol, expressionSymbol);
    }

    private async Task<ExpressionSyntax> ConvertInvocationAsync(VBSyntax.InvocationExpressionSyntax node, ISymbol invocationSymbol, ISymbol expressionSymbol)
    {
        var expressionType = _semanticModel.GetTypeInfo(node.Expression).Type;
        var expressionReturnType = expressionSymbol?.GetReturnType() ?? expressionType;
        var operation = _semanticModel.GetOperation(node);

        var expr = await node.Expression.AcceptAsync<CSharpSyntaxNode>(TriviaConvertingExpressionVisitor);
        if (await TryConvertParameterizedPropertyAsync(operation, node, expr, node.ArgumentList) is { } invocation)
        {
            return invocation;
        }

        (var convertedExpression, bool shouldBeElementAccess) = await ConvertInvocationSubExpressionAsync(node, operation, expressionSymbol, expressionReturnType, expr);
        if (shouldBeElementAccess)
        {
            return await CreateElementAccessAsync(node, convertedExpression);
        }

        if (expressionSymbol != null && expressionSymbol.IsKind(SymbolKind.Property) &&
            invocationSymbol != null && invocationSymbol.GetParameters().Length == 0 && node.ArgumentList.Arguments.Count == 0)
        {
            return convertedExpression;
        }

        var convertedArgumentList = await ConvertArgumentListOrEmptyAsync(node, node.ArgumentList);

        if (IsElementAtOrDefaultInvocation(invocationSymbol, expressionSymbol))
        {
            convertedExpression = GetElementAtOrDefaultExpression(expressionType, convertedExpression);
        }

        if (invocationSymbol.IsReducedExtension() && invocationSymbol is IMethodSymbol {ReducedFrom: {Parameters: var parameters}} &&
            !parameters.FirstOrDefault().ValidCSharpExtensionMethodParameter() &&
            node.Expression is VBSyntax.MemberAccessExpressionSyntax maes)
        {
            var thisArgExpression = await maes.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
            var thisArg = SyntaxFactory.Argument(thisArgExpression).WithRefKindKeyword(GetRefToken(RefKind.Ref));
            convertedArgumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(convertedArgumentList.Arguments.Prepend(thisArg)));
            var containingType = (ExpressionSyntax) CommonConversions.CsSyntaxGenerator.TypeExpression(invocationSymbol.ContainingType);
            convertedExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, containingType,
                ValidSyntaxFactory.IdentifierName((invocationSymbol.Name)));
        }

        if (invocationSymbol is IMethodSymbol m && convertedExpression is LambdaExpressionSyntax) {
            convertedExpression = SyntaxFactory.ObjectCreationExpression(CommonConversions.GetFuncTypeSyntax(expressionType, m), ExpressionSyntaxExtensions.CreateArgList(convertedExpression), null);
        }
        return SyntaxFactory.InvocationExpression(convertedExpression, convertedArgumentList);
    }

    private async Task<(ExpressionSyntax, bool isElementAccess)> ConvertInvocationSubExpressionAsync(VBSyntax.InvocationExpressionSyntax node,
        IOperation operation, ISymbol expressionSymbol, ITypeSymbol expressionReturnType, CSharpSyntaxNode expr)
    {
        var isElementAccess = operation.IsPropertyElementAccess()
                              || operation.IsArrayElementAccess()
                              || ProbablyNotAMethodCall(node, expressionSymbol, expressionReturnType);

        var expressionSyntax = (ExpressionSyntax)expr;

        return (expressionSyntax, isElementAccess);
    }

    private async Task<ExpressionSyntax> CreateElementAccessAsync(VBSyntax.InvocationExpressionSyntax node, ExpressionSyntax expression)
    {
        var args =
            await node.ArgumentList.Arguments.AcceptSeparatedListAsync<VBSyntax.ArgumentSyntax, ArgumentSyntax>(TriviaConvertingExpressionVisitor);
        var bracketedArgumentListSyntax = SyntaxFactory.BracketedArgumentList(args);
        if (expression is ElementBindingExpressionSyntax binding &&
            !binding.ArgumentList.Arguments.Any()) {
            return binding.WithArgumentList(bracketedArgumentListSyntax);
        }

        return SyntaxFactory.ElementAccessExpression(expression, bracketedArgumentListSyntax);
    }

    private static bool IsElementAtOrDefaultInvocation(ISymbol invocationSymbol, ISymbol expressionSymbol)
    {
        return (expressionSymbol != null
                && (invocationSymbol?.Name == nameof(Enumerable.ElementAtOrDefault)
                    && !expressionSymbol.Equals(invocationSymbol, SymbolEqualityComparer.IncludeNullability)));
    }

    private ExpressionSyntax GetElementAtOrDefaultExpression(ISymbol expressionType,
        ExpressionSyntax expression)
    {
        _extraUsingDirectives.Add(nameof(System) + "." + nameof(System.Linq));

        if (expressionType.Name == nameof(DataTable))
        {
            _extraUsingDirectives.Add(nameof(System) + "." + nameof(System.Data));

            expression = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression, expression,
                ValidSyntaxFactory.IdentifierName(nameof(DataTableExtensions.AsEnumerable))));
        }

        var newExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
            expression, ValidSyntaxFactory.IdentifierName(nameof(Enumerable.ElementAtOrDefault)));

        return newExpression;
    }

    private async Task<InvocationExpressionSyntax> TryConvertParameterizedPropertyAsync(IOperation operation,
        SyntaxNode node, CSharpSyntaxNode identifier,
        VBSyntax.ArgumentListSyntax optionalArgumentList = null)
    {
        var (overrideIdentifier, extraArg) =
            await CommonConversions.GetParameterizedPropertyAccessMethodAsync(operation);
        if (overrideIdentifier != null)
        {
            var expr = identifier;
            var idToken = expr.DescendantTokens().Last(t => t.IsKind(SyntaxKind.IdentifierToken));
            expr = ReplaceRightmostIdentifierText(expr, idToken, overrideIdentifier);

            var args = await ConvertArgumentListOrEmptyAsync(node, optionalArgumentList);
            if (extraArg != null) {
                var extraArgSyntax = SyntaxFactory.Argument(extraArg);
                var propertySymbol = ((IPropertyReferenceOperation)operation).Property;
                var forceNamedExtraArg = args.Arguments.Count != propertySymbol.GetParameters().Length ||
                                         args.Arguments.Any(t => t.NameColon != null);

                if (forceNamedExtraArg) {
                    extraArgSyntax = extraArgSyntax.WithNameColon(SyntaxFactory.NameColon("value"));
                }

                args = args.WithArguments(args.Arguments.Add(extraArgSyntax));
            }

            return SyntaxFactory.InvocationExpression((ExpressionSyntax)expr, args);
        }

        return null;
    }

    private async Task<InvocationExpressionSyntax> HoistAndCallLocalFunctionAsync(VBSyntax.InvocationExpressionSyntax invocation, IMethodSymbol invocationSymbol, ExpressionSyntax csExpression)
    {
        const string retVariableName = "ret";
        var localFuncName = $"local{invocationSymbol.Name}";

        var callAndStoreResult = CommonConversions.CreateLocalVariableDeclarationAndAssignment(retVariableName, csExpression);

        var statements = await _typeContext.PerScopeState.CreateLocalsAsync(invocation, new[] { callAndStoreResult }, _generatedNames, _semanticModel);

        var block = SyntaxFactory.Block(
            statements.Concat(SyntaxFactory.ReturnStatement(ValidSyntaxFactory.IdentifierName(retVariableName)).Yield())
        );
        var returnType = CommonConversions.GetTypeSyntax(invocationSymbol.ReturnType);

        var refParametersOfParent = GetRefParameters(invocation.ArgumentList);
        var (args, @params) = CreateArgumentsAndParametersLists(refParametersOfParent);

        var localFunc = _typeContext.PerScopeState.Hoist(new HoistedFunction(localFuncName, returnType, block, SyntaxFactory.ParameterList(@params)));
        return SyntaxFactory.InvocationExpression(localFunc.TempIdentifier, SyntaxFactory.ArgumentList(args));
    }

    private bool RequiresLocalFunction(VBSyntax.InvocationExpressionSyntax invocation, IMethodSymbol invocationSymbol)
    {
        if (invocation.ArgumentList == null) return false;
        var definitelyExecutedAfterPrevious = DefinitelyExecutedAfterPreviousStatement(invocation);
        var nextStatementDefinitelyExecuted = NextStatementDefinitelyExecutedAfter(invocation);
        if (definitelyExecutedAfterPrevious && nextStatementDefinitelyExecuted) return false;
        var possibleInline = definitelyExecutedAfterPrevious ? RefConversion.PreAssigment : RefConversion.Inline;
        return invocation.ArgumentList.Arguments.Any(a => RequiresLocalFunction(possibleInline, invocation, invocationSymbol, a));

        bool RequiresLocalFunction(RefConversion possibleInline, VBSyntax.InvocationExpressionSyntax inv, IMethodSymbol sym, VBSyntax.ArgumentSyntax arg)
        {
            var refConversion = GetRefConversionType(arg, inv.ArgumentList, sym.Parameters, out string _, out _);
            if (RefConversion.Inline == refConversion || possibleInline == refConversion) return false;
            if (!(arg is VBSyntax.SimpleArgumentSyntax sas)) return false;
            var argExpression = sas.Expression.SkipIntoParens();
            if (argExpression is VBSyntax.InstanceExpressionSyntax) return false;
            return !_semanticModel.GetConstantValue(argExpression).HasValue;
        }
    }

    private bool DefinitelyExecutedAfterPreviousStatement(VBSyntax.InvocationExpressionSyntax invocation)
    {
        SyntaxNode parent = invocation;
        while (true) {
            parent = parent.Parent;
            switch (parent)
            {
                case VBSyntax.ParenthesizedExpressionSyntax _:
                    continue;
                case VBSyntax.BinaryExpressionSyntax binaryExpression:
                    if (binaryExpression.Left == invocation) continue;
                    else return false;
                case VBSyntax.ArgumentSyntax argumentSyntax:
                    if (argumentSyntax.Parent.Parent is VBSyntax.InvocationExpressionSyntax parentInvocation && parentInvocation.ArgumentList.Arguments.First() == argumentSyntax && FirstArgDefinitelyEvaluated(parentInvocation)) continue;
                    else return false;
                case VBSyntax.ElseIfStatementSyntax _:
                case VBSyntax.ExpressionSyntax _:
                    return false;
                case VBSyntax.StatementSyntax _:
                    return true;
            }
        }
    }

    private bool FirstArgDefinitelyEvaluated(VBSyntax.InvocationExpressionSyntax parentInvocation) =>
        parentInvocation.Expression.SkipIntoParens() switch {
            VBSyntax.IdentifierNameSyntax _ => true,
            VBSyntax.MemberAccessExpressionSyntax maes => maes.Expression is {} exp && !MayThrow(exp),
            _ => true
        };

    private bool MayThrow(VBSyntax.ExpressionSyntax expression)
    {
        expression = expression.SkipIntoParens();
        if (expression is VBSyntax.InstanceExpressionSyntax) return false;
        var symbol = _semanticModel.GetSymbolInfo(expression).Symbol;
        return !symbol.IsKind(SymbolKind.Local) && !symbol.IsKind(SymbolKind.Field);
    }

    private static bool NextStatementDefinitelyExecutedAfter(VBSyntax.InvocationExpressionSyntax invocation)
    {
        SyntaxNode parent = invocation;
        while (true) {
            parent = parent.Parent;
            switch (parent)
            {
                case VBSyntax.ParenthesizedExpressionSyntax _:
                    continue;
                case VBSyntax.BinaryExpressionSyntax binaryExpression:
                    if (binaryExpression.Right == invocation) continue;
                    else return false;
                case VBSyntax.IfStatementSyntax _:
                case VBSyntax.ElseIfStatementSyntax _:
                case VBSyntax.SingleLineIfStatementSyntax _:
                    return false;
                case VBSyntax.ExpressionSyntax _:
                case VBSyntax.StatementSyntax _:
                    return true;
            }
        }
    }
    public override async Task<CSharpSyntaxNode> VisitBinaryConditionalExpression(VBasic.Syntax.BinaryConditionalExpressionSyntax node)
    {
        var leftSide = await node.FirstExpression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
        var rightSide = await node.SecondExpression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
        var expr = SyntaxFactory.BinaryExpression(SyntaxKind.CoalesceExpression,
            node.FirstExpression.ParenthesizeIfPrecedenceCouldChange(leftSide),
            node.SecondExpression.ParenthesizeIfPrecedenceCouldChange(rightSide));

        if (node.Parent.IsKind(VBasic.SyntaxKind.Interpolation) || node.PrecedenceCouldChange())
            return SyntaxFactory.ParenthesizedExpression(expr);

        return expr;
    }

    public override async Task<CSharpSyntaxNode> VisitTernaryConditionalExpression(VBasic.Syntax.TernaryConditionalExpressionSyntax node)
    {
        var condition = await node.Condition.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
        condition = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Condition, condition, forceTargetType: CommonConversions.KnownTypes.Boolean);

        var whenTrue = await node.WhenTrue.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
        whenTrue = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.WhenTrue, whenTrue);

        var whenFalse = await node.WhenFalse.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
        whenFalse = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.WhenFalse, whenFalse);

        var expr = SyntaxFactory.ConditionalExpression(condition, whenTrue, whenFalse);

        if (node.Parent.IsKind(VBasic.SyntaxKind.Interpolation) || node.PrecedenceCouldChange())
            return SyntaxFactory.ParenthesizedExpression(expr);

        return expr;
    }
    private async Task<CSharpSyntaxNode> WithRemovedRedundantConversionOrNullAsync(VBSyntax.InvocationExpressionSyntax conversionNode, ISymbol invocationSymbol)
    {
        if (invocationSymbol?.ContainingNamespace.MetadataName != nameof(Microsoft.VisualBasic) ||
            invocationSymbol.ContainingType.Name != nameof(Conversions) ||
            !invocationSymbol.Name.StartsWith("To", StringComparison.InvariantCulture) ||
            conversionNode.ArgumentList.Arguments.Count != 1) {
            return null;
        }

        var conversionArg = conversionNode.ArgumentList.Arguments.First().GetExpression();
        VBSyntax.ExpressionSyntax coercedConversionNode = conversionNode;
        return await WithRemovedRedundantConversionOrNullAsync(coercedConversionNode, conversionArg);
    }

    private async Task<CSharpSyntaxNode> WithRemovedRedundantConversionOrNullAsync(VBSyntax.ExpressionSyntax conversionNode, VBSyntax.ExpressionSyntax conversionArg)
    {
        var csharpArg = await conversionArg.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
        var typeInfo = _semanticModel.GetTypeInfo(conversionNode);

        var writtenByUser = !conversionNode.HasAnnotation(Simplifier.Annotation);
        var forceTargetType = typeInfo.ConvertedType;
        return writtenByUser ? null : CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(conversionArg, csharpArg,
            forceTargetType: forceTargetType, defaultToCast: true);
    }
    private async Task<CSharpSyntaxNode> SubstituteVisualBasicMethodOrNullAsync(VBSyntax.InvocationExpressionSyntax node, ISymbol symbol)
    {
        ExpressionSyntax cSharpSyntaxNode = null;
        if (IsVisualBasicChrMethod(symbol)) {
            var vbArg = node.ArgumentList.Arguments.Single().GetExpression();
            var constValue = _semanticModel.GetConstantValue(vbArg);
            if (IsCultureInvariant(constValue)) {
                var csArg = await vbArg.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
                cSharpSyntaxNode = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node, csArg, true, true, true, forceTargetType: _semanticModel.GetTypeInfo(node).Type);
            }
        }

        if (SimpleMethodReplacement.TryGet(symbol, out var methodReplacement) &&
            methodReplacement.ReplaceIfMatches(symbol, await ConvertArgumentsAsync(node.ArgumentList), false) is {} csExpression) {
            cSharpSyntaxNode = csExpression;
        }

        return cSharpSyntaxNode;
    }
    private static bool IsVisualBasicChrMethod(ISymbol symbol) =>
        symbol is not null
        && symbol.ContainingNamespace.MetadataName == nameof(Microsoft.VisualBasic)
        && (symbol.Name == "ChrW" || symbol.Name == "Chr");
    private static bool IsCultureInvariant(Optional<object> constValue) =>
       constValue.HasValue && Convert.ToUInt64(constValue.Value, CultureInfo.InvariantCulture) <= 127;

    private bool ProbablyNotAMethodCall(VBasic.Syntax.InvocationExpressionSyntax node, ISymbol symbol, ITypeSymbol symbolReturnType)
    {
        return !node.IsParentKind(VBasic.SyntaxKind.CallStatement) && !(symbol is IMethodSymbol) &&
               symbolReturnType.IsErrorType() && node.Expression is VBasic.Syntax.IdentifierNameSyntax &&
               node.ArgumentList?.Arguments.OnlyOrDefault()?.GetExpression() is {} arg &&
               _semanticModel.GetTypeInfo(arg).Type.IsNumericType();
    }

    private async Task<CSharpSyntaxNode> ConvertCastExpressionAsync(VBSyntax.CastExpressionSyntax node,
        ExpressionSyntax convertMethodOrNull = null, VBSyntax.TypeSyntax castToOrNull = null)
    {
        var simplifiedOrNull = await WithRemovedRedundantConversionOrNullAsync(node, node.Expression);
        if (simplifiedOrNull != null) return simplifiedOrNull;
        var expressionSyntax = await node.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
        if (_semanticModel.GetOperation(node) is not IConversionOperation { Conversion.IsIdentity: true }) {
            if (convertMethodOrNull != null) {
                expressionSyntax = Invoke(convertMethodOrNull, expressionSyntax);
            }

            if (castToOrNull != null) {
                expressionSyntax = await CastAsync(expressionSyntax, castToOrNull);
                expressionSyntax = node.ParenthesizeIfPrecedenceCouldChange(expressionSyntax);
            }
        }
        return expressionSyntax;
    }

    private async Task<CastExpressionSyntax> CastAsync(ExpressionSyntax expressionSyntax, VBSyntax.TypeSyntax typeSyntax)
    {
        return ValidSyntaxFactory.CastExpression(await typeSyntax.AcceptAsync<TypeSyntax>(TriviaConvertingExpressionVisitor), expressionSyntax);
    }

    private static InvocationExpressionSyntax Invoke(ExpressionSyntax toInvoke, ExpressionSyntax argExpression)
    {
        return
            SyntaxFactory.InvocationExpression(toInvoke,
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(argExpression)))
            );
    }
    private ExpressionSyntax GetConvertMethodForKeywordOrNull(SyntaxNode type)
    {
        var targetType = _semanticModel.GetTypeInfo(type).Type;
        return GetConvertMethodForKeywordOrNull(targetType);
    }

    private ExpressionSyntax GetConvertMethodForKeywordOrNull(ITypeSymbol targetType)
    {
        _extraUsingDirectives.Add(ConvertType.Namespace);
        return targetType != null &&
               _convertMethodsLookupByReturnType.Value.TryGetValue(targetType, out var convertMethodName)
            ? SyntaxFactory.ParseExpression(convertMethodName)
            : null;
    }
    private static bool IsSubPartOfConditionalAccess(VBasic.Syntax.MemberAccessExpressionSyntax node)
    {
        var firstPossiblyConditionalAncestor = node.Parent;
        while (firstPossiblyConditionalAncestor != null &&
               firstPossiblyConditionalAncestor.IsKind(VBasic.SyntaxKind.InvocationExpression,
                   VBasic.SyntaxKind.SimpleMemberAccessExpression)) {
            firstPossiblyConditionalAncestor = firstPossiblyConditionalAncestor.Parent;
        }

        return firstPossiblyConditionalAncestor?.IsKind(VBasic.SyntaxKind.ConditionalAccessExpression) == true;
    }
    private CSharpSyntaxNode AddEmptyArgumentListIfImplicit(SyntaxNode node, ExpressionSyntax id)
    {
        if (_semanticModel.SyntaxTree != node.SyntaxTree) return id;
        return _semanticModel.GetOperation(node) switch {
            IInvocationOperation invocation => SyntaxFactory.InvocationExpression(id, CreateArgList(invocation.TargetMethod)),
            IPropertyReferenceOperation propReference when propReference.Property.Parameters.Any() => SyntaxFactory.InvocationExpression(id, CreateArgList(propReference.Property)),
            _ => id
        };
    }
}
