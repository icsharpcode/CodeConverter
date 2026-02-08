using ICSharpCode.CodeConverter.Util.FromRoslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions;
using Microsoft.VisualBasic.CompilerServices;

namespace ICSharpCode.CodeConverter.CSharp;

/// <summary>
/// These could be nested within something like a field declaration, an arrow bodied member, or a statement within a method body
/// To understand the difference between how expressions are expressed, compare:
/// http://source.roslyn.codeplex.com/#Microsoft.CodeAnalysis.CSharp/Binder/Binder_Expressions.cs,365
/// http://source.roslyn.codeplex.com/#Microsoft.CodeAnalysis.VisualBasic/Binding/Binder_Expressions.vb,43
/// </summary>
internal partial class ExpressionNodeVisitor : VBasic.VisualBasicSyntaxVisitor<Task<CSharpSyntaxNode>>
{
    public CommentConvertingVisitorWrapper TriviaConvertingExpressionVisitor => CommonConversions.TriviaConvertingExpressionVisitor;
    private readonly SemanticModel _semanticModel;
    private readonly HashSet<string> _extraUsingDirectives;
    private readonly IOperatorConverter _operatorConverter;
    private readonly Stack<ExpressionSyntax> _withBlockLhs = new();
    private readonly ITypeContext _typeContext;
    private readonly QueryConverter _queryConverter;
    private readonly LambdaConverter _lambdaConverter;
    private readonly Dictionary<string, Stack<(SyntaxNode Scope, string TempName)>> _tempNameForAnonymousScope = new();
    private readonly HashSet<string> _generatedNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly XmlExpressionConverter _xmlExpressionConverter;
    private readonly NameExpressionNodeVisitor _nameExpressionNodeVisitor;
    private readonly ArgumentConverter _argumentConverter;
    private readonly BinaryExpressionConverter _binaryExpressionConverter;
    private readonly InitializerConverter _initializerConverter;

    public ExpressionNodeVisitor(SemanticModel semanticModel,
        VisualBasicEqualityComparison visualBasicEqualityComparison, ITypeContext typeContext, CommonConversions commonConversions,
        HashSet<string> extraUsingDirectives, XmlImportContext xmlImportContext, VisualBasicNullableExpressionsConverter visualBasicNullableTypesConverter)
    {
        _semanticModel = semanticModel;
        CommonConversions = commonConversions;
        commonConversions.TriviaConvertingExpressionVisitor = new CommentConvertingVisitorWrapper(this, _semanticModel.SyntaxTree);
        _initializerConverter = new InitializerConverter(semanticModel, commonConversions, _generatedNames, _tempNameForAnonymousScope);
        _lambdaConverter = new LambdaConverter(commonConversions, semanticModel, _withBlockLhs, extraUsingDirectives, typeContext);
        _queryConverter = new QueryConverter(commonConversions, _semanticModel, TriviaConvertingExpressionVisitor);
        _typeContext = typeContext;
        _extraUsingDirectives = extraUsingDirectives;
        _argumentConverter = new ArgumentConverter(visualBasicEqualityComparison, typeContext, semanticModel, commonConversions);
        _xmlExpressionConverter = new XmlExpressionConverter(xmlImportContext, extraUsingDirectives, TriviaConvertingExpressionVisitor);
        _nameExpressionNodeVisitor = new NameExpressionNodeVisitor(semanticModel, _generatedNames, typeContext, extraUsingDirectives, _tempNameForAnonymousScope, _withBlockLhs, commonConversions, _argumentConverter, TriviaConvertingExpressionVisitor);
        _operatorConverter = VbOperatorConversion.Create(TriviaConvertingExpressionVisitor, semanticModel, visualBasicEqualityComparison, commonConversions.TypeConversionAnalyzer);
        _binaryExpressionConverter = new BinaryExpressionConverter(semanticModel, _operatorConverter, visualBasicEqualityComparison, visualBasicNullableTypesConverter, commonConversions);
    }
    public CommonConversions CommonConversions { get; }

    public override async Task<CSharpSyntaxNode> DefaultVisit(SyntaxNode node)
    {
        throw new NotImplementedException(
                $"Conversion for {VBasic.VisualBasicExtensions.Kind(node)} not implemented, please report this issue")
            .WithNodeInformation(node);
    }


    public override Task<CSharpSyntaxNode> VisitMemberAccessExpression(VBSyntax.MemberAccessExpressionSyntax node) => _nameExpressionNodeVisitor.ConvertMemberAccessExpressionAsync(node);
    public override Task<CSharpSyntaxNode> VisitGlobalName(VBSyntax.GlobalNameSyntax node) => _nameExpressionNodeVisitor.ConvertGlobalNameAsync(node);
    public override Task<CSharpSyntaxNode> VisitMeExpression(VBSyntax.MeExpressionSyntax node) => _nameExpressionNodeVisitor.ConvertMeExpressionAsync(node);
    public override Task<CSharpSyntaxNode> VisitMyBaseExpression(VBSyntax.MyBaseExpressionSyntax node) => _nameExpressionNodeVisitor.ConvertMyBaseExpressionAsync(node);
    public override Task<CSharpSyntaxNode> VisitGenericName(VBSyntax.GenericNameSyntax node) => _nameExpressionNodeVisitor.ConvertGenericNameAsync(node);
    public override Task<CSharpSyntaxNode> VisitQualifiedName(VBSyntax.QualifiedNameSyntax node) => _nameExpressionNodeVisitor.ConvertQualifiedNameAsync(node);
    public override Task<CSharpSyntaxNode> VisitIdentifierName(VBSyntax.IdentifierNameSyntax node) => _nameExpressionNodeVisitor.ConvertIdentifierNameAsync(node);
    public override Task<CSharpSyntaxNode> VisitInvocationExpression(VBSyntax.InvocationExpressionSyntax node) => _nameExpressionNodeVisitor.ConvertInvocationExpressionAsync(node);
    public override Task<CSharpSyntaxNode> VisitXmlEmbeddedExpression(VBSyntax.XmlEmbeddedExpressionSyntax node) => _xmlExpressionConverter.ConvertXmlEmbeddedExpressionAsync(node);
    public override Task<CSharpSyntaxNode> VisitXmlDocument(VBasic.Syntax.XmlDocumentSyntax node) => _xmlExpressionConverter.ConvertXmlDocumentAsync(node);
    public override Task<CSharpSyntaxNode> VisitXmlElement(VBasic.Syntax.XmlElementSyntax node) => _xmlExpressionConverter.ConvertXmlElementAsync(node);
    public override Task<CSharpSyntaxNode> VisitXmlEmptyElement(VBSyntax.XmlEmptyElementSyntax node) => _xmlExpressionConverter.ConvertXmlEmptyElementAsync(node);
    public override Task<CSharpSyntaxNode> VisitXmlAttribute(VBSyntax.XmlAttributeSyntax node) => _xmlExpressionConverter.ConvertXmlAttributeAsync(node);
    public override Task<CSharpSyntaxNode> VisitXmlString(VBSyntax.XmlStringSyntax node) => _xmlExpressionConverter.ConvertXmlStringAsync(node);
    public override Task<CSharpSyntaxNode> VisitXmlText(VBSyntax.XmlTextSyntax node) => _xmlExpressionConverter.ConvertXmlTextAsync(node);
    public override Task<CSharpSyntaxNode> VisitXmlCDataSection(VBSyntax.XmlCDataSectionSyntax node) => _xmlExpressionConverter.ConvertXmlCDataSectionAsync(node);
    public override Task<CSharpSyntaxNode> VisitXmlMemberAccessExpression(VBSyntax.XmlMemberAccessExpressionSyntax node) => _xmlExpressionConverter.ConvertXmlMemberAccessExpressionAsync(node);
    public override Task<CSharpSyntaxNode> VisitXmlBracketedName(VBSyntax.XmlBracketedNameSyntax node) => _xmlExpressionConverter.ConvertXmlBracketedNameAsync(node);
    public override Task<CSharpSyntaxNode> VisitXmlName(VBSyntax.XmlNameSyntax node) => _xmlExpressionConverter.ConvertXmlNameAsync(node);
    public override async Task<CSharpSyntaxNode> VisitSimpleArgument(VBasic.Syntax.SimpleArgumentSyntax node) => await _argumentConverter.ConvertSimpleArgumentAsync(node);
    public override async Task<CSharpSyntaxNode> VisitBinaryExpression(VBasic.Syntax.BinaryExpressionSyntax entryNode) => await _binaryExpressionConverter.ConvertBinaryExpressionAsync(entryNode);
    public override Task<CSharpSyntaxNode> VisitSingleLineLambdaExpression(VBasic.Syntax.SingleLineLambdaExpressionSyntax node) => _lambdaConverter.ConvertSingleLineLambdaAsync(node);
    public override Task<CSharpSyntaxNode> VisitMultiLineLambdaExpression(VBasic.Syntax.MultiLineLambdaExpressionSyntax node) => _lambdaConverter.ConvertMultiLineLambdaAsync(node);
    public override Task<CSharpSyntaxNode> VisitInferredFieldInitializer(VBasic.Syntax.InferredFieldInitializerSyntax node) => _initializerConverter.ConvertInferredFieldInitializerAsync(node);
    /// <remarks>Collection initialization has many variants in both VB and C#. Please add especially many test cases when touching this.</remarks>
    public override Task<CSharpSyntaxNode> VisitCollectionInitializer(VBasic.Syntax.CollectionInitializerSyntax node) => _initializerConverter.ConvertCollectionInitializerAsync(node);
    public override Task<CSharpSyntaxNode> VisitObjectMemberInitializer(VBasic.Syntax.ObjectMemberInitializerSyntax node) => _initializerConverter.ConvertObjectMemberInitializerAsync(node);
    public override Task<CSharpSyntaxNode> VisitNamedFieldInitializer(VBasic.Syntax.NamedFieldInitializerSyntax node) => _initializerConverter.ConvertNamedFieldInitializerAsync(node);
    public override Task<CSharpSyntaxNode> VisitObjectCollectionInitializer(VBasic.Syntax.ObjectCollectionInitializerSyntax node) => _initializerConverter.ConvertObjectCollectionInitializerAsync(node);

    public override async Task<CSharpSyntaxNode> VisitGetTypeExpression(VBasic.Syntax.GetTypeExpressionSyntax node)
    {
        return CS.SyntaxFactory.TypeOfExpression(await node.Type.AcceptAsync<TypeSyntax>(TriviaConvertingExpressionVisitor));
    }

    public override async Task<CSharpSyntaxNode> VisitAwaitExpression(VBasic.Syntax.AwaitExpressionSyntax node)
    {
        return CS.SyntaxFactory.AwaitExpression(await node.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor));
    }

    public override async Task<CSharpSyntaxNode> VisitCTypeExpression(VBasic.Syntax.CTypeExpressionSyntax node)
    {
        var csharpArg = await node.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
        var typeInfo = _semanticModel.GetTypeInfo(node.Type);
        var forceTargetType = typeInfo.ConvertedType;
        return CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Expression, csharpArg, forceTargetType: forceTargetType, defaultToCast: true).AddParens();
    }

    public override async Task<CSharpSyntaxNode> VisitDirectCastExpression(VBasic.Syntax.DirectCastExpressionSyntax node)
    {
        return await ConvertCastExpressionAsync(node, castToOrNull: node.Type);
    }

    public override async Task<CSharpSyntaxNode> VisitPredefinedCastExpression(VBasic.Syntax.PredefinedCastExpressionSyntax node)
    {
        var simplifiedOrNull = await CommonConversions.WithRemovedRedundantConversionOrNullAsync(node, node.Expression);
        if (simplifiedOrNull != null) return simplifiedOrNull;

        var expressionSyntax = await node.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
        if (node.Keyword.IsKind(VBasic.SyntaxKind.CDateKeyword)) {

            _extraUsingDirectives.Add("Microsoft.VisualBasic.CompilerServices");
            return CS.SyntaxFactory.InvocationExpression(CS.SyntaxFactory.ParseExpression("Conversions.ToDate"), CS.SyntaxFactory.ArgumentList(
                CS.SyntaxFactory.SingletonSeparatedList(
                    CS.SyntaxFactory.Argument(expressionSyntax))));
        }

        var withConversion = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Expression, expressionSyntax, false, true, forceTargetType: _semanticModel.GetTypeInfo(node).Type);
        return node.ParenthesizeIfPrecedenceCouldChange(withConversion); // Use context of outer node, rather than just its exprssion, as the above method call would do if allowed to add parenthesis
    }

    public override async Task<CSharpSyntaxNode> VisitTryCastExpression(VBasic.Syntax.TryCastExpressionSyntax node)
    {
        return node.ParenthesizeIfPrecedenceCouldChange(CS.SyntaxFactory.BinaryExpression(
            CS.SyntaxKind.AsExpression,
            await node.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor),
            await node.Type.AcceptAsync<TypeSyntax>(TriviaConvertingExpressionVisitor)
        ));
    }

    public override async Task<CSharpSyntaxNode> VisitLiteralExpression(VBasic.Syntax.LiteralExpressionSyntax node)
    {
        var typeInfo = _semanticModel.GetTypeInfo(node);
        var convertedType = typeInfo.ConvertedType;
        if (node.Token.Value == null) {
            if (convertedType == null) {
                return CS.SyntaxFactory.LiteralExpression(CS.SyntaxKind.DefaultLiteralExpression);
            }

            return !convertedType.IsReferenceType ? CS.SyntaxFactory.DefaultExpression(CommonConversions.GetTypeSyntax(convertedType)) : CommonConversions.Literal(null);
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
        return CS.SyntaxFactory.Interpolation(await node.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor), await node.AlignmentClause.AcceptAsync<InterpolationAlignmentClauseSyntax>(TriviaConvertingExpressionVisitor), await node.FormatClause.AcceptAsync<InterpolationFormatClauseSyntax>(TriviaConvertingExpressionVisitor));
    }

    public override async Task<CSharpSyntaxNode> VisitInterpolatedStringExpression(VBasic.Syntax.InterpolatedStringExpressionSyntax node)
    {
        var useVerbatim = node.DescendantNodes().OfType<VBasic.Syntax.InterpolatedStringTextSyntax>().Any(c => LiteralConversions.IsWorthBeingAVerbatimString(c.TextToken.Text));
        var startToken = useVerbatim ?
            CS.SyntaxFactory.Token(default(SyntaxTriviaList), CS.SyntaxKind.InterpolatedVerbatimStringStartToken, "$@\"", "$@\"", default(SyntaxTriviaList))
            : CS.SyntaxFactory.Token(default(SyntaxTriviaList), CS.SyntaxKind.InterpolatedStringStartToken, "$\"", "$\"", default(SyntaxTriviaList));
        var contents = await node.Contents.SelectAsync(async c => await c.AcceptAsync<InterpolatedStringContentSyntax>(TriviaConvertingExpressionVisitor));
        InterpolatedStringExpressionSyntax interpolatedStringExpressionSyntax = CS.SyntaxFactory.InterpolatedStringExpression(startToken, CS.SyntaxFactory.List(contents), CS.SyntaxFactory.Token(CS.SyntaxKind.InterpolatedStringEndToken));
        return interpolatedStringExpressionSyntax;
    }

    public override async Task<CSharpSyntaxNode> VisitInterpolatedStringText(VBasic.Syntax.InterpolatedStringTextSyntax node)
    {
        var useVerbatim = node.Parent.DescendantNodes().OfType<VBasic.Syntax.InterpolatedStringTextSyntax>().Any(c => LiteralConversions.IsWorthBeingAVerbatimString(c.TextToken.Text));
        var textForUser = LiteralConversions.EscapeQuotes(node.TextToken.Text, node.TextToken.ValueText, useVerbatim);
        InterpolatedStringTextSyntax interpolatedStringTextSyntax = CS.SyntaxFactory.InterpolatedStringText(CS.SyntaxFactory.Token(default(SyntaxTriviaList), CS.SyntaxKind.InterpolatedStringTextToken, textForUser, node.TextToken.ValueText, default(SyntaxTriviaList)));
        return interpolatedStringTextSyntax;
    }

    public override async Task<CSharpSyntaxNode> VisitInterpolationAlignmentClause(VBasic.Syntax.InterpolationAlignmentClauseSyntax node)
    {
        return CS.SyntaxFactory.InterpolationAlignmentClause(CS.SyntaxFactory.Token(CS.SyntaxKind.CommaToken), await node.Value.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor));
    }

    public override async Task<CSharpSyntaxNode> VisitInterpolationFormatClause(VBasic.Syntax.InterpolationFormatClauseSyntax node)
    {
        var textForUser = LiteralConversions.EscapeEscapeChar(node.FormatStringToken.ValueText);
        SyntaxToken formatStringToken = CS.SyntaxFactory.Token(SyntaxTriviaList.Empty, CS.SyntaxKind.InterpolatedStringTextToken, textForUser, node.FormatStringToken.ValueText, SyntaxTriviaList.Empty);
        return CS.SyntaxFactory.InterpolationFormatClause(CS.SyntaxFactory.Token(CS.SyntaxKind.ColonToken), formatStringToken);
    }

    public override async Task<CSharpSyntaxNode> VisitParenthesizedExpression(VBasic.Syntax.ParenthesizedExpressionSyntax node)
    {
        var cSharpSyntaxNode = await node.Expression.AcceptAsync<CSharpSyntaxNode>(TriviaConvertingExpressionVisitor);
        // If structural changes are necessary the expression may have been lifted a statement (e.g. Type inferred lambda)
        return cSharpSyntaxNode is ExpressionSyntax expr ? CS.SyntaxFactory.ParenthesizedExpression(expr) : cSharpSyntaxNode;
    }

    public override async Task<CSharpSyntaxNode> VisitConditionalAccessExpression(VBasic.Syntax.ConditionalAccessExpressionSyntax node)
    {
        var leftExpression = await node.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor) ?? _withBlockLhs.Peek();
        return CS.SyntaxFactory.ConditionalAccessExpression(leftExpression, await node.WhenNotNull.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor));
    }

    public override async Task<CSharpSyntaxNode> VisitArgumentList(VBasic.Syntax.ArgumentListSyntax node)
    {
        if (node.Parent.IsKind(VBasic.SyntaxKind.Attribute)) {
            return CommonConversions.CreateAttributeArgumentList(await node.Arguments.SelectAsync(_argumentConverter.ToAttributeArgumentAsync));
        }
        var argumentSyntaxes = await _argumentConverter.ConvertArgumentsAsync(node);
        return CS.SyntaxFactory.ArgumentList(CS.SyntaxFactory.SeparatedList(argumentSyntaxes));
    }

    public override async Task<CSharpSyntaxNode> VisitNameOfExpression(VBasic.Syntax.NameOfExpressionSyntax node)
    {
        return CS.SyntaxFactory.InvocationExpression(ValidSyntaxFactory.NameOf(), CS.SyntaxFactory.ArgumentList(CS.SyntaxFactory.SingletonSeparatedList(CS.SyntaxFactory.Argument(await node.Argument.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor)))));
    }

    public override async Task<CSharpSyntaxNode> VisitEqualsValue(VBasic.Syntax.EqualsValueSyntax node)
    {
        return CS.SyntaxFactory.EqualsValueClause(await node.Value.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor));
    }

    public override async Task<CSharpSyntaxNode> VisitAnonymousObjectCreationExpression(VBasic.Syntax.AnonymousObjectCreationExpressionSyntax node)
    {
        var vbInitializers = node.Initializer.Initializers;
        try {
            var initializers = await vbInitializers.AcceptSeparatedListAsync<VBSyntax.FieldInitializerSyntax, AnonymousObjectMemberDeclaratorSyntax>(TriviaConvertingExpressionVisitor);
            return CS.SyntaxFactory.AnonymousObjectCreationExpression(initializers);
        } finally {
            var kvpsToPop = _tempNameForAnonymousScope.Where(t => t.Value.Peek().Scope == node).ToArray();
            foreach (var kvp in kvpsToPop) {
                if (kvp.Value.Count == 1) _tempNameForAnonymousScope.Remove(kvp.Key);
                else kvp.Value.Pop();
            }
        }
        
    }

    public override async Task<CSharpSyntaxNode> VisitObjectCreationExpression(VBasic.Syntax.ObjectCreationExpressionSyntax node)
    {

        var objectCreationExpressionSyntax = CS.SyntaxFactory.ObjectCreationExpression(
            await node.Type.AcceptAsync<TypeSyntax>(TriviaConvertingExpressionVisitor),
            // VB can omit empty arg lists:
            await _argumentConverter.ConvertArgumentListOrEmptyAsync(node, node.ArgumentList),
            null
        );
        async Task<InitializerExpressionSyntax> ConvertInitializer() => await node.Initializer.AcceptAsync<InitializerExpressionSyntax>(TriviaConvertingExpressionVisitor);
        if (node.Initializer is VBSyntax.ObjectMemberInitializerSyntax objectMemberInitializerSyntax && HasInitializersUsingImpliedLhs(objectMemberInitializerSyntax)) {

            var idToUse = _typeContext.PerScopeState.Hoist(new AdditionalDeclaration("init", objectCreationExpressionSyntax, CommonConversions.GetTypeSyntax(_semanticModel.GetTypeInfo(node).Type))).IdentifierName;
            _withBlockLhs.Push(idToUse);
            try {
                var initializer = await ConvertInitializer();
                var originalExpressions = initializer.Expressions.Select(x => x is AssignmentExpressionSyntax e ? e.ReplaceNode(e.Left, CS.SyntaxFactory.MemberAccessExpression(CS.SyntaxKind.SimpleMemberAccessExpression, idToUse, (SimpleNameSyntax) e.Left)) : null).ToArray<ExpressionSyntax>();
                var expressions = CS.SyntaxFactory.SeparatedList(originalExpressions.Append(idToUse).Select(CS.SyntaxFactory.Argument));
                var tuple = CS.SyntaxFactory.TupleExpression(expressions);
                return CS.SyntaxFactory.MemberAccessExpression(CS.SyntaxKind.SimpleMemberAccessExpression, tuple, idToUse);
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
        return CS.SyntaxFactory.ArrayCreationExpression(
            CS.SyntaxFactory.ArrayType(await node.Type.AcceptAsync<TypeSyntax>(TriviaConvertingExpressionVisitor), bounds),
            await initializerToConvert.AcceptAsync<InitializerExpressionSyntax>(TriviaConvertingExpressionVisitor)
        );
    }


    public override async Task<CSharpSyntaxNode> VisitQueryExpression(VBasic.Syntax.QueryExpressionSyntax node)
    {
        return await _queryConverter.ConvertClausesAsync(node.Clauses);
    }

    public override async Task<CSharpSyntaxNode> VisitOrdering(VBasic.Syntax.OrderingSyntax node)
    {
        var convertToken = node.Kind().ConvertToken();
        var expressionSyntax = await node.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
        var ascendingOrDescendingKeyword = node.AscendingOrDescendingKeyword.ConvertToken();
        return CS.SyntaxFactory.Ordering(convertToken, expressionSyntax, ascendingOrDescendingKeyword);
    }
    public override async Task<CSharpSyntaxNode> VisitVariableNameEquals(VBSyntax.VariableNameEqualsSyntax node) =>
        CS.SyntaxFactory.NameEquals(CS.SyntaxFactory.IdentifierName(ConvertIdentifier(node.Identifier.Identifier)));


    public override async Task<CSharpSyntaxNode> VisitBinaryConditionalExpression(VBasic.Syntax.BinaryConditionalExpressionSyntax node)
    {
        var leftSide = await node.FirstExpression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
        var rightSide = await node.SecondExpression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
        var expr = CS.SyntaxFactory.BinaryExpression(CS.SyntaxKind.CoalesceExpression,
            node.FirstExpression.ParenthesizeIfPrecedenceCouldChange(leftSide),
            node.SecondExpression.ParenthesizeIfPrecedenceCouldChange(rightSide));

        if (node.Parent.IsKind(VBasic.SyntaxKind.Interpolation) || node.PrecedenceCouldChange())
            return CS.SyntaxFactory.ParenthesizedExpression(expr);

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

        var expr = CS.SyntaxFactory.ConditionalExpression(condition, whenTrue, whenFalse);


        if (node.Parent.IsKind(VBasic.SyntaxKind.Interpolation) || node.PrecedenceCouldChange())
            return CS.SyntaxFactory.ParenthesizedExpression(expr);

        return expr;
    }

    public override async Task<CSharpSyntaxNode> VisitTypeOfExpression(VBasic.Syntax.TypeOfExpressionSyntax node)
    {
        var expr = CS.SyntaxFactory.BinaryExpression(
            CS.SyntaxKind.IsExpression,
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
        CS.SyntaxKind csTokenKind = CSharpUtil.GetExpressionOperatorTokenKind(kind);

        if (kind == CS.SyntaxKind.LogicalNotExpression && _semanticModel.GetTypeInfo(node.Operand).ConvertedType is { } t) {
            if (t.IsNumericType() || t.IsEnumType()) {
                csTokenKind = CS.SyntaxKind.TildeToken;
            } else if (await NegateAndSimplifyOrNullAsync(node, expr, t) is { } simpleNegation) {
                return simpleNegation;
            }
        }

        return CS.SyntaxFactory.PrefixUnaryExpression(
            kind,
            CS.SyntaxFactory.Token(csTokenKind),
            expr.AddParens()
        );
    }

    private async Task<ExpressionSyntax> NegateAndSimplifyOrNullAsync(VBSyntax.UnaryExpressionSyntax node, ExpressionSyntax expr, ITypeSymbol operandConvertedType)
    {
        if (await _operatorConverter.ConvertReferenceOrNothingComparisonOrNullAsync(node.Operand.SkipIntoParens(), TriviaConvertingExpressionVisitor.IsWithinQuery, true) is { } nothingComparison) {
            return nothingComparison;
        }
        if (operandConvertedType.GetNullableUnderlyingType()?.SpecialType == SpecialType.System_Boolean && node.AlwaysHasBooleanTypeInCSharp()) {
            return CS.SyntaxFactory.BinaryExpression(CS.SyntaxKind.EqualsExpression, expr, LiteralConversions.GetLiteralExpression(false));
        }

        if (expr is BinaryExpressionSyntax eq && eq.OperatorToken.IsKind(CS.SyntaxKind.EqualsEqualsToken, CS.SyntaxKind.ExclamationEqualsToken)){
            return eq.WithOperatorToken(CS.SyntaxFactory.Token(eq.OperatorToken.IsKind(CS.SyntaxKind.ExclamationEqualsToken) ? CS.SyntaxKind.EqualsEqualsToken : CS.SyntaxKind.ExclamationEqualsToken));
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


    public override async Task<CSharpSyntaxNode> VisitParameterList(VBSyntax.ParameterListSyntax node)
    {
        var parameters = await node.Parameters.SelectAsync(async p => await p.AcceptAsync<ParameterSyntax>(TriviaConvertingExpressionVisitor));
        if (node.Parent is VBSyntax.PropertyStatementSyntax && CommonConversions.IsDefaultIndexer(node.Parent)) {
            return CS.SyntaxFactory.BracketedParameterList(CS.SyntaxFactory.SeparatedList(parameters));
        }
        return CS.SyntaxFactory.ParameterList(CS.SyntaxFactory.SeparatedList(parameters));
    }

    public override async Task<CSharpSyntaxNode> VisitParameter(VBSyntax.ParameterSyntax node)
    {
        var id = CommonConversions.ConvertIdentifier(node.Identifier.Identifier);

        TypeSyntax paramType = null;
        if (node.Parent?.Parent?.IsKind(VBasic.SyntaxKind.FunctionLambdaHeader,
                VBasic.SyntaxKind.SubLambdaHeader) != true || node.AsClause != null) {
            var vbParamSymbol = (IParameterSymbol)_semanticModel.GetDeclaredSymbol(node);
            paramType = vbParamSymbol != null ? CommonConversions.GetTypeSyntax(vbParamSymbol.Type)
                : await SyntaxOnlyConvertParamAsync(node);
        }

        var attributes = (await node.AttributeLists.SelectManyAsync(CommonConversions.ConvertAttributeAsync)).ToList();
        var modifiers = CommonConversions.ConvertModifiers(node, node.Modifiers, TokenContext.Local);
        var vbSymbol = _semanticModel.GetDeclaredSymbol(node);
        var baseParameters = vbSymbol?.ContainingSymbol.OriginalDefinition.GetBaseSymbol().GetParameters();
        var baseParameter = baseParameters?[vbSymbol.Ordinal];

        var csRefKind = CommonConversions.GetCsRefKind(baseParameter ?? vbSymbol, node);
        if (csRefKind == RefKind.Out) {
            modifiers = CS.SyntaxFactory.TokenList(modifiers
                .Where(m => !m.IsKind(CS.SyntaxKind.RefKeyword))
                .Concat(CS.SyntaxFactory.Token(CS.SyntaxKind.OutKeyword).Yield())
            );
        }

        EqualsValueClauseSyntax @default = null;
        // Parameterized properties get compiled/converted to a method with non-optional parameters
        if (node.Default != null) {
            var defaultValue = node.Default.Value.SkipIntoParens();
            if (_semanticModel.GetTypeInfo(defaultValue).Type?.SpecialType == SpecialType.System_DateTime) {
                var constant = _semanticModel.GetConstantValue(defaultValue);
                if (constant.HasValue && constant.Value is DateTime dt) {
                    var dateTimeAsLongCsLiteral = CommonConversions.Literal(dt.Ticks)
                        .WithTrailingTrivia(CS.SyntaxFactory.ParseTrailingTrivia($"/* {defaultValue} */"));
                    var dateTimeArg = CommonConversions.CreateAttributeArgumentList(CS.SyntaxFactory.AttributeArgument(dateTimeAsLongCsLiteral));
                    _extraUsingDirectives.Add("System.Runtime.InteropServices");
                    _extraUsingDirectives.Add("System.Runtime.CompilerServices");
                    var optionalDateTimeAttributes = new[] {
                        CS.SyntaxFactory.Attribute(ValidSyntaxFactory.IdentifierName("Optional")),
                        CS.SyntaxFactory.Attribute(ValidSyntaxFactory.IdentifierName("DateTimeConstant"), dateTimeArg)
                    };
                    attributes.Insert(0,
                        CS.SyntaxFactory.AttributeList(CS.SyntaxFactory.SeparatedList(optionalDateTimeAttributes)));
                }
            } else if (node.Modifiers.Any(m => m.IsKind(VBasic.SyntaxKind.ByRefKeyword)) || HasRefParametersAfterThisOne()) {
                var defaultExpression = await node.Default.Value.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
                var arg = CommonConversions.CreateAttributeArgumentList(CS.SyntaxFactory.AttributeArgument(defaultExpression));
                _extraUsingDirectives.Add("System.Runtime.InteropServices");
                _extraUsingDirectives.Add("System.Runtime.CompilerServices");
                var optionalAttributes = new List<AttributeSyntax> {
                    CS.SyntaxFactory.Attribute(ValidSyntaxFactory.IdentifierName("Optional")),
                };
                if (!node.Default.Value.IsKind(VBasic.SyntaxKind.NothingLiteralExpression)) {
                    optionalAttributes.Add(CS.SyntaxFactory.Attribute(ValidSyntaxFactory.IdentifierName("DefaultParameterValue"), arg));
                }
                attributes.Insert(0,
                    CS.SyntaxFactory.AttributeList(CS.SyntaxFactory.SeparatedList(optionalAttributes)));
            } else {
                @default = CS.SyntaxFactory.EqualsValueClause(
                    await node.Default.Value.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor));
            }
        }

        if (node.Parent.Parent is VBSyntax.MethodStatementSyntax mss
            && mss.AttributeLists.Any(CommonConversions.HasExtensionAttribute) && node.Parent.ChildNodes().First() == node &&
            vbSymbol.ValidCSharpExtensionMethodParameter()) {
            modifiers = modifiers.Insert(0, CS.SyntaxFactory.Token(CS.SyntaxKind.ThisKeyword));
        }
        return CS.SyntaxFactory.Parameter(
            CS.SyntaxFactory.List(attributes),
            modifiers,
            paramType,
            id,
            @default
        );

        bool HasRefParametersAfterThisOne() => vbSymbol is not null && baseParameters is {} bp && bp.Skip(vbSymbol.Ordinal + 1).Any(x => x.RefKind != RefKind.None);
    }

    private async Task<TypeSyntax> SyntaxOnlyConvertParamAsync(VBSyntax.ParameterSyntax node)
    {
        var syntaxParamType = await (node.AsClause?.Type).AcceptAsync<TypeSyntax>(TriviaConvertingExpressionVisitor)
                              ?? CS.SyntaxFactory.PredefinedType(CS.SyntaxFactory.Token(CS.SyntaxKind.ObjectKeyword));

        var rankSpecifiers = await CommonConversions.ConvertArrayRankSpecifierSyntaxesAsync(node.Identifier.ArrayRankSpecifiers, node.Identifier.ArrayBounds, false);
        if (rankSpecifiers.Any()) {
            syntaxParamType = CS.SyntaxFactory.ArrayType(syntaxParamType, rankSpecifiers);
        }

        if (!node.Identifier.Nullable.IsKind(CS.SyntaxKind.None)) {
            var arrayType = syntaxParamType as ArrayTypeSyntax;
            if (arrayType == null) {
                syntaxParamType = CS.SyntaxFactory.NullableType(syntaxParamType);
            } else {
                syntaxParamType = arrayType.WithElementType(CS.SyntaxFactory.NullableType(arrayType.ElementType));
            }
        }
        return syntaxParamType;
    }

    public override async Task<CSharpSyntaxNode> VisitAttribute(VBSyntax.AttributeSyntax node)
    {
        return CS.SyntaxFactory.AttributeList(
            node.Target == null ? null : CS.SyntaxFactory.AttributeTargetSpecifier(node.Target.AttributeModifier.ConvertToken()),
            CS.SyntaxFactory.SingletonSeparatedList(CS.SyntaxFactory.Attribute(await node.Name.AcceptAsync<NameSyntax>(TriviaConvertingExpressionVisitor), await node.ArgumentList.AcceptAsync<AttributeArgumentListSyntax>(TriviaConvertingExpressionVisitor)))
        );
    }

    public override async Task<CSharpSyntaxNode> VisitTupleType(VBasic.Syntax.TupleTypeSyntax node)
    {
        var elements = await node.Elements.SelectAsync(async e => await e.AcceptAsync<TupleElementSyntax>(TriviaConvertingExpressionVisitor));
        return CS.SyntaxFactory.TupleType(CS.SyntaxFactory.SeparatedList(elements));
    }

    public override async Task<CSharpSyntaxNode> VisitTypedTupleElement(VBasic.Syntax.TypedTupleElementSyntax node)
    {
        return CS.SyntaxFactory.TupleElement(await node.Type.AcceptAsync<TypeSyntax>(TriviaConvertingExpressionVisitor));
    }

    public override async Task<CSharpSyntaxNode> VisitNamedTupleElement(VBasic.Syntax.NamedTupleElementSyntax node)
    {
        return CS.SyntaxFactory.TupleElement(await node.AsClause.Type.AcceptAsync<TypeSyntax>(TriviaConvertingExpressionVisitor), CommonConversions.ConvertIdentifier(node.Identifier));
    }

    public override async Task<CSharpSyntaxNode> VisitTupleExpression(VBasic.Syntax.TupleExpressionSyntax node)
    {
        var args = await node.Arguments.SelectAsync(async a => {
            var expr = await a.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
            return CS.SyntaxFactory.Argument(expr);
        });
        return CS.SyntaxFactory.TupleExpression(CS.SyntaxFactory.SeparatedList(args));
    }

    public override async Task<CSharpSyntaxNode> VisitPredefinedType(VBasic.Syntax.PredefinedTypeSyntax node)
    {
        if (node.Keyword.IsKind(VBasic.SyntaxKind.DateKeyword)) {
            return ValidSyntaxFactory.IdentifierName(nameof(DateTime));
        }
        return CS.SyntaxFactory.PredefinedType(node.Keyword.ConvertToken());
    }

    public override async Task<CSharpSyntaxNode> VisitNullableType(VBasic.Syntax.NullableTypeSyntax node)
    {
        return CS.SyntaxFactory.NullableType(await node.ElementType.AcceptAsync<TypeSyntax>(TriviaConvertingExpressionVisitor));
    }

    public override async Task<CSharpSyntaxNode> VisitArrayType(VBasic.Syntax.ArrayTypeSyntax node)
    {
        var ranks = await node.RankSpecifiers.SelectAsync(async r => await r.AcceptAsync<ArrayRankSpecifierSyntax>(TriviaConvertingExpressionVisitor));
        return CS.SyntaxFactory.ArrayType(await node.ElementType.AcceptAsync<TypeSyntax>(TriviaConvertingExpressionVisitor), CS.SyntaxFactory.List(ranks));
    }

    public override async Task<CSharpSyntaxNode> VisitArrayRankSpecifier(VBasic.Syntax.ArrayRankSpecifierSyntax node)
    {
        return CS.SyntaxFactory.ArrayRankSpecifier(CS.SyntaxFactory.SeparatedList(Enumerable.Repeat<ExpressionSyntax>(CS.SyntaxFactory.OmittedArraySizeExpression(), node.Rank)));
    }


    public override async Task<CSharpSyntaxNode> VisitTypeArgumentList(VBasic.Syntax.TypeArgumentListSyntax node)
    {
        var args = await node.Arguments.SelectAsync(async a => await a.AcceptAsync<TypeSyntax>(TriviaConvertingExpressionVisitor));
        return CS.SyntaxFactory.TypeArgumentList(CS.SyntaxFactory.SeparatedList(args));
    }

    private async Task<CSharpSyntaxNode> ConvertCastExpressionAsync(VBSyntax.CastExpressionSyntax node,
        ExpressionSyntax convertMethodOrNull = null, VBSyntax.TypeSyntax castToOrNull = null)
    {
        var simplifiedOrNull = await CommonConversions.WithRemovedRedundantConversionOrNullAsync(node, node.Expression);
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
            CS.SyntaxFactory.InvocationExpression(toInvoke,
                CS.SyntaxFactory.ArgumentList(
                    CS.SyntaxFactory.SingletonSeparatedList(
                        CS.SyntaxFactory.Argument(argExpression)))
            );
    }

    private SyntaxToken ConvertIdentifier(SyntaxToken identifierIdentifier, bool isAttribute = false) =>
        CommonConversions.ConvertIdentifier(identifierIdentifier, isAttribute);

    public Task<IReadOnlyCollection<StatementSyntax>> ConvertMethodBodyStatementsAsync(VBasic.VisualBasicSyntaxNode node, IReadOnlyCollection<VBSyntax.StatementSyntax> statements, bool isIterator = false, IdentifierNameSyntax csReturnVariable = null) =>
        _lambdaConverter.ConvertMethodBodyStatementsAsync(node, statements, isIterator, csReturnVariable);
}
