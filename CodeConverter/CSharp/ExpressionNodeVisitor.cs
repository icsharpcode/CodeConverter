using System.Collections.Immutable;
using System.Data;
using System.Globalization;
using ICSharpCode.CodeConverter.CSharp.Replacements;
using ICSharpCode.CodeConverter.Util.FromRoslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using ComparisonKind = ICSharpCode.CodeConverter.CSharp.VisualBasicEqualityComparison.ComparisonKind;

namespace ICSharpCode.CodeConverter.CSharp;

/// <summary>
/// These could be nested within something like a field declaration, an arrow bodied member, or a statement within a method body
/// To understand the difference between how expressions are expressed, compare:
/// http://source.roslyn.codeplex.com/#Microsoft.CodeAnalysis.CSharp/Binder/Binder_Expressions.cs,365
/// http://source.roslyn.codeplex.com/#Microsoft.CodeAnalysis.VisualBasic/Binding/Binder_Expressions.vb,43
/// </summary>
internal partial class ExpressionNodeVisitor : VBasic.VisualBasicSyntaxVisitor<Task<CSharpSyntaxNode>>
{
    private static readonly Type ConvertType = typeof(Conversions);
    public CommentConvertingVisitorWrapper TriviaConvertingExpressionVisitor { get; }
    private readonly SemanticModel _semanticModel;
    private readonly HashSet<string> _extraUsingDirectives;
    private readonly IOperatorConverter _operatorConverter;
    private readonly VisualBasicEqualityComparison _visualBasicEqualityComparison;
    private readonly Stack<ExpressionSyntax> _withBlockLhs = new();
    private readonly ITypeContext _typeContext;
    private readonly QueryConverter _queryConverter;
    private readonly Lazy<IReadOnlyDictionary<ITypeSymbol, string>> _convertMethodsLookupByReturnType;
    private readonly LambdaConverter _lambdaConverter;
    private readonly Dictionary<string, Stack<(SyntaxNode Scope, string TempName)>> _tempNameForAnonymousScope = new();
    private readonly HashSet<string> _generatedNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly XmlExpressionConverter _xmlExpressionConverter;
    private readonly NameExpressionNodeVisitor _nameExpressionNodeVisitor;
    private readonly ArgumentConverter _argumentConverter;
    private readonly BinaryExpressionConverter _binaryExpressionConverter;

    public ExpressionNodeVisitor(SemanticModel semanticModel,
        VisualBasicEqualityComparison visualBasicEqualityComparison, ITypeContext typeContext, CommonConversions commonConversions,
        HashSet<string> extraUsingDirectives, XmlImportContext xmlImportContext, VisualBasicNullableExpressionsConverter visualBasicNullableTypesConverter)
    {
        _semanticModel = semanticModel;
        CommonConversions = commonConversions;;
        _lambdaConverter = new LambdaConverter(commonConversions, semanticModel);
        _visualBasicEqualityComparison = visualBasicEqualityComparison;
        TriviaConvertingExpressionVisitor = new CommentConvertingVisitorWrapper(this, _semanticModel.SyntaxTree);
        _queryConverter = new QueryConverter(commonConversions, _semanticModel, TriviaConvertingExpressionVisitor);
        _typeContext = typeContext;
        _extraUsingDirectives = extraUsingDirectives;
        _argumentConverter = new ArgumentConverter(visualBasicEqualityComparison, typeContext, semanticModel, commonConversions);
        _xmlExpressionConverter = new XmlExpressionConverter(xmlImportContext, extraUsingDirectives, TriviaConvertingExpressionVisitor);
        _nameExpressionNodeVisitor = new NameExpressionNodeVisitor(semanticModel, _generatedNames, typeContext, extraUsingDirectives, _tempNameForAnonymousScope, _withBlockLhs, commonConversions, _argumentConverter, TriviaConvertingExpressionVisitor);
        _visualBasicNullableTypesConverter = visualBasicNullableTypesConverter;
        _operatorConverter = VbOperatorConversion.Create(TriviaConvertingExpressionVisitor, semanticModel, visualBasicEqualityComparison, commonConversions.TypeConversionAnalyzer);
        _binaryExpressionConverter = new BinaryExpressionConverter(semanticModel, _operatorConverter, visualBasicEqualityComparison, visualBasicNullableTypesConverter, commonConversions);
        // If this isn't needed, the assembly with Conversions may not be referenced, so this must be done lazily
        _convertMethodsLookupByReturnType =
            new Lazy<IReadOnlyDictionary<ITypeSymbol, string>>(() => CreateConvertMethodsLookupByReturnType(semanticModel));
    }

    private static IReadOnlyDictionary<ITypeSymbol, string> CreateConvertMethodsLookupByReturnType(
        SemanticModel semanticModel)
    {
        // In some projects there's a source declaration as well as the referenced one, which causes the first of these methods to fail
        var symbolsWithName = semanticModel.Compilation
            .GetSymbolsWithName(n => n.Equals(ConvertType.Name, StringComparison.Ordinal), SymbolFilter.Type).ToList();
        
        var convertType =
            semanticModel.Compilation.GetTypeByMetadataName(ConvertType.FullName) ??
            (ITypeSymbol)symbolsWithName.FirstOrDefault(s =>
                    s.ContainingNamespace.ToDisplayString().Equals(ConvertType.Namespace, StringComparison.Ordinal));

        if (convertType is null) return ImmutableDictionary<ITypeSymbol, string>.Empty;

        var convertMethods = convertType.GetMembers().Where(m =>
            m.Name.StartsWith("To", StringComparison.Ordinal) && m.GetParameters().Length == 1);

#pragma warning disable RS1024 // Compare symbols correctly - GroupBy and ToDictionary use the same logic to dedupe as to lookup, so it doesn't matter which equality is used
        var methodsByType = convertMethods
            .GroupBy(m => new { ReturnType = m.GetReturnType(), Name = $"{ConvertType.FullName}.{m.Name}" })
            .ToDictionary(m => m.Key.ReturnType, m => m.Key.Name);
#pragma warning restore RS1024 // Compare symbols correctly

        return methodsByType;
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

    public override async Task<CSharpSyntaxNode> VisitGetTypeExpression(VBasic.Syntax.GetTypeExpressionSyntax node)
    {
        return SyntaxFactory.TypeOfExpression(await node.Type.AcceptAsync<TypeSyntax>(TriviaConvertingExpressionVisitor));
    }

    public override async Task<CSharpSyntaxNode> VisitAwaitExpression(VBasic.Syntax.AwaitExpressionSyntax node)
    {
        return SyntaxFactory.AwaitExpression(await node.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor));
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
            return SyntaxFactory.InvocationExpression(SyntaxFactory.ParseExpression("Conversions.ToDate"), SyntaxFactory.ArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(expressionSyntax))));
        }

        var withConversion = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Expression, expressionSyntax, false, true, forceTargetType: _semanticModel.GetTypeInfo(node).Type);
        return node.ParenthesizeIfPrecedenceCouldChange(withConversion); // Use context of outer node, rather than just its exprssion, as the above method call would do if allowed to add parenthesis
    }

    public override async Task<CSharpSyntaxNode> VisitTryCastExpression(VBasic.Syntax.TryCastExpressionSyntax node)
    {
        return node.ParenthesizeIfPrecedenceCouldChange(SyntaxFactory.BinaryExpression(
            SyntaxKind.AsExpression,
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

    public override async Task<CSharpSyntaxNode> VisitParenthesizedExpression(VBasic.Syntax.ParenthesizedExpressionSyntax node)
    {
        var cSharpSyntaxNode = await node.Expression.AcceptAsync<CSharpSyntaxNode>(TriviaConvertingExpressionVisitor);
        // If structural changes are necessary the expression may have been lifted a statement (e.g. Type inferred lambda)
        return cSharpSyntaxNode is ExpressionSyntax expr ? SyntaxFactory.ParenthesizedExpression(expr) : cSharpSyntaxNode;
    }

    public override async Task<CSharpSyntaxNode> VisitConditionalAccessExpression(VBasic.Syntax.ConditionalAccessExpressionSyntax node)
    {
        var leftExpression = await node.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor) ?? _withBlockLhs.Peek();
        return SyntaxFactory.ConditionalAccessExpression(leftExpression, await node.WhenNotNull.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor));
    }

    public override async Task<CSharpSyntaxNode> VisitArgumentList(VBasic.Syntax.ArgumentListSyntax node)
    {
        if (node.Parent.IsKind(VBasic.SyntaxKind.Attribute)) {
            return CommonConversions.CreateAttributeArgumentList(await node.Arguments.SelectAsync(_argumentConverter.ToAttributeArgumentAsync));
        }
        var argumentSyntaxes = await _argumentConverter.ConvertArgumentsAsync(node);
        return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(argumentSyntaxes));
    }

    public override async Task<CSharpSyntaxNode> VisitNameOfExpression(VBasic.Syntax.NameOfExpressionSyntax node)
    {
        return SyntaxFactory.InvocationExpression(ValidSyntaxFactory.NameOf(), SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(await node.Argument.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor)))));
    }

    public override async Task<CSharpSyntaxNode> VisitEqualsValue(VBasic.Syntax.EqualsValueSyntax node)
    {
        return SyntaxFactory.EqualsValueClause(await node.Value.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor));
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

    /// <remarks>Collection initialization has many variants in both VB and C#. Please add especially many test cases when touching this.</remarks>
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
        var dimensions = convertedType is IArrayTypeSymbol ats ? ats.Rank : 1; // For multidimensional array [,] note these are different from nested arrays [][]
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

    public override async Task<CSharpSyntaxNode> VisitOrdering(VBasic.Syntax.OrderingSyntax node)
    {
        var convertToken = node.Kind().ConvertToken();
        var expressionSyntax = await node.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
        var ascendingOrDescendingKeyword = node.AscendingOrDescendingKeyword.ConvertToken();
        return SyntaxFactory.Ordering(convertToken, expressionSyntax, ascendingOrDescendingKeyword);
    }

    public override async Task<CSharpSyntaxNode> VisitObjectMemberInitializer(VBasic.Syntax.ObjectMemberInitializerSyntax node)
    {
        var initializers = await node.Initializers.AcceptSeparatedListAsync<VBSyntax.FieldInitializerSyntax, ExpressionSyntax>(TriviaConvertingExpressionVisitor);
        return SyntaxFactory.InitializerExpression(SyntaxKind.ObjectInitializerExpression, initializers);
    }

    public override async Task<CSharpSyntaxNode> VisitNamedFieldInitializer(VBasic.Syntax.NamedFieldInitializerSyntax node)
    {
        var csExpressionSyntax = await node.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
        csExpressionSyntax =
            CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Expression, csExpressionSyntax);
        if (node.Parent?.Parent is VBasic.Syntax.AnonymousObjectCreationExpressionSyntax {Initializer: {Initializers: var initializers}} anonymousObjectCreationExpression) {
            string nameIdentifierText = node.Name.Identifier.Text;
            var isAnonymouslyReused = initializers.OfType<VBasic.Syntax.NamedFieldInitializerSyntax>()
                .Select(i => i.Expression).OfType<VBasic.Syntax.MemberAccessExpressionSyntax>()
                .Any(maes => maes.Expression is null && maes.Name.Identifier.Text.Equals(nameIdentifierText, StringComparison.OrdinalIgnoreCase));
            if (isAnonymouslyReused) {
                string tempNameForAnonymousSelfReference = GenerateUniqueVariableName(node.Name, "temp" + ((VBSyntax.SimpleNameSyntax) node.Name).Identifier.Text.UppercaseFirstLetter());
                csExpressionSyntax = DeclareVariableInline(csExpressionSyntax, tempNameForAnonymousSelfReference);
                if (!_tempNameForAnonymousScope.TryGetValue(nameIdentifierText, out var stack)) {
                    stack = _tempNameForAnonymousScope[nameIdentifierText] = new Stack<(SyntaxNode Scope, string TempName)>();
                }
                stack.Push((anonymousObjectCreationExpression, tempNameForAnonymousSelfReference));
            }

            var anonymousObjectMemberDeclaratorSyntax = SyntaxFactory.AnonymousObjectMemberDeclarator(
                SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName(ConvertIdentifier(node.Name.Identifier))),
                csExpressionSyntax);
            return anonymousObjectMemberDeclaratorSyntax;
        }

        return SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
            await node.Name.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor),
            csExpressionSyntax
        );
    }

    private string GenerateUniqueVariableName(VisualBasicSyntaxNode existingNode, string varNameBase) => NameGenerator.CS.GetUniqueVariableNameInScope(_semanticModel, _generatedNames, existingNode, varNameBase);

    private static ExpressionSyntax DeclareVariableInline(ExpressionSyntax csExpressionSyntax, string temporaryName)
    {
        var temporaryNameId = SyntaxFactory.Identifier(temporaryName);
        var temporaryNameExpression = ValidSyntaxFactory.IdentifierName(temporaryNameId);
        csExpressionSyntax = SyntaxFactory.ConditionalExpression(
            SyntaxFactory.IsPatternExpression(
                csExpressionSyntax,
                SyntaxFactory.VarPattern(
                    SyntaxFactory.SingleVariableDesignation(temporaryNameId))),
            temporaryNameExpression,
            SyntaxFactory.LiteralExpression(
                SyntaxKind.DefaultLiteralExpression,
                SyntaxFactory.Token(SyntaxKind.DefaultKeyword)));
        return csExpressionSyntax;
    }

    public override async Task<CSharpSyntaxNode> VisitVariableNameEquals(VBSyntax.VariableNameEqualsSyntax node) =>
        SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName(ConvertIdentifier(node.Identifier.Identifier)));

    public override async Task<CSharpSyntaxNode> VisitObjectCollectionInitializer(VBasic.Syntax.ObjectCollectionInitializerSyntax node)
    {
        return await node.Initializer.AcceptAsync<CSharpSyntaxNode>(TriviaConvertingExpressionVisitor); //Dictionary initializer comes through here despite the FROM keyword not being in the source code
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


    public override async Task<CSharpSyntaxNode> VisitSingleLineLambdaExpression(VBasic.Syntax.SingleLineLambdaExpressionSyntax node)
    {
        var originalIsWithinQuery = TriviaConvertingExpressionVisitor.IsWithinQuery;
        TriviaConvertingExpressionVisitor.IsWithinQuery = CommonConversions.IsLinqDelegateExpression(node);
        try {
            return await ConvertInnerAsync();
        } finally {
            TriviaConvertingExpressionVisitor.IsWithinQuery = originalIsWithinQuery;
        }

        async Task<CSharpSyntaxNode> ConvertInnerAsync()
        {
            IReadOnlyCollection<StatementSyntax> convertedStatements;
            if (node.Body is VBasic.Syntax.StatementSyntax statement)
            {
                convertedStatements = await ConvertMethodBodyStatementsAsync(statement, statement.Yield().ToArray());
            }
            else
            {
                var csNode = await node.Body.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
                convertedStatements = new[] {SyntaxFactory.ExpressionStatement(csNode)};
            }

            var param = await node.SubOrFunctionHeader.ParameterList.AcceptAsync<ParameterListSyntax>(TriviaConvertingExpressionVisitor);
            return await _lambdaConverter.ConvertAsync(node, param, convertedStatements);
        }
    }

    public override async Task<CSharpSyntaxNode> VisitMultiLineLambdaExpression(VBasic.Syntax.MultiLineLambdaExpressionSyntax node)
    {
        var originalIsWithinQuery = TriviaConvertingExpressionVisitor.IsWithinQuery;
        TriviaConvertingExpressionVisitor.IsWithinQuery = CommonConversions.IsLinqDelegateExpression(node);
        try {
            return await ConvertInnerAsync();
        } finally {
            TriviaConvertingExpressionVisitor.IsWithinQuery = originalIsWithinQuery;
        }

        async Task<CSharpSyntaxNode> ConvertInnerAsync()
        {
            var body = await ConvertMethodBodyStatementsAsync(node, node.Statements);
            var param = await node.SubOrFunctionHeader.ParameterList.AcceptAsync<ParameterListSyntax>(TriviaConvertingExpressionVisitor);
            return await _lambdaConverter.ConvertAsync(node, param, body.ToList());
        }
    }

    public async Task<IReadOnlyCollection<StatementSyntax>> ConvertMethodBodyStatementsAsync(VBasic.VisualBasicSyntaxNode node, IReadOnlyCollection<VBSyntax.StatementSyntax> statements, bool isIterator = false, IdentifierNameSyntax csReturnVariable = null)
    {

        var innerMethodBodyVisitor = await MethodBodyExecutableStatementVisitor.CreateAsync(node, _semanticModel, TriviaConvertingExpressionVisitor, CommonConversions, _visualBasicEqualityComparison, _withBlockLhs, _extraUsingDirectives, _typeContext, isIterator, csReturnVariable);
        return await GetWithConvertedGotosOrNull(statements) ?? await ConvertStatements(statements);

        async Task<List<StatementSyntax>> ConvertStatements(IEnumerable<VBSyntax.StatementSyntax> readOnlyCollection)
        {
            return (await readOnlyCollection.SelectManyAsync(async s => (IEnumerable<StatementSyntax>)await s.Accept(innerMethodBodyVisitor.CommentConvertingVisitor))).ToList();
        }

        async Task<IReadOnlyCollection<StatementSyntax>> GetWithConvertedGotosOrNull(IReadOnlyCollection<Microsoft.CodeAnalysis.VisualBasic.Syntax.StatementSyntax> statements)
        {
            var onlyIdentifierLabel = statements.OnlyOrDefault(s => s.IsKind(VBasic.SyntaxKind.LabelStatement));
            var onlyOnErrorGotoStatement = statements.OnlyOrDefault(s => s.IsKind(VBasic.SyntaxKind.OnErrorGoToLabelStatement));

            // See https://learn.microsoft.com/en-us/dotnet/visual-basic/language-reference/statements/on-error-statement
            if (onlyIdentifierLabel != null && onlyOnErrorGotoStatement != null) {
                var statementsList = statements.ToList();
                var onlyIdentifierLabelIndex = statementsList.IndexOf(onlyIdentifierLabel);
                var onlyOnErrorGotoStatementIndex = statementsList.IndexOf(onlyOnErrorGotoStatement);

                // Even this very simple case can generate compile errors if the error handling uses statements declared in the scope of the try block
                // For now, the user will have to fix these manually, in future it'd be possible to hoist any used declarations out of the try block
                if (onlyOnErrorGotoStatementIndex < onlyIdentifierLabelIndex) {
                    var beforeStatements = await ConvertStatements(statements.Take(onlyOnErrorGotoStatementIndex));
                    var tryBlockStatements = await ConvertStatements(statements.Take(onlyIdentifierLabelIndex).Skip(onlyOnErrorGotoStatementIndex + 1));
                    var tryBlock = SyntaxFactory.Block(tryBlockStatements);
                    var afterStatements = await ConvertStatements(statements.Skip(onlyIdentifierLabelIndex + 1));
                    
                    var catchClauseSyntax = SyntaxFactory.CatchClause();

                    // Default to putting the statements after the catch block in case logic falls through, but if the last statement is a return, put them inside the catch block for neatness.
                    if (tryBlockStatements.LastOrDefault().IsKind(SyntaxKind.ReturnStatement)) {
                        catchClauseSyntax = catchClauseSyntax.WithBlock(SyntaxFactory.Block(afterStatements));
                        afterStatements = new List<StatementSyntax>();
                    }

                    var tryStatement = SyntaxFactory.TryStatement(SyntaxFactory.SingletonList(catchClauseSyntax)).WithBlock(tryBlock);
                    return beforeStatements.Append(tryStatement).Concat(afterStatements).ToList();
                }
            }

            return null;
        }
    }

    public override async Task<CSharpSyntaxNode> VisitParameterList(VBSyntax.ParameterListSyntax node)
    {
        var parameters = await node.Parameters.SelectAsync(async p => await p.AcceptAsync<ParameterSyntax>(TriviaConvertingExpressionVisitor));
        if (node.Parent is VBSyntax.PropertyStatementSyntax && CommonConversions.IsDefaultIndexer(node.Parent)) {
            return SyntaxFactory.BracketedParameterList(SyntaxFactory.SeparatedList(parameters));
        }
        return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters));
    }

    public override async Task<CSharpSyntaxNode> VisitParameter(VBSyntax.ParameterSyntax node)
    {
        var id = CommonConversions.ConvertIdentifier(node.Identifier.Identifier);

        TypeSyntax paramType = null;
        if (node.Parent?.Parent?.IsKind(VBasic.SyntaxKind.FunctionLambdaHeader,
                VBasic.SyntaxKind.SubLambdaHeader) != true || node.AsClause != null) {
            var vbParamSymbol = _semanticModel.GetDeclaredSymbol(node) as IParameterSymbol;
            paramType = vbParamSymbol != null ? CommonConversions.GetTypeSyntax(vbParamSymbol.Type)
                : await SyntaxOnlyConvertParamAsync(node);
        }

        var attributes = (await node.AttributeLists.SelectManyAsync(CommonConversions.ConvertAttributeAsync)).ToList();
        var modifiers = CommonConversions.ConvertModifiers(node, node.Modifiers, TokenContext.Local);
        var vbSymbol = _semanticModel.GetDeclaredSymbol(node) as IParameterSymbol;
        var baseParameters = vbSymbol?.ContainingSymbol.OriginalDefinition.GetBaseSymbol().GetParameters();
        var baseParameter = baseParameters?[vbSymbol.Ordinal];

        var csRefKind = CommonConversions.GetCsRefKind(baseParameter ?? vbSymbol, node);
        if (csRefKind == RefKind.Out) {
            modifiers = SyntaxFactory.TokenList(modifiers
                .Where(m => !m.IsKind(SyntaxKind.RefKeyword))
                .Concat(SyntaxFactory.Token(SyntaxKind.OutKeyword).Yield())
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
                        .WithTrailingTrivia(SyntaxFactory.ParseTrailingTrivia($"/* {defaultValue} */"));
                    var dateTimeArg = CommonConversions.CreateAttributeArgumentList(SyntaxFactory.AttributeArgument(dateTimeAsLongCsLiteral));
                    _extraUsingDirectives.Add("System.Runtime.InteropServices");
                    _extraUsingDirectives.Add("System.Runtime.CompilerServices");
                    var optionalDateTimeAttributes = new[] {
                        SyntaxFactory.Attribute(ValidSyntaxFactory.IdentifierName("Optional")),
                        SyntaxFactory.Attribute(ValidSyntaxFactory.IdentifierName("DateTimeConstant"), dateTimeArg)
                    };
                    attributes.Insert(0,
                        SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(optionalDateTimeAttributes)));
                }
            } else if (node.Modifiers.Any(m => m.IsKind(VBasic.SyntaxKind.ByRefKeyword)) || HasRefParametersAfterThisOne()) {
                var defaultExpression = await node.Default.Value.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
                var arg = CommonConversions.CreateAttributeArgumentList(SyntaxFactory.AttributeArgument(defaultExpression));
                _extraUsingDirectives.Add("System.Runtime.InteropServices");
                _extraUsingDirectives.Add("System.Runtime.CompilerServices");
                var optionalAttributes = new List<AttributeSyntax> {
                    SyntaxFactory.Attribute(ValidSyntaxFactory.IdentifierName("Optional")),
                };
                if (!node.Default.Value.IsKind(VBasic.SyntaxKind.NothingLiteralExpression)) {
                    optionalAttributes.Add(SyntaxFactory.Attribute(ValidSyntaxFactory.IdentifierName("DefaultParameterValue"), arg));
                }
                attributes.Insert(0,
                    SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(optionalAttributes)));
            } else {
                @default = SyntaxFactory.EqualsValueClause(
                    await node.Default.Value.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor));
            }
        }

        if (node.Parent.Parent is VBSyntax.MethodStatementSyntax mss
            && mss.AttributeLists.Any(CommonConversions.HasExtensionAttribute) && node.Parent.ChildNodes().First() == node &&
            vbSymbol.ValidCSharpExtensionMethodParameter()) {
            modifiers = modifiers.Insert(0, SyntaxFactory.Token(SyntaxKind.ThisKeyword));
        }
        return SyntaxFactory.Parameter(
            SyntaxFactory.List(attributes),
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
                              ?? SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword));

        var rankSpecifiers = await CommonConversions.ConvertArrayRankSpecifierSyntaxesAsync(node.Identifier.ArrayRankSpecifiers, node.Identifier.ArrayBounds, false);
        if (rankSpecifiers.Any()) {
            syntaxParamType = SyntaxFactory.ArrayType(syntaxParamType, rankSpecifiers);
        }

        if (!node.Identifier.Nullable.IsKind(SyntaxKind.None)) {
            var arrayType = syntaxParamType as ArrayTypeSyntax;
            if (arrayType == null) {
                syntaxParamType = SyntaxFactory.NullableType(syntaxParamType);
            } else {
                syntaxParamType = arrayType.WithElementType(SyntaxFactory.NullableType(arrayType.ElementType));
            }
        }
        return syntaxParamType;
    }

    public override async Task<CSharpSyntaxNode> VisitAttribute(VBSyntax.AttributeSyntax node)
    {
        return SyntaxFactory.AttributeList(
            node.Target == null ? null : SyntaxFactory.AttributeTargetSpecifier(node.Target.AttributeModifier.ConvertToken()),
            SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(await node.Name.AcceptAsync<NameSyntax>(TriviaConvertingExpressionVisitor), await node.ArgumentList.AcceptAsync<AttributeArgumentListSyntax>(TriviaConvertingExpressionVisitor)))
        );
    }

    public override async Task<CSharpSyntaxNode> VisitTupleType(VBasic.Syntax.TupleTypeSyntax node)
    {
        var elements = await node.Elements.SelectAsync(async e => await e.AcceptAsync<TupleElementSyntax>(TriviaConvertingExpressionVisitor));
        return SyntaxFactory.TupleType(SyntaxFactory.SeparatedList(elements));
    }

    public override async Task<CSharpSyntaxNode> VisitTypedTupleElement(VBasic.Syntax.TypedTupleElementSyntax node)
    {
        return SyntaxFactory.TupleElement(await node.Type.AcceptAsync<TypeSyntax>(TriviaConvertingExpressionVisitor));
    }

    public override async Task<CSharpSyntaxNode> VisitNamedTupleElement(VBasic.Syntax.NamedTupleElementSyntax node)
    {
        return SyntaxFactory.TupleElement(await node.AsClause.Type.AcceptAsync<TypeSyntax>(TriviaConvertingExpressionVisitor), CommonConversions.ConvertIdentifier(node.Identifier));
    }

    public override async Task<CSharpSyntaxNode> VisitTupleExpression(VBasic.Syntax.TupleExpressionSyntax node)
    {
        var args = await node.Arguments.SelectAsync(async a => {
            var expr = await a.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
            return SyntaxFactory.Argument(expr);
        });
        return SyntaxFactory.TupleExpression(SyntaxFactory.SeparatedList(args));
    }

    public override async Task<CSharpSyntaxNode> VisitPredefinedType(VBasic.Syntax.PredefinedTypeSyntax node)
    {
        if (node.Keyword.IsKind(VBasic.SyntaxKind.DateKeyword)) {
            return ValidSyntaxFactory.IdentifierName(nameof(DateTime));
        }
        return SyntaxFactory.PredefinedType(node.Keyword.ConvertToken());
    }

    public override async Task<CSharpSyntaxNode> VisitNullableType(VBasic.Syntax.NullableTypeSyntax node)
    {
        return SyntaxFactory.NullableType(await node.ElementType.AcceptAsync<TypeSyntax>(TriviaConvertingExpressionVisitor));
    }

    public override async Task<CSharpSyntaxNode> VisitArrayType(VBasic.Syntax.ArrayTypeSyntax node)
    {
        var ranks = await node.RankSpecifiers.SelectAsync(async r => await r.AcceptAsync<ArrayRankSpecifierSyntax>(TriviaConvertingExpressionVisitor));
        return SyntaxFactory.ArrayType(await node.ElementType.AcceptAsync<TypeSyntax>(TriviaConvertingExpressionVisitor), SyntaxFactory.List(ranks));
    }

    public override async Task<CSharpSyntaxNode> VisitArrayRankSpecifier(VBasic.Syntax.ArrayRankSpecifierSyntax node)
    {
        return SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SeparatedList(Enumerable.Repeat<ExpressionSyntax>(SyntaxFactory.OmittedArraySizeExpression(), node.Rank)));
    }


    public override async Task<CSharpSyntaxNode> VisitTypeArgumentList(VBasic.Syntax.TypeArgumentListSyntax node)
    {
        var args = await node.Arguments.SelectAsync(async a => await a.AcceptAsync<TypeSyntax>(TriviaConvertingExpressionVisitor));
        return SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(args));
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
            SyntaxFactory.InvocationExpression(toInvoke,
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(argExpression)))
            );
    }

    private SyntaxToken ConvertIdentifier(SyntaxToken identifierIdentifier, bool isAttribute = false)
    {
        return CommonConversions.ConvertIdentifier(identifierIdentifier, isAttribute);
    }
}