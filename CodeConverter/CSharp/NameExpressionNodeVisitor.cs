using System.Data;
using System.Globalization;
using ICSharpCode.CodeConverter.CSharp.Replacements;
using ICSharpCode.CodeConverter.Util.FromRoslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.VisualBasic.CompilerServices;
using static ICSharpCode.CodeConverter.CSharp.SemanticModelExtensions;

namespace ICSharpCode.CodeConverter.CSharp;


internal class NameExpressionNodeVisitor
{
    private readonly SemanticModel _semanticModel;
    private readonly HashSet<string> _generatedNames;
    private readonly ITypeContext _typeContext;
    private readonly HashSet<string> _extraUsingDirectives;
    private readonly Dictionary<string, Stack<(SyntaxNode Scope, string TempName)>> _tempNameForAnonymousScope;
    private readonly Stack<ExpressionSyntax> _withBlockLhs;
    private readonly ArgumentConverter _argumentConverter;

    public CommonConversions CommonConversions { get; }
    public CommentConvertingVisitorWrapper TriviaConvertingExpressionVisitor { get; }

    public NameExpressionNodeVisitor(SemanticModel semanticModel, HashSet<string> generatedNames, ITypeContext typeContext, HashSet<string> extraUsingDirectives,
        Dictionary<string, Stack<(SyntaxNode Scope, string TempName)>> tempNameForAnonymousScope, Stack<ExpressionSyntax> withBlockLhs, CommonConversions commonConversions,
        ArgumentConverter argumentConverter,
        CommentConvertingVisitorWrapper triviaConvertingExpressionVisitor)
    {
        _semanticModel = semanticModel;
        _generatedNames = generatedNames;
        _typeContext = typeContext;
        _extraUsingDirectives = extraUsingDirectives;
        _tempNameForAnonymousScope = tempNameForAnonymousScope;
        _withBlockLhs = withBlockLhs;
        _argumentConverter = argumentConverter;
        CommonConversions = commonConversions;
        TriviaConvertingExpressionVisitor = triviaConvertingExpressionVisitor;
    }

    public async Task<CSharpSyntaxNode> ConvertMemberAccessExpressionAsync(VBasic.Syntax.MemberAccessExpressionSyntax node)
    {
        var nodeSymbol = _semanticModel.GetSymbolInfoInDocument<ISymbol>(node.Name);

        if (!node.IsParentKind(VBasic.SyntaxKind.InvocationExpression) &&
            SimpleMethodReplacement.TryGet(nodeSymbol, out var methodReplacement) &&
            methodReplacement.ReplaceIfMatches(nodeSymbol, Array.Empty<ArgumentSyntax>(), node.IsParentKind(VBasic.SyntaxKind.AddressOfExpression)) is { } replacement) {
            return replacement;
        }

        var simpleNameSyntax = await node.Name.AcceptAsync<SimpleNameSyntax>(TriviaConvertingExpressionVisitor);

        var isDefaultProperty = nodeSymbol is IPropertySymbol p && VBasic.VisualBasicExtensions.IsDefault(p) && p.Parameters.Any();
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
                            $"MyClass{CommonConversions.ConvertIdentifier(node.Name.Identifier).ValueText}");
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
            if (left != null && _semanticModel.GetSymbolInfo(node) is { CandidateReason: CandidateReason.LateBound, CandidateSymbols.Length: 0 }
                             && _semanticModel.GetSymbolInfo(node.Expression).Symbol is { Kind: var expressionSymbolKind }
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

    public async Task<CSharpSyntaxNode> ConvertGlobalNameAsync(VBasic.Syntax.GlobalNameSyntax node)
    {
        return ValidSyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword));
    }

    public async Task<CSharpSyntaxNode> ConvertMeExpressionAsync(VBasic.Syntax.MeExpressionSyntax node)
    {
        return SyntaxFactory.ThisExpression();
    }

    public async Task<CSharpSyntaxNode> ConvertMyBaseExpressionAsync(VBasic.Syntax.MyBaseExpressionSyntax node)
    {
        return SyntaxFactory.BaseExpression();
    }

    public async Task<CSharpSyntaxNode> ConvertGenericNameAsync(VBasic.Syntax.GenericNameSyntax node)
    {
        var symbol = _semanticModel.GetSymbolInfoInDocument<ISymbol>(node);
        var genericNameSyntax = await GenericNameAccountingForReducedParametersAsync(node, symbol);
        return await AdjustForImplicitInvocationAsync(node, genericNameSyntax);
    }
    public async Task<CSharpSyntaxNode> ConvertQualifiedNameAsync(VBasic.Syntax.QualifiedNameSyntax node)
    {
        var symbol = _semanticModel.GetSymbolInfoInDocument<ITypeSymbol>(node);
        if (symbol != null) {
            return CommonConversions.GetTypeSyntax(symbol.GetSymbolType());
        }
        var lhsSyntax = await node.Left.AcceptAsync<NameSyntax>(TriviaConvertingExpressionVisitor);
        var rhsSyntax = await node.Right.AcceptAsync<SimpleNameSyntax>(TriviaConvertingExpressionVisitor);

        VBasic.Syntax.NameSyntax topLevelName = node;
        while (topLevelName.Parent is VBasic.Syntax.NameSyntax parentName) {
            topLevelName = parentName;
        }
        var partOfNamespaceDeclaration = topLevelName.Parent.IsKind(VBasic.SyntaxKind.NamespaceStatement);
        var leftIsGlobal = node.Left.IsKind(VBasic.SyntaxKind.GlobalName);
        ExpressionSyntax qualifiedName;
        if (partOfNamespaceDeclaration || !(lhsSyntax is SimpleNameSyntax sns)) {
            if (leftIsGlobal) return rhsSyntax;
            qualifiedName = lhsSyntax;
        } else {
            qualifiedName = QualifyNode(node.Left, sns);
        }

        return leftIsGlobal ? SyntaxFactory.AliasQualifiedName((IdentifierNameSyntax)lhsSyntax, rhsSyntax) :
            SyntaxFactory.QualifiedName((NameSyntax)qualifiedName, rhsSyntax);
    }

    /// <remarks>PERF: This is a hot code path, try to avoid using things like GetOperation except where needed.</remarks>
    public async Task<CSharpSyntaxNode> ConvertIdentifierNameAsync(VBasic.Syntax.IdentifierNameSyntax node)
    {
        var identifier = SyntaxFactory.IdentifierName(CommonConversions.ConvertIdentifier(node.Identifier, node.GetAncestor<VBasic.Syntax.AttributeSyntax>() != null));

        bool requiresQualification = !node.Parent.IsKind(VBasic.SyntaxKind.SimpleMemberAccessExpression, VBasic.SyntaxKind.QualifiedName, VBasic.SyntaxKind.NameColonEquals, VBasic.SyntaxKind.ImportsStatement, VBasic.SyntaxKind.NamespaceStatement, VBasic.SyntaxKind.NamedFieldInitializer) ||
                                     node.Parent is VBSyntax.NamedFieldInitializerSyntax nfs && nfs.Expression == node ||
                                     node.Parent is VBasic.Syntax.MemberAccessExpressionSyntax maes && maes.Expression == node;
        var qualifiedIdentifier = requiresQualification
            ? QualifyNode(node, identifier) : identifier;

        var sym = _semanticModel.GetSymbolInfoInDocument<ISymbol>(node);
        if (sym is ILocalSymbol) {
            if (sym.IsStatic && sym.ContainingSymbol is IMethodSymbol m && m.AssociatedSymbol is IPropertySymbol) {
                qualifiedIdentifier = qualifiedIdentifier.WithParentPropertyAccessorKind(m.MethodKind);
            }

            var vbMethodBlock = node.Ancestors().OfType<VBasic.Syntax.MethodBlockBaseSyntax>().FirstOrDefault();
            if (vbMethodBlock != null &&
                vbMethodBlock.MustReturn() &&
                !node.Parent.IsKind(VBasic.SyntaxKind.NameOfExpression) &&
                node.Identifier.ValueText.Equals(CommonConversions.GetMethodBlockBaseIdentifierForImplicitReturn(vbMethodBlock).ValueText, StringComparison.OrdinalIgnoreCase)) {
                var retVar = CommonConversions.GetRetVariableNameOrNull(vbMethodBlock);
                if (retVar != null) {
                    return retVar;
                }
            }
        }

        return await AdjustForImplicitInvocationAsync(node, qualifiedIdentifier);
    }




    public async Task<CSharpSyntaxNode> ConvertInvocationExpressionAsync(
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
                var convertArgumentListOrEmptyAsync = await _argumentConverter.ConvertArgumentsAsync(node.ArgumentList);
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
        return await CommonConversions.WithRemovedRedundantConversionOrNullAsync(coercedConversionNode, conversionArg);
    }

    private async Task<ExpressionSyntax> ConvertInvocationAsync(VBSyntax.InvocationExpressionSyntax node, ISymbol invocationSymbol, ISymbol expressionSymbol)
    {
        var expressionType = _semanticModel.GetTypeInfo(node.Expression).Type;
        var expressionReturnType = expressionSymbol?.GetReturnType() ?? expressionType;
        var operation = _semanticModel.GetOperation(node);

        var expr = await node.Expression.AcceptAsync<CSharpSyntaxNode>(TriviaConvertingExpressionVisitor);
        if (await TryConvertParameterizedPropertyAsync(operation, node, expr, node.ArgumentList) is { } invocation) {
            return invocation;
        }
        //TODO: Decide if the above override should be subject to the rest of this method's adjustments (probably)


        // VB doesn't have a specialized node for element access because the syntax is ambiguous. Instead, it just uses an invocation expression or dictionary access expression, then figures out using the semantic model which one is most likely intended.
        // https://github.com/dotnet/roslyn/blob/master/src/Workspaces/VisualBasic/Portable/LanguageServices/VisualBasicSyntaxFactsService.vb#L768
        (var convertedExpression, bool shouldBeElementAccess) = await ConvertInvocationSubExpressionAsync(node, operation, expressionSymbol, expressionReturnType, expr);
        if (shouldBeElementAccess) {
            return await CreateElementAccessAsync(node, convertedExpression);
        }

        if (expressionSymbol != null && expressionSymbol.IsKind(SymbolKind.Property) &&
            invocationSymbol != null && invocationSymbol.GetParameters().Length == 0 && node.ArgumentList.Arguments.Count == 0) {
            return convertedExpression; //Parameterless property access
        }

        var convertedArgumentList = await _argumentConverter.ConvertArgumentListOrEmptyAsync(node, node.ArgumentList);

        if (IsElementAtOrDefaultInvocation(invocationSymbol, expressionSymbol)) {
            convertedExpression = GetElementAtOrDefaultExpression(expressionType, convertedExpression);
        }

        if (invocationSymbol.IsReducedExtension() && invocationSymbol is IMethodSymbol { ReducedFrom: { Parameters: var parameters } } &&
            !parameters.FirstOrDefault().ValidCSharpExtensionMethodParameter() &&
            node.Expression is VBSyntax.MemberAccessExpressionSyntax maes) {
            var thisArgExpression = await maes.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
            var thisArg = SyntaxFactory.Argument(thisArgExpression).WithRefKindKeyword(CommonConversions.GetRefToken(RefKind.Ref));
            convertedArgumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(convertedArgumentList.Arguments.Prepend(thisArg)));
            var containingType = (ExpressionSyntax)CommonConversions.CsSyntaxGenerator.TypeExpression(invocationSymbol.ContainingType);
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
            // Special case where structure changes due to conditional access (See ConvertMemberAccessExpression)
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

        // The Vb compiler interprets Datatable indexing as a AsEnumerable().ElementAtOrDefault() operation.
        if (expressionType.Name == nameof(DataTable)) {
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
        if (overrideIdentifier != null) {
            var expr = identifier;
            var idToken = expr.DescendantTokens().Last(t => t.IsKind(SyntaxKind.IdentifierToken));
            expr = ReplaceRightmostIdentifierText(expr, idToken, overrideIdentifier);

            var args = await _argumentConverter.ConvertArgumentListOrEmptyAsync(node, optionalArgumentList);
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


    /// <summary>
    /// The VB compiler actually just hoists the conditions within the same method, but that leads to the original logic looking very different.
    /// This should be equivalent but keep closer to the look of the original source code.
    /// See https://github.com/icsharpcode/CodeConverter/issues/310 and https://github.com/icsharpcode/CodeConverter/issues/324
    /// </summary>
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

        //any argument that's a ByRef parameter of the parent method needs to be passed as a ref parameter to the local function (to avoid error CS1628)
        var refParametersOfParent = GetRefParameters(invocation.ArgumentList);
        var (args, @params) = CreateArgumentsAndParametersLists(refParametersOfParent);

        var localFunc = _typeContext.PerScopeState.Hoist(new HoistedFunction(localFuncName, returnType, block, SyntaxFactory.ParameterList(@params)));
        return SyntaxFactory.InvocationExpression(localFunc.TempIdentifier, SyntaxFactory.ArgumentList(args));

        List<IParameterSymbol> GetRefParameters(VBSyntax.ArgumentListSyntax argumentList)
        {
            var result = new List<IParameterSymbol>();
            if (argumentList is null) return result;

            foreach (var arg in argumentList.Arguments) {
                if (_semanticModel.GetSymbolInfo(arg.GetExpression()).Symbol is not IParameterSymbol p) continue;
                if (p.RefKind != RefKind.None) {
                    result.Add(p);
                }
            }

            return result;
        }

        (SeparatedSyntaxList<ArgumentSyntax>, SeparatedSyntaxList<ParameterSyntax>) CreateArgumentsAndParametersLists(List<IParameterSymbol> parameterSymbols)
        {
            var arguments = new List<ArgumentSyntax>();
            var parameters = new List<ParameterSyntax>();
            foreach (var p in parameterSymbols) {
                var arg = (ArgumentSyntax)CommonConversions.CsSyntaxGenerator.Argument(p.RefKind, SyntaxFactory.IdentifierName(p.Name));
                arguments.Add(arg);
                var par = (ParameterSyntax)CommonConversions.CsSyntaxGenerator.ParameterDeclaration(p);
                parameters.Add(par.WithDefault(null));
            }
            return (SyntaxFactory.SeparatedList(arguments), SyntaxFactory.SeparatedList(parameters));
        }
    }

    private bool RequiresLocalFunction(VBSyntax.InvocationExpressionSyntax invocation, IMethodSymbol invocationSymbol)
    {
        if (invocation.ArgumentList == null) return false;
        var definitelyExecutedAfterPrevious = DefinitelyExecutedAfterPreviousStatement(invocation);
        var nextStatementDefinitelyExecuted = NextStatementDefinitelyExecutedAfter(invocation);
        if (definitelyExecutedAfterPrevious && nextStatementDefinitelyExecuted) return false;
        var possibleInline = definitelyExecutedAfterPrevious ? RefConversion.PreAssigment : RefConversion.Inline;
        return invocation.ArgumentList.Arguments.Any(a => RequiresLocalFunction(possibleInline, invocation, invocationSymbol, a));

        bool RequiresLocalFunction(RefConversion possibleInline, VBSyntax.InvocationExpressionSyntax invocation, IMethodSymbol invocationSymbol, VBSyntax.ArgumentSyntax a)
        {
            var refConversion = CommonConversions.GetRefConversionType(a, invocation.ArgumentList, invocationSymbol.Parameters, out string _, out _);
            if (RefConversion.Inline == refConversion || possibleInline == refConversion) return false;
            if (!(a is VBSyntax.SimpleArgumentSyntax sas)) return false;
            var argExpression = sas.Expression.SkipIntoParens();
            if (argExpression is VBSyntax.InstanceExpressionSyntax) return false;
            return !_semanticModel.GetConstantValue(argExpression).HasValue;
        }
    }

    /// <summary>
    /// Conservative version of _semanticModel.AnalyzeControlFlow(invocation).ExitPoints to account for exceptions
    /// </summary>
    private bool DefinitelyExecutedAfterPreviousStatement(VBSyntax.InvocationExpressionSyntax invocation)
    {
        SyntaxNode parent = invocation;
        while (true) {
            parent = parent.Parent;
            switch (parent) {
                case VBSyntax.ParenthesizedExpressionSyntax _:
                    continue;
                case VBSyntax.BinaryExpressionSyntax binaryExpression:
                    if (binaryExpression.Left == invocation) continue;
                    else return false;
                case VBSyntax.ArgumentSyntax argumentSyntax:
                    // Being the leftmost invocation of an unqualified method call ensures no other code is executed. Could add other cases here, such as a method call on a local variable name, or "this.". A method call on a property is not acceptable.
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
            VBSyntax.MemberAccessExpressionSyntax maes => maes.Expression is { } exp && !MayThrow(exp),
            _ => true
        };

    /// <summary>
    /// Safe overapproximation of whether an expression may throw.
    /// </summary>
    private bool MayThrow(VBSyntax.ExpressionSyntax expression)
    {
        expression = expression.SkipIntoParens();
        if (expression is VBSyntax.InstanceExpressionSyntax) return false;
        var symbol = _semanticModel.GetSymbolInfo(expression).Symbol;
        return !symbol.IsKind(SymbolKind.Local) && !symbol.IsKind(SymbolKind.Field);
    }

    /// <summary>
    /// Conservative version of _semanticModel.AnalyzeControlFlow(invocation).ExitPoints to account for exceptions
    /// </summary>
    private static bool NextStatementDefinitelyExecutedAfter(VBSyntax.InvocationExpressionSyntax invocation)
    {
        SyntaxNode parent = invocation;
        while (true) {
            parent = parent.Parent;
            switch (parent) {
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


    /// <summary>
    /// The pre-expansion phase <see cref="DocumentExtensions.WithExpandedRootAsync(Document, System.Threading.CancellationToken)"/> should handle this for compiling nodes.
    /// This is mainly targeted at dealing with missing semantic info.
    /// </summary>
    /// <returns></returns>
    private ExpressionSyntax QualifyNode(SyntaxNode node, SimpleNameSyntax left)
    {
        var nodeSymbolInfo = _semanticModel.GetSymbolInfoInDocument<ISymbol>(node);
        if (left != null &&
            nodeSymbolInfo != null &&
            nodeSymbolInfo.MatchesKind(SymbolKind.TypeParameter) == false &&
            nodeSymbolInfo.ContainingSymbol is INamespaceOrTypeSymbol containingSymbol &&
            !ContextImplicitlyQualfiesSymbol(node, containingSymbol)) {

            if (containingSymbol is ITypeSymbol containingTypeSymbol &&
                !nodeSymbolInfo.IsConstructor() /* Constructors are implicitly qualified with their type */) {
                // Qualify with a type to handle VB's type promotion https://docs.microsoft.com/en-us/dotnet/visual-basic/programming-guide/language-features/declared-elements/type-promotion
                var qualification =
                    CommonConversions.GetTypeSyntax(containingTypeSymbol);
                return Qualify(qualification.ToString(), left);
            }

            if (nodeSymbolInfo.IsNamespace()) {
                // Turn partial namespace qualification into full namespace qualification
                var qualification =
                    containingSymbol.ToCSharpDisplayString();
                return Qualify(qualification, left);
            }
        }

        return left;
    }

    private async Task<CSharpSyntaxNode> AdjustForImplicitInvocationAsync(SyntaxNode node, ExpressionSyntax qualifiedIdentifier)
    {
        //PERF: Avoid calling expensive GetOperation when it's easy
        bool nonExecutableNode = node.IsParentKind(VBasic.SyntaxKind.QualifiedName);
        if (nonExecutableNode || _semanticModel.SyntaxTree != node.SyntaxTree) return qualifiedIdentifier;

        if (await TryConvertParameterizedPropertyAsync(_semanticModel.GetOperation(node), node, qualifiedIdentifier) is { }
                invocation) {
            return invocation;
        }

        return AddEmptyArgumentListIfImplicit(node, qualifiedIdentifier);
    }


    /// <summary>
    /// Adjusts for Visual Basic's omission of type arguments that can be inferred in reduced generic method invocations
    /// The upfront WithExpandedRootAsync pass should ensure this only happens on broken syntax trees.
    /// In those cases, just comment the errant information. It would only cause a compiling change in behaviour if it can be inferred, was not set to the inferred value, and was reflected upon within the method body
    /// </summary>
    private async Task<SimpleNameSyntax> GenericNameAccountingForReducedParametersAsync(VBSyntax.GenericNameSyntax node, ISymbol symbol)
    {
        SyntaxToken convertedIdentifier = CommonConversions.ConvertIdentifier(node.Identifier);
        if (symbol is IMethodSymbol vbMethod && vbMethod.IsReducedTypeParameterMethod()) {
            var allTypeArgs = GetOrNullAllTypeArgsIncludingInferred(vbMethod);
            if (allTypeArgs != null) {
                return (SimpleNameSyntax)CommonConversions.CsSyntaxGenerator.GenericName(convertedIdentifier.Text, allTypeArgs);
            }
            var commentedText = "/* " + (await ConvertTypeArgumentListAsync(node)).ToFullString() + " */";
            var error = SyntaxFactory.ParseLeadingTrivia($"#error Conversion error: Could not convert all type parameters, so they've been commented out. Inferred type may be different{Environment.NewLine}");
            var partialConversion = SyntaxFactory.Comment(commentedText);
            return ValidSyntaxFactory.IdentifierName(convertedIdentifier).WithPrependedLeadingTrivia(error).WithTrailingTrivia(partialConversion);
        }

        return SyntaxFactory.GenericName(convertedIdentifier, await ConvertTypeArgumentListAsync(node));
    }

    /// <remarks>TODO: Would be more robust to use <seealso cref="IMethodSymbol.GetTypeInferredDuringReduction"/></remarks>
    private ITypeSymbol[] GetOrNullAllTypeArgsIncludingInferred(IMethodSymbol vbMethod)
    {
        if (!(CommonConversions.GetCsOriginalSymbolOrNull(vbMethod) is IMethodSymbol
                csSymbolWithInferredTypeParametersSet)) return null;
        var argSubstitutions = vbMethod.TypeParameters
            .Zip(vbMethod.TypeArguments, (parameter, arg) => (parameter, arg))
            .ToDictionary(x => x.parameter.Name, x => x.arg);
        var allTypeArgs = csSymbolWithInferredTypeParametersSet.GetTypeArguments()
            .Select(a => a.Kind == SymbolKind.TypeParameter && argSubstitutions.TryGetValue(a.Name, out var t) ? t : a)
            .ToArray();
        return allTypeArgs;
    }

    private async Task<TypeArgumentListSyntax> ConvertTypeArgumentListAsync(VBSyntax.GenericNameSyntax node)
    {
        return await node.TypeArgumentList.AcceptAsync<TypeArgumentListSyntax>(TriviaConvertingExpressionVisitor);
    }

    private CSharpSyntaxNode AddEmptyArgumentListIfImplicit(SyntaxNode node, ExpressionSyntax id)
    {
        if (_semanticModel.SyntaxTree != node.SyntaxTree) return id;
        return _semanticModel.GetOperation(node) switch {
            IInvocationOperation invocation => SyntaxFactory.InvocationExpression(id, _argumentConverter.CreateArgList(invocation.TargetMethod)),
            IPropertyReferenceOperation propReference when propReference.Property.Parameters.Any() => SyntaxFactory.InvocationExpression(id, _argumentConverter.CreateArgList(propReference.Property)),
            _ => id
        };
    }

    private bool ContextImplicitlyQualfiesSymbol(SyntaxNode syntaxNodeContext, INamespaceOrTypeSymbol symbolToCheck)
    {
        return symbolToCheck is INamespaceSymbol ns && ns.IsGlobalNamespace ||
               EnclosingTypeImplicitlyQualifiesSymbol(syntaxNodeContext, symbolToCheck);
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
            methodReplacement.ReplaceIfMatches(symbol, await _argumentConverter.ConvertArgumentsAsync(node.ArgumentList), false) is { } csExpression) {
            cSharpSyntaxNode = csExpression;
        }

        return cSharpSyntaxNode;
    }


    private static bool IsVisualBasicChrMethod(ISymbol symbol) =>
        symbol is not null
        && symbol.ContainingNamespace.MetadataName == nameof(Microsoft.VisualBasic)
        && (symbol.Name == "ChrW" || symbol.Name == "Chr");

    /// <summary>
    /// https://github.com/icsharpcode/CodeConverter/issues/745
    /// </summary>
    private static bool IsCultureInvariant(Optional<object> constValue) =>
        constValue.HasValue && Convert.ToUInt64(constValue.Value, CultureInfo.InvariantCulture) <= 127;

    private bool EnclosingTypeImplicitlyQualifiesSymbol(SyntaxNode syntaxNodeContext, INamespaceOrTypeSymbol symbolToCheck)
    {
        ISymbol typeContext = syntaxNodeContext.GetEnclosingDeclaredTypeSymbol(_semanticModel);
        var implicitCsQualifications = ((ITypeSymbol)typeContext).GetBaseTypesAndThis()
            .Concat(typeContext.FollowProperty(n => n.ContainingSymbol))
            .ToList();

        return implicitCsQualifications.Contains(symbolToCheck);
    }

    private static QualifiedNameSyntax Qualify(string qualification, ExpressionSyntax toBeQualified)
    {
        return SyntaxFactory.QualifiedName(
            SyntaxFactory.ParseName(qualification),
            (SimpleNameSyntax)toBeQualified);
    }

    private static bool IsSubPartOfConditionalAccess(VBasic.Syntax.MemberAccessExpressionSyntax node)
    {
        static bool IsMemberAccessChain(SyntaxNode exp) =>
            exp?.IsKind(VBasic.SyntaxKind.InvocationExpression,
                VBasic.SyntaxKind.SimpleMemberAccessExpression,
                VBasic.SyntaxKind.ParenthesizedExpression,
                VBasic.SyntaxKind.ConditionalAccessExpression) == true;

        for (SyntaxNode child = node, parent = node.Parent; IsMemberAccessChain(parent); child = parent, parent = parent.Parent) {
            if (parent is VBSyntax.ConditionalAccessExpressionSyntax cae && cae.WhenNotNull == child) {
                return true; // On right hand side of a ?. conditional access
            }
        }

        return false;
    }

    private static CSharpSyntaxNode ReplaceRightmostIdentifierText(CSharpSyntaxNode expr, SyntaxToken idToken, string overrideIdentifier)
    {
        return expr.ReplaceToken(idToken, SyntaxFactory.Identifier(overrideIdentifier).WithTriviaFrom(idToken).WithAdditionalAnnotations(idToken.GetAnnotations()));
    }


    /// <summary>
    /// If there's a single numeric arg, let's assume it's an indexer (probably an array).
    /// Otherwise, err on the side of a method call.
    /// </summary>
    private bool ProbablyNotAMethodCall(VBasic.Syntax.InvocationExpressionSyntax node, ISymbol symbol, ITypeSymbol symbolReturnType)
    {
        return !node.IsParentKind(VBasic.SyntaxKind.CallStatement) && !(symbol is IMethodSymbol) &&
               symbolReturnType.IsErrorType() && node.Expression is VBasic.Syntax.IdentifierNameSyntax &&
               node.ArgumentList?.Arguments.OnlyOrDefault()?.GetExpression() is { } arg &&
               _semanticModel.GetTypeInfo(arg).Type.IsNumericType();
    }

}
