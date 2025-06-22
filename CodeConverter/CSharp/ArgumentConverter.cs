using Microsoft.VisualBasic;

namespace ICSharpCode.CodeConverter.CSharp;

internal class ArgumentConverter
{
    public CommonConversions CommonConversions { get; }
    private readonly VisualBasicEqualityComparison _visualBasicEqualityComparison;
    private readonly ITypeContext _typeContext;
    private readonly SemanticModel _semanticModel;
    private CommentConvertingVisitorWrapper TriviaConvertingExpressionVisitor { get; }

    public ArgumentConverter(VisualBasicEqualityComparison visualBasicEqualityComparison, ITypeContext typeContext, SemanticModel semanticModel, CommonConversions commonConversions)
    {
        CommonConversions = commonConversions;
        _visualBasicEqualityComparison = visualBasicEqualityComparison;
        _typeContext = typeContext;
        _semanticModel = semanticModel;
        TriviaConvertingExpressionVisitor = commonConversions.TriviaConvertingExpressionVisitor;
    }

    public async Task<CSharpSyntaxNode> ConvertSimpleArgumentAsync(VBSyntax.SimpleArgumentSyntax node)
    {
        var argList = (VBasic.Syntax.ArgumentListSyntax)node.Parent;
        var invocation = argList.Parent;
        if (invocation is VBasic.Syntax.ArrayCreationExpressionSyntax)
            return await node.Expression.AcceptAsync<CSharpSyntaxNode>(TriviaConvertingExpressionVisitor);
        var symbol = GetInvocationSymbol(invocation);
        SyntaxToken token = default(SyntaxToken);
        var convertedArgExpression = (await node.Expression.AcceptAsync<CSSyntax.ExpressionSyntax>(TriviaConvertingExpressionVisitor)).SkipIntoParens();
        var typeConversionAnalyzer = CommonConversions.TypeConversionAnalyzer;
        var baseSymbol = symbol?.OriginalDefinition.GetBaseSymbol();
        var possibleParameters = (CommonConversions.GetCsOriginalSymbolOrNull(baseSymbol) ?? symbol)?.GetParameters();
        if (possibleParameters.HasValue) {
            var refType = _semanticModel.GetRefConversionType(node, argList, possibleParameters.Value, out var argName, out var refKind);
            token = CommonConversions.GetRefToken(refKind);
            if (refType != SemanticModelExtensions.RefConversion.Inline) {
                convertedArgExpression = HoistByRefDeclaration(node, convertedArgExpression, refType, argName, refKind);
            } else {
                convertedArgExpression = typeConversionAnalyzer.AddExplicitConversion(node.Expression, convertedArgExpression, defaultToCast: refKind != RefKind.None);
            }
        } else {
            convertedArgExpression = typeConversionAnalyzer.AddExplicitConversion(node.Expression, convertedArgExpression);
        }

        var nameColon = node.IsNamed ? CS.SyntaxFactory.NameColon(await node.NameColonEquals.Name.AcceptAsync<CSSyntax.IdentifierNameSyntax>(TriviaConvertingExpressionVisitor)) : null;
        return CS.SyntaxFactory.Argument(nameColon, token, convertedArgExpression);
    }

    public async Task<IEnumerable<CSSyntax.ArgumentSyntax>> ConvertArgumentsAsync(VBasic.Syntax.ArgumentListSyntax node)
    {
        ISymbol invocationSymbol = GetInvocationSymbol(node.Parent);
        var forceNamedParameters = false;
        var invocationHasOverloads = invocationSymbol.HasOverloads();

        var processedParameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var argumentSyntaxs = (await node.Arguments.SelectAsync(ConvertArg)).Where(a => a != null);
        return Enumerable.Concat(argumentSyntaxs, GetAdditionalRequiredArgs(node.Arguments, processedParameters, invocationSymbol, invocationHasOverloads));

        async Task<CSSyntax.ArgumentSyntax> ConvertArg(VBSyntax.ArgumentSyntax arg, int argIndex)
        {
            var argName = arg is VBSyntax.SimpleArgumentSyntax { IsNamed: true } namedArg ? namedArg.NameColonEquals.Name.Identifier.Text : null;
            var parameterSymbol = invocationSymbol?.GetParameters().GetArgument(argName, argIndex);
            var convertedArg = await ConvertArgForParameter(arg, parameterSymbol);

            if (convertedArg is not null && parameterSymbol is not null) {
                processedParameters.Add(parameterSymbol.Name);
            }

            return convertedArg;
        }

        async Task<CSSyntax.ArgumentSyntax> ConvertArgForParameter(VBSyntax.ArgumentSyntax arg, IParameterSymbol parameterSymbol)
        {
            if (arg.IsOmitted) {
                if (invocationSymbol != null && !invocationHasOverloads) {
                    forceNamedParameters = true;
                    return null; //Prefer to skip omitted and use named parameters when the symbol has only one overload
                }
                return ConvertOmittedArgument(parameterSymbol);
            }

            var argSyntax = await arg.AcceptAsync<CSSyntax.ArgumentSyntax>(TriviaConvertingExpressionVisitor);
            if (forceNamedParameters && !arg.IsNamed && parameterSymbol != null) {
                return argSyntax.WithNameColon(CS.SyntaxFactory.NameColon(CS.SyntaxFactory.IdentifierName(CommonConversions.CsEscapedIdentifier(parameterSymbol.Name))));
            }

            return argSyntax;
        }

        CSSyntax.ArgumentSyntax ConvertOmittedArgument(IParameterSymbol parameter)
        {
            if (parameter == null) {
                return CS.SyntaxFactory.Argument(CS.SyntaxFactory.LiteralExpression(CS.SyntaxKind.DefaultLiteralExpression));
            }

            var csRefKind = CommonConversions.GetCsRefKind(parameter);
            return csRefKind != RefKind.None
                ? CreateOptionalRefArg(parameter, csRefKind)
                : CS.SyntaxFactory.Argument(CommonConversions.Literal(parameter.ExplicitDefaultValue));
        }
    }

    public async Task<CSSyntax.AttributeArgumentSyntax> ToAttributeArgumentAsync(VBasic.Syntax.ArgumentSyntax arg)
    {
        if (!(arg is VBasic.Syntax.SimpleArgumentSyntax))
            throw new NotSupportedException();
        var a = (VBasic.Syntax.SimpleArgumentSyntax)arg;
        var attr = CS.SyntaxFactory.AttributeArgument(await a.Expression.AcceptAsync<CSSyntax.ExpressionSyntax>(TriviaConvertingExpressionVisitor));
        if (a.IsNamed) {
            attr = attr.WithNameEquals(CS.SyntaxFactory.NameEquals(await a.NameColonEquals.Name.AcceptAsync<CSSyntax.IdentifierNameSyntax>(TriviaConvertingExpressionVisitor)));
        }
        return attr;
    }

    public async Task<CSSyntax.ArgumentListSyntax> ConvertArgumentListOrEmptyAsync(SyntaxNode node, VBSyntax.ArgumentListSyntax argumentList)
    {
        return await argumentList.AcceptAsync<CSSyntax.ArgumentListSyntax>(TriviaConvertingExpressionVisitor) ?? CreateArgList(_semanticModel.GetSymbolInfo(node).Symbol);
    }


    private CSSyntax.ExpressionSyntax HoistByRefDeclaration(VBSyntax.SimpleArgumentSyntax node, CSSyntax.ExpressionSyntax refLValue, SemanticModelExtensions.RefConversion refType, string argName, RefKind refKind)
    {
        string prefix = $"arg{argName}";
        var expressionTypeInfo = _semanticModel.GetTypeInfo(node.Expression);
        bool useVar = expressionTypeInfo.Type?.Equals(expressionTypeInfo.ConvertedType, SymbolEqualityComparer.IncludeNullability) == true && !CommonConversions.ShouldPreferExplicitType(node.Expression, expressionTypeInfo.ConvertedType, out var _);
        var typeSyntax = CommonConversions.GetTypeSyntax(expressionTypeInfo.ConvertedType, useVar);

        if (refLValue is CSSyntax.ElementAccessExpressionSyntax eae) {
            //Hoist out the container so we can assign back to the same one after (like VB does)
            var tmpContainer = _typeContext.PerScopeState.Hoist(new AdditionalDeclaration("tmp", eae.Expression, ValidSyntaxFactory.VarType));
            refLValue = eae.WithExpression(tmpContainer.IdentifierName);
        }

        var withCast = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Expression, refLValue, defaultToCast: refKind != RefKind.None);

        var local = _typeContext.PerScopeState.Hoist(new AdditionalDeclaration(prefix, withCast, typeSyntax));

        if (refType == SemanticModelExtensions.RefConversion.PreAndPostAssignment) {
            var convertedLocalIdentifier = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Expression, local.IdentifierName, forceSourceType: expressionTypeInfo.ConvertedType, forceTargetType: expressionTypeInfo.Type);
            _typeContext.PerScopeState.Hoist(new AdditionalAssignment(refLValue, convertedLocalIdentifier));
        }

        return local.IdentifierName;
    }

    private ISymbol GetInvocationSymbol(SyntaxNode invocation)
    {
        var symbol = invocation.TypeSwitch(
            (VBSyntax.InvocationExpressionSyntax e) => _semanticModel.GetSymbolInfo(e).ExtractBestMatch<ISymbol>(),
            (VBSyntax.ObjectCreationExpressionSyntax e) => _semanticModel.GetSymbolInfo(e).ExtractBestMatch<ISymbol>(),
            (VBSyntax.RaiseEventStatementSyntax e) => _semanticModel.GetSymbolInfo(e.Name).ExtractBestMatch<ISymbol>(),
            (VBSyntax.MidExpressionSyntax _) => CommonConversions.KnownTypes.VbCompilerStringType?.GetMembers("MidStmtStr").FirstOrDefault(),
            _ => throw new NotSupportedException());
        return symbol;
    }

    private IEnumerable<CSSyntax.ArgumentSyntax> GetAdditionalRequiredArgs(
        IEnumerable<VBSyntax.ArgumentSyntax> arguments,
        ISymbol invocationSymbol)
    {
        var invocationHasOverloads = invocationSymbol.HasOverloads();
        return GetAdditionalRequiredArgs(arguments, processedParametersNames: null, invocationSymbol, invocationHasOverloads);
    }

    private IEnumerable<CSSyntax.ArgumentSyntax> GetAdditionalRequiredArgs(
        IEnumerable<VBSyntax.ArgumentSyntax> arguments,
        ICollection<string> processedParametersNames,
        ISymbol invocationSymbol,
        bool invocationHasOverloads)
    {
        if (invocationSymbol is null) {
            yield break;
        }

        var invocationHasOmittedArgs = arguments.Any(t => t.IsOmitted);
        var expandOptionalArgs = invocationHasOmittedArgs && invocationHasOverloads;
        var missingArgs = invocationSymbol.GetParameters().Where(t => processedParametersNames is null || !processedParametersNames.Contains(t.Name));
        var requiresCompareMethod = _visualBasicEqualityComparison.OptionCompareTextCaseInsensitive && RequiresStringCompareMethodToBeAppended(invocationSymbol);

        foreach (var parameterSymbol in missingArgs) {
            var extraArg = CreateExtraArgOrNull(parameterSymbol, requiresCompareMethod, expandOptionalArgs);
            if (extraArg != null) {
                yield return extraArg;
            }
        }
    }


    private static bool RequiresStringCompareMethodToBeAppended(ISymbol symbol) =>
        symbol?.ContainingType.Name == nameof(Strings) &&
        symbol.ContainingType.ContainingNamespace.Name == nameof(Microsoft.VisualBasic) &&
        symbol.ContainingType.ContainingNamespace.ContainingNamespace.Name == nameof(Microsoft) &&
        symbol.Name is "InStr" or "InStrRev" or "Replace" or "Split" or "StrComp";

    private CSSyntax.ArgumentSyntax CreateExtraArgOrNull(IParameterSymbol p, bool requiresCompareMethod, bool expandOptionalArgs)
    {
        var csRefKind = CommonConversions.GetCsRefKind(p);
        if (csRefKind != RefKind.None) {
            return CreateOptionalRefArg(p, csRefKind);
        }

        if (requiresCompareMethod && p.Type.GetFullMetadataName() == "Microsoft.VisualBasic.CompareMethod") {
            return (CSSyntax.ArgumentSyntax)CommonConversions.CsSyntaxGenerator.Argument(p.Name, RefKind.None, _visualBasicEqualityComparison.CompareMethodExpression);
        }

        if (expandOptionalArgs && p.HasExplicitDefaultValue) {
            return (CSSyntax.ArgumentSyntax)CommonConversions.CsSyntaxGenerator.Argument(p.Name, RefKind.None, CommonConversions.Literal(p.ExplicitDefaultValue));
        }

        return null;
    }

    private CSSyntax.ArgumentSyntax CreateOptionalRefArg(IParameterSymbol p, RefKind refKind)
    {
        string prefix = $"arg{p.Name}";
        var type = CommonConversions.GetTypeSyntax(p.Type);
        CSSyntax.ExpressionSyntax initializer;
        if (p.HasExplicitDefaultValue) {
            initializer = CommonConversions.Literal(p.ExplicitDefaultValue);
        } else if (HasOptionalAttribute(p)) {
            if (TryGetDefaultParameterValueAttributeValue(p, out var defaultValue)) {
                initializer = CommonConversions.Literal(defaultValue);
            } else {
                initializer = CS.SyntaxFactory.DefaultExpression(type);
            }
        } else {
            //invalid VB.NET code
            return null;
        }
        var local = _typeContext.PerScopeState.Hoist(new AdditionalDeclaration(prefix, initializer, type));
        return (CSSyntax.ArgumentSyntax)CommonConversions.CsSyntaxGenerator.Argument(p.Name, refKind, local.IdentifierName);

        bool HasOptionalAttribute(IParameterSymbol p)
        {
            var optionalAttribute = CommonConversions.KnownTypes.OptionalAttribute;
            if (optionalAttribute == null) {
                return false;
            }

            return p.GetAttributes().Any(a => SymbolEqualityComparer.IncludeNullability.Equals(a.AttributeClass, optionalAttribute));
        }

        bool TryGetDefaultParameterValueAttributeValue(IParameterSymbol p, out object defaultValue)
        {
            defaultValue = null;

            var defaultParameterValueAttribute = CommonConversions.KnownTypes.DefaultParameterValueAttribute;
            if (defaultParameterValueAttribute == null) {
                return false;
            }

            var attributeData = p.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.IncludeNullability.Equals(a.AttributeClass, defaultParameterValueAttribute));
            if (attributeData == null) {
                return false;
            }

            if (attributeData.ConstructorArguments.Length == 0) {
                return false;
            }

            defaultValue = attributeData.ConstructorArguments.First().Value;
            return true;
        }
    }

    public CSSyntax.ArgumentListSyntax CreateArgList(ISymbol invocationSymbol)
    {
        return CS.SyntaxFactory.ArgumentList(CS.SyntaxFactory.SeparatedList(
            GetAdditionalRequiredArgs(Array.Empty<VBSyntax.ArgumentSyntax>(), invocationSymbol))
        );
    }
}