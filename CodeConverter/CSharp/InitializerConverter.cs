using Microsoft.CodeAnalysis.Operations;

namespace ICSharpCode.CodeConverter.CSharp;

internal class InitializerConverter
{
    private readonly SemanticModel _semanticModel;
    private readonly Dictionary<string, Stack<(SyntaxNode Scope, string TempName)>> _tempNameForAnonymousScope;
    private readonly HashSet<string> _generatedNames;
    public CommonConversions CommonConversions { get; }

    public InitializerConverter(SemanticModel semanticModel, CommonConversions commonConversions, HashSet<string> generatedNames,
        Dictionary<string, Stack<(SyntaxNode Scope, string TempName)>> tempNameForAnonymousScope)
    {
        CommonConversions = commonConversions;
        _semanticModel = semanticModel;
        _generatedNames = generatedNames;
        _tempNameForAnonymousScope = tempNameForAnonymousScope;
    }

    public async Task<CSharpSyntaxNode> ConvertInferredFieldInitializerAsync(VBasic.Syntax.InferredFieldInitializerSyntax node)
    {
        return CS.SyntaxFactory.AnonymousObjectMemberDeclarator(await node.Expression.AcceptAsync<CSSyntax.ExpressionSyntax>(CommonConversions.TriviaConvertingExpressionVisitor));
    }

    /// <remarks>Collection initialization has many variants in both VB and C#. Please add especially many test cases when touching this.</remarks>
    public async Task<CSharpSyntaxNode> ConvertCollectionInitializerAsync(VBasic.Syntax.CollectionInitializerSyntax node)
    {
        var isExplicitCollectionInitializer = node.Parent is VBasic.Syntax.ObjectCollectionInitializerSyntax
                                              || node.Parent is VBasic.Syntax.CollectionInitializerSyntax
                                              || node.Parent is VBasic.Syntax.ArrayCreationExpressionSyntax;
        var initializerKind = node.IsParentKind(VBasic.SyntaxKind.ObjectCollectionInitializer) || node.IsParentKind(VBasic.SyntaxKind.ObjectCreationExpression) ?
            CS.SyntaxKind.CollectionInitializerExpression :
            node.IsParentKind(VBasic.SyntaxKind.CollectionInitializer) && IsComplexInitializer(node) ? CS.SyntaxKind.ComplexElementInitializerExpression :
                CS.SyntaxKind.ArrayInitializerExpression;
        var initializers = (await node.Initializers.SelectAsync(async i => {
            var convertedInitializer = await i.AcceptAsync<CSSyntax.ExpressionSyntax>(CommonConversions.TriviaConvertingExpressionVisitor);
            return CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(i, convertedInitializer, false);
        }));
        var initializer = CS.SyntaxFactory.InitializerExpression(initializerKind, CS.SyntaxFactory.SeparatedList(initializers));
        if (isExplicitCollectionInitializer) return initializer;

        var convertedType = _semanticModel.GetTypeInfo(node).ConvertedType;
        var dimensions = convertedType is IArrayTypeSymbol ats ? ats.Rank : 1; // For multidimensional array [,] note these are different from nested arrays [][]
        if (!(convertedType.GetEnumerableElementTypeOrDefault() is {} elementType)) return CS.SyntaxFactory.ImplicitArrayCreationExpression(initializer);
            
        if (!initializers.Any() && dimensions == 1) {
            var arrayTypeArgs = CS.SyntaxFactory.TypeArgumentList(CS.SyntaxFactory.SingletonSeparatedList(CommonConversions.GetTypeSyntax(elementType)));
            var arrayEmpty = CS.SyntaxFactory.MemberAccessExpression(CS.SyntaxKind.SimpleMemberAccessExpression,
                ValidSyntaxFactory.IdentifierName(nameof(Array)), CS.SyntaxFactory.GenericName(nameof(Array.Empty)).WithTypeArgumentList(arrayTypeArgs));
            return CS.SyntaxFactory.InvocationExpression(arrayEmpty);
        }

        bool hasExpressionToInferTypeFrom = node.Initializers.SelectMany(n => n.DescendantNodesAndSelf()).Any(n => n is not VBasic.Syntax.CollectionInitializerSyntax);
        if (hasExpressionToInferTypeFrom) {
            var commas = Enumerable.Repeat(CS.SyntaxFactory.Token(CS.SyntaxKind.CommaToken), dimensions - 1);
            return CS.SyntaxFactory.ImplicitArrayCreationExpression(CS.SyntaxFactory.TokenList(commas), initializer);
        }

        var arrayType = (CSSyntax.ArrayTypeSyntax)CommonConversions.CsSyntaxGenerator.ArrayTypeExpression(CommonConversions.GetTypeSyntax(elementType));
        var sizes = Enumerable.Repeat<CSSyntax.ExpressionSyntax>(CS.SyntaxFactory.OmittedArraySizeExpression(), dimensions);
        var arrayRankSpecifierSyntax = CS.SyntaxFactory.SingletonList(CS.SyntaxFactory.ArrayRankSpecifier(CS.SyntaxFactory.SeparatedList(sizes)));
        arrayType = arrayType.WithRankSpecifiers(arrayRankSpecifierSyntax);
        return CS.SyntaxFactory.ArrayCreationExpression(arrayType, initializer);
    }

    public async Task<CSharpSyntaxNode> ConvertObjectMemberInitializerAsync(VBasic.Syntax.ObjectMemberInitializerSyntax node)
    {
        var initializers = await node.Initializers.AcceptSeparatedListAsync<VBSyntax.FieldInitializerSyntax, CSSyntax.ExpressionSyntax>(CommonConversions.TriviaConvertingExpressionVisitor);
        return CS.SyntaxFactory.InitializerExpression(CS.SyntaxKind.ObjectInitializerExpression, initializers);
    }

    public async Task<CSharpSyntaxNode> ConvertNamedFieldInitializerAsync(VBasic.Syntax.NamedFieldInitializerSyntax node)
    {
        var csExpressionSyntax = await node.Expression.AcceptAsync<CSSyntax.ExpressionSyntax>(CommonConversions.TriviaConvertingExpressionVisitor);
        csExpressionSyntax = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Expression, csExpressionSyntax);
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

            var anonymousObjectMemberDeclaratorSyntax = CS.SyntaxFactory.AnonymousObjectMemberDeclarator(
                CS.SyntaxFactory.NameEquals(CS.SyntaxFactory.IdentifierName(CommonConversions.ConvertIdentifier(node.Name.Identifier))),
                csExpressionSyntax);
            return anonymousObjectMemberDeclaratorSyntax;
        }

        return CS.SyntaxFactory.AssignmentExpression(CS.SyntaxKind.SimpleAssignmentExpression,
            await node.Name.AcceptAsync<CSSyntax.ExpressionSyntax>(CommonConversions.TriviaConvertingExpressionVisitor),
            csExpressionSyntax
        );
    }

    public async Task<CSharpSyntaxNode> ConvertObjectCollectionInitializerAsync(VBasic.Syntax.ObjectCollectionInitializerSyntax node)
    {
        return await node.Initializer.AcceptAsync<CSharpSyntaxNode>(CommonConversions.TriviaConvertingExpressionVisitor); //Dictionary initializer comes through here despite the FROM keyword not being in the source code
    }

    private bool IsComplexInitializer(VBSyntax.CollectionInitializerSyntax node)
    {
        return _semanticModel.GetOperation(node.Parent.Parent) is IObjectOrCollectionInitializerOperation initializer &&
               initializer.Initializers.OfType<IInvocationOperation>().Any();
    }

    private string GenerateUniqueVariableName(VisualBasicSyntaxNode existingNode, string varNameBase) => NameGenerator.CS.GetUniqueVariableNameInScope(_semanticModel, _generatedNames, existingNode, varNameBase);

    private static CSSyntax.ExpressionSyntax DeclareVariableInline(CSSyntax.ExpressionSyntax csExpressionSyntax, string temporaryName)
    {
        var temporaryNameId = CS.SyntaxFactory.Identifier(temporaryName);
        var temporaryNameExpression = ValidSyntaxFactory.IdentifierName(temporaryNameId);
        csExpressionSyntax = CS.SyntaxFactory.ConditionalExpression(
            CS.SyntaxFactory.IsPatternExpression(
                csExpressionSyntax,
                CS.SyntaxFactory.VarPattern(
                    CS.SyntaxFactory.SingleVariableDesignation(temporaryNameId))),
            temporaryNameExpression,
            CS.SyntaxFactory.LiteralExpression(
                CS.SyntaxKind.DefaultLiteralExpression,
                CS.SyntaxFactory.Token(CS.SyntaxKind.DefaultKeyword)));
        return csExpressionSyntax;
    }
}