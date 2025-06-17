using System.Collections.Immutable;
//using System.Data; // Not directly used by these methods
using System.Globalization;
//using System.Linq.Expressions; // Not directly used
//using System.Runtime.CompilerServices; // Not directly used
//using System.Xml.Linq; // Not used
//using ICSharpCode.CodeConverter.CSharp.Replacements; // Not directly used
using ICSharpCode.CodeConverter.Util.FromRoslyn; // For .Yield()
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations; // For IPropertyReferenceOperation in IsRefArrayAcces
//using Microsoft.CodeAnalysis.Simplification; // Not directly used
using Microsoft.VisualBasic; // For Strings in RequiresStringCompareMethodToBeAppended
//using Microsoft.VisualBasic.CompilerServices; // Not directly used
//using ComparisonKind = ICSharpCode.CodeConverter.CSharp.VisualBasicEqualityComparison.ComparisonKind; // Not used
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.CSharp;

internal partial class ExpressionNodeVisitor // Must be partial
{
    public override async Task<CSharpSyntaxNode> VisitArgumentList(VBasic.Syntax.ArgumentListSyntax node)
    {
        if (node.Parent.IsKind(VBasic.SyntaxKind.Attribute)) {
            return CommonConversions.CreateAttributeArgumentList(await node.Arguments.SelectAsync(ToAttributeArgumentAsync));
        }
        var argumentSyntaxes = await ConvertArgumentsAsync(node);
        return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(argumentSyntaxes));
    }

    public override async Task<CSharpSyntaxNode> VisitSimpleArgument(VBasic.Syntax.SimpleArgumentSyntax node)
    {
        var argList = (VBasic.Syntax.ArgumentListSyntax)node.Parent;
        var invocation = argList.Parent;
        if (invocation is VBasic.Syntax.ArrayCreationExpressionSyntax)
            return await node.Expression.AcceptAsync<CSharpSyntaxNode>(TriviaConvertingExpressionVisitor);
        var symbol = GetInvocationSymbol(invocation);
        SyntaxToken token = default(SyntaxToken);
        var convertedArgExpression = (await node.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor)).SkipIntoParens();
        var typeConversionAnalyzer = CommonConversions.TypeConversionAnalyzer;
        var baseSymbol = symbol?.OriginalDefinition.GetBaseSymbol();
        var possibleParameters = (CommonConversions.GetCsOriginalSymbolOrNull(baseSymbol) ?? symbol)?.GetParameters();
        if (possibleParameters.HasValue) {
            var refType = GetRefConversionType(node, argList, possibleParameters.Value, out var argName, out var refKind);
            token = GetRefToken(refKind);
            if (refType != RefConversion.Inline) {
                convertedArgExpression = HoistByRefDeclaration(node, convertedArgExpression, refType, argName, refKind);
            } else {
                convertedArgExpression = typeConversionAnalyzer.AddExplicitConversion(node.Expression, convertedArgExpression, defaultToCast: refKind != RefKind.None);
            }
        } else {
            convertedArgExpression = typeConversionAnalyzer.AddExplicitConversion(node.Expression, convertedArgExpression);
        }

        var nameColon = node.IsNamed ? SyntaxFactory.NameColon(await node.NameColonEquals.Name.AcceptAsync<IdentifierNameSyntax>(TriviaConvertingExpressionVisitor)) : null;
        return SyntaxFactory.Argument(nameColon, token, convertedArgExpression);
    }

    private ExpressionSyntax HoistByRefDeclaration(VBSyntax.SimpleArgumentSyntax node, ExpressionSyntax refLValue, RefConversion refType, string argName, RefKind refKind)
    {
        string prefix = $"arg{argName}";
        var expressionTypeInfo = _semanticModel.GetTypeInfo(node.Expression);
        bool useVar = expressionTypeInfo.Type?.Equals(expressionTypeInfo.ConvertedType, SymbolEqualityComparer.IncludeNullability) == true && !CommonConversions.ShouldPreferExplicitType(node.Expression, expressionTypeInfo.ConvertedType, out var _);
        var typeSyntax = CommonConversions.GetTypeSyntax(expressionTypeInfo.ConvertedType, useVar);

        if (refLValue is ElementAccessExpressionSyntax eae) {
            var tmpContainer = _typeContext.PerScopeState.Hoist(new AdditionalDeclaration("tmp", eae.Expression, ValidSyntaxFactory.VarType));
            refLValue = eae.WithExpression(tmpContainer.IdentifierName);
        }

        var withCast = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Expression, refLValue, defaultToCast: refKind != RefKind.None);
        var local = _typeContext.PerScopeState.Hoist(new AdditionalDeclaration(prefix, withCast, typeSyntax));

        if (refType == RefConversion.PreAndPostAssignment) {
            var convertedLocalIdentifier = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Expression, local.IdentifierName, forceSourceType: expressionTypeInfo.ConvertedType, forceTargetType: expressionTypeInfo.Type);
            _typeContext.PerScopeState.Hoist(new AdditionalAssignment(refLValue, convertedLocalIdentifier));
        }
        return local.IdentifierName;
    }

    private static SyntaxToken GetRefToken(RefKind refKind)
    {
        SyntaxToken token;
        switch (refKind) {
            case RefKind.None: token = default(SyntaxToken); break;
            case RefKind.Ref: token = SyntaxFactory.Token(SyntaxKind.RefKeyword); break;
            case RefKind.Out: token = SyntaxFactory.Token(SyntaxKind.OutKeyword); break;
            default: throw new ArgumentOutOfRangeException(nameof(refKind), refKind, null);
        }
        return token;
    }

    private RefConversion GetRefConversionType(VBSyntax.ArgumentSyntax node, VBSyntax.ArgumentListSyntax argList, ImmutableArray<IParameterSymbol> parameters, out string argName, out RefKind refKind)
    {
        var parameter = node.IsNamed && node is VBSyntax.SimpleArgumentSyntax sas
            ? parameters.FirstOrDefault(p => p.Name.Equals(sas.NameColonEquals.Name.Identifier.Text, StringComparison.OrdinalIgnoreCase))
            : parameters.ElementAtOrDefault(argList.Arguments.IndexOf(node));
        if (parameter != null) {
            refKind = parameter.RefKind;
            argName = parameter.Name;
        } else {
            refKind = RefKind.None;
            argName = null;
        }
        return NeedsVariableForArgument(node, refKind);
    }

    private async Task<IEnumerable<ArgumentSyntax>> ConvertArgumentsAsync(VBasic.Syntax.ArgumentListSyntax node)
    {
        ISymbol invocationSymbol = GetInvocationSymbol(node.Parent);
        var forceNamedParameters = false;
        var invocationHasOverloads = invocationSymbol.HasOverloads();

        var processedParameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var argumentSyntaxs = (await node.Arguments.SelectAsync(ConvertArg)).Where(a => a != null);
        return argumentSyntaxs.Concat(GetAdditionalRequiredArgs(node.Arguments, processedParameters, invocationSymbol, invocationHasOverloads));

        async Task<ArgumentSyntax> ConvertArg(VBSyntax.ArgumentSyntax arg, int argIndex)
        {
            var argName = arg is VBSyntax.SimpleArgumentSyntax { IsNamed: true } namedArg ? namedArg.NameColonEquals.Name.Identifier.Text : null;
            var parameterSymbol = invocationSymbol?.GetParameters().GetArgument(argName, argIndex);
            var convertedArg = await ConvertArgForParameter(arg, parameterSymbol);

            if (convertedArg is not null && parameterSymbol is not null) {
                processedParameters.Add(parameterSymbol.Name);
            }
            return convertedArg;
        }

        async Task<ArgumentSyntax> ConvertArgForParameter(VBSyntax.ArgumentSyntax arg, IParameterSymbol parameterSymbol)
        {
            if (arg.IsOmitted) {
                if (invocationSymbol != null && !invocationHasOverloads) {
                    forceNamedParameters = true;
                    return null;
                }
                return ConvertOmittedArgument(parameterSymbol);
            }

            var argSyntax = await arg.AcceptAsync<ArgumentSyntax>(TriviaConvertingExpressionVisitor);
            if (forceNamedParameters && !arg.IsNamed && parameterSymbol != null) {
                return argSyntax.WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName(CommonConversions.CsEscapedIdentifier(parameterSymbol.Name))));
            }
            return argSyntax;
        }

        ArgumentSyntax ConvertOmittedArgument(IParameterSymbol parameter)
        {
            if (parameter == null) {
                return SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression));
            }
            var csRefKind = CommonConversions.GetCsRefKind(parameter);
            return csRefKind != RefKind.None
                ? CreateOptionalRefArg(parameter, csRefKind)
                : SyntaxFactory.Argument(CommonConversions.Literal(parameter.ExplicitDefaultValue));
        }
    }

    private IEnumerable<ArgumentSyntax> GetAdditionalRequiredArgs(
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

    private ArgumentSyntax CreateExtraArgOrNull(IParameterSymbol p, bool requiresCompareMethod, bool expandOptionalArgs)
    {
        var csRefKind = CommonConversions.GetCsRefKind(p);
        if (csRefKind != RefKind.None) {
            return CreateOptionalRefArg(p, csRefKind);
        }

        if (requiresCompareMethod && p.Type.GetFullMetadataName() == "Microsoft.VisualBasic.CompareMethod") {
             return (ArgumentSyntax)CommonConversions.CsSyntaxGenerator.Argument(p.Name, RefKind.None, _visualBasicEqualityComparison.CompareMethodExpression);
        }

        if (expandOptionalArgs && p.HasExplicitDefaultValue) {
            return (ArgumentSyntax)CommonConversions.CsSyntaxGenerator.Argument(p.Name, RefKind.None, CommonConversions.Literal(p.ExplicitDefaultValue));
        }
        return null;
    }

    private ArgumentSyntax CreateOptionalRefArg(IParameterSymbol p, RefKind refKind)
    {
        string prefix = $"arg{p.Name}";
        var type = CommonConversions.GetTypeSyntax(p.Type);
        ExpressionSyntax initializer;
        if (p.HasExplicitDefaultValue) {
            initializer = CommonConversions.Literal(p.ExplicitDefaultValue);
        } else if (HasOptionalAttribute(p)) {
            if (TryGetDefaultParameterValueAttributeValue(p, out var defaultValue)){
                initializer = CommonConversions.Literal(defaultValue);
            } else {
                initializer = SyntaxFactory.DefaultExpression(type);
            }
        } else {
            return null; // Should not happen in valid VB code with ByRef optional
        }
        var local = _typeContext.PerScopeState.Hoist(new AdditionalDeclaration(prefix, initializer, type));
        return (ArgumentSyntax)CommonConversions.CsSyntaxGenerator.Argument(p.Name, refKind, local.IdentifierName);

        bool HasOptionalAttribute(IParameterSymbol param)
        {
            var optionalAttribute = CommonConversions.KnownTypes.OptionalAttribute;
            if (optionalAttribute == null) return false;
            return param.GetAttributes().Any(a => SymbolEqualityComparer.IncludeNullability.Equals(a.AttributeClass, optionalAttribute));
        }

        bool TryGetDefaultParameterValueAttributeValue(IParameterSymbol param, out object defaultValue)
        {
            defaultValue = null;
            var defaultParameterValueAttribute = CommonConversions.KnownTypes.DefaultParameterValueAttribute;
            if (defaultParameterValueAttribute == null) return false;
            var attributeData = param.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.IncludeNullability.Equals(a.AttributeClass, defaultParameterValueAttribute));
            if (attributeData == null || attributeData.ConstructorArguments.Length == 0) return false;
            defaultValue = attributeData.ConstructorArguments.First().Value;
            return true;
        }
    }

    private RefConversion NeedsVariableForArgument(VBasic.Syntax.ArgumentSyntax node, RefKind refKind)
    {
        if (refKind == RefKind.None) return RefConversion.Inline;
        if (!(node is VBSyntax.SimpleArgumentSyntax sas) || sas is { Expression: VBSyntax.ParenthesizedExpressionSyntax }) return RefConversion.PreAssigment;
        var expression = sas.Expression;

        return GetRefConversion(expression);

        RefConversion GetRefConversion(VBSyntax.ExpressionSyntax expr)
        {
            var symbolInfo = GetSymbolInfoInDocument<ISymbol>(expr);
            if (symbolInfo is IPropertySymbol { ReturnsByRef: false, ReturnsByRefReadonly: false } propertySymbol) {
                return propertySymbol.IsReadOnly ? RefConversion.PreAssigment : RefConversion.PreAndPostAssignment;
            }
            else if (symbolInfo is IFieldSymbol { IsConst: true } or ILocalSymbol { IsConst: true }) {
                return RefConversion.PreAssigment;
            } else if (symbolInfo is IMethodSymbol { ReturnsByRef: false, ReturnsByRefReadonly: false }) {
                return RefConversion.PreAssigment;
            }
            if (DeclaredInUsing(symbolInfo)) return RefConversion.PreAssigment;
            if (expr is VBasic.Syntax.IdentifierNameSyntax || expr is VBSyntax.MemberAccessExpressionSyntax || IsRefArrayAcces(expr)) {
                var typeInfo = _semanticModel.GetTypeInfo(expr);
                bool isTypeMismatch = typeInfo.Type == null || !typeInfo.Type.Equals(typeInfo.ConvertedType, SymbolEqualityComparer.IncludeNullability);
                if (isTypeMismatch) return RefConversion.PreAndPostAssignment;
                return RefConversion.Inline;
            }
            return RefConversion.PreAssigment;
        }

        bool IsRefArrayAcces(VBSyntax.ExpressionSyntax argumentExpression)
        {
            if (!(argumentExpression is VBSyntax.InvocationExpressionSyntax ies)) return false;
            var op = _semanticModel.GetOperation(ies);
            return (op.IsArrayElementAccess() || IsReturnsByRefPropertyElementAccess(op))
                && GetRefConversion(ies.Expression) == RefConversion.Inline;

            static bool IsReturnsByRefPropertyElementAccess(IOperation operation) =>
                operation.IsPropertyElementAccess() && operation is IPropertyReferenceOperation { Property: { } prop } && (prop.ReturnsByRef || prop.ReturnsByRefReadonly);
        }
    }

    private static bool DeclaredInUsing(ISymbol symbolInfo)
    {
        return symbolInfo?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax()?.Parent?.Parent?.IsKind(VBasic.SyntaxKind.UsingStatement) == true;
    }

    private enum RefConversion { Inline, PreAssigment, PreAndPostAssignment }

    private ISymbol GetInvocationSymbol(SyntaxNode invocation)
    {
        return invocation.TypeSwitch(
            (VBSyntax.InvocationExpressionSyntax e) => _semanticModel.GetSymbolInfo(e).ExtractBestMatch<ISymbol>(),
            (VBSyntax.ObjectCreationExpressionSyntax e) => _semanticModel.GetSymbolInfo(e).ExtractBestMatch<ISymbol>(),
            (VBSyntax.RaiseEventStatementSyntax e) => _semanticModel.GetSymbolInfo(e.Name).ExtractBestMatch<ISymbol>(),
            (VBSyntax.MidExpressionSyntax _) => CommonConversions.KnownTypes.VbCompilerStringType?.GetMembers("MidStmtStr").FirstOrDefault(),
            _ => throw new NotSupportedException());
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
        if (node.Parent?.Parent?.IsKind(VBasic.SyntaxKind.FunctionLambdaHeader, VBasic.SyntaxKind.SubLambdaHeader) != true || node.AsClause != null) {
            var vbParamSymbol = _semanticModel.GetDeclaredSymbol(node) as IParameterSymbol;
            paramType = vbParamSymbol != null ? CommonConversions.GetTypeSyntax(vbParamSymbol.Type) : await SyntaxOnlyConvertParamAsync(node);
        }
        var attributes = (await node.AttributeLists.SelectManyAsync(CommonConversions.ConvertAttributeAsync)).ToList();
        var modifiers = CommonConversions.ConvertModifiers(node, node.Modifiers, TokenContext.Local);
        var vbSymbol = _semanticModel.GetDeclaredSymbol(node) as IParameterSymbol;
        var baseParameters = vbSymbol?.ContainingSymbol.OriginalDefinition.GetBaseSymbol().GetParameters();
        var baseParameter = baseParameters?[vbSymbol.Ordinal];
        var csRefKind = CommonConversions.GetCsRefKind(baseParameter ?? vbSymbol, node);
        if (csRefKind == RefKind.Out) {
            modifiers = SyntaxFactory.TokenList(modifiers.Where(m => !m.IsKind(SyntaxKind.RefKeyword)).Concat(SyntaxFactory.Token(SyntaxKind.OutKeyword).Yield()));
        }
        EqualsValueClauseSyntax @default = null;
        if (node.Default != null) {
            var defaultValue = node.Default.Value.SkipIntoParens();
            if (_semanticModel.GetTypeInfo(defaultValue).Type?.SpecialType == SpecialType.System_DateTime) {
                 var constant = _semanticModel.GetConstantValue(defaultValue);
                if (constant.HasValue && constant.Value is DateTime dt) {
                    var dateTimeAsLongCsLiteral = CommonConversions.Literal(dt.Ticks).WithTrailingTrivia(SyntaxFactory.ParseTrailingTrivia($"/* {defaultValue} */"));
                    var dateTimeArg = CommonConversions.CreateAttributeArgumentList(SyntaxFactory.AttributeArgument(dateTimeAsLongCsLiteral));
                    _extraUsingDirectives.Add("System.Runtime.InteropServices");
                    _extraUsingDirectives.Add("System.Runtime.CompilerServices");
                    var optionalDateTimeAttributes = new[] { SyntaxFactory.Attribute(ValidSyntaxFactory.IdentifierName("Optional")), SyntaxFactory.Attribute(ValidSyntaxFactory.IdentifierName("DateTimeConstant"), dateTimeArg) };
                    attributes.Insert(0, SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(optionalDateTimeAttributes)));
                }
            } else if (node.Modifiers.Any(m => m.IsKind(VBasic.SyntaxKind.ByRefKeyword)) || HasRefParametersAfterThisOne(vbSymbol, baseParameters)) {
                 var defaultExpression = await node.Default.Value.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
                var arg = CommonConversions.CreateAttributeArgumentList(SyntaxFactory.AttributeArgument(defaultExpression));
                 _extraUsingDirectives.Add("System.Runtime.InteropServices");
                 _extraUsingDirectives.Add("System.Runtime.CompilerServices");
                var optionalAttributes = new List<AttributeSyntax> { SyntaxFactory.Attribute(ValidSyntaxFactory.IdentifierName("Optional")) };
                if (!node.Default.Value.IsKind(VBasic.SyntaxKind.NothingLiteralExpression)) {
                    optionalAttributes.Add(SyntaxFactory.Attribute(ValidSyntaxFactory.IdentifierName("DefaultParameterValue"), arg));
                }
                attributes.Insert(0, SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(optionalAttributes)));
            }
             else {
                @default = SyntaxFactory.EqualsValueClause(await node.Default.Value.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor));
            }
        }
        if (node.Parent.Parent is VBSyntax.MethodStatementSyntax mss && mss.AttributeLists.Any(CommonConversions.HasExtensionAttribute) && node.Parent.ChildNodes().First() == node && vbSymbol.ValidCSharpExtensionMethodParameter()) {
            modifiers = modifiers.Insert(0, SyntaxFactory.Token(SyntaxKind.ThisKeyword));
        }
        return SyntaxFactory.Parameter(SyntaxFactory.List(attributes), modifiers, paramType, id, @default);
    }

    private bool HasRefParametersAfterThisOne(IParameterSymbol vbSymbol, ImmutableArray<IParameterSymbol>? baseParameters) =>
        vbSymbol is not null && baseParameters is {} bp && bp.Skip(vbSymbol.Ordinal + 1).Any(x => x.RefKind != RefKind.None);

    private async Task<TypeSyntax> SyntaxOnlyConvertParamAsync(VBSyntax.ParameterSyntax node)
    {
        var syntaxParamType = await (node.AsClause?.Type).AcceptAsync<TypeSyntax>(TriviaConvertingExpressionVisitor) ?? SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword));
        var rankSpecifiers = await CommonConversions.ConvertArrayRankSpecifierSyntaxesAsync(node.Identifier.ArrayRankSpecifiers, node.Identifier.ArrayBounds, false);
        if (rankSpecifiers.Any()) syntaxParamType = SyntaxFactory.ArrayType(syntaxParamType, rankSpecifiers);
        if (!node.Identifier.Nullable.IsKind(SyntaxKind.None)) {
            var arrayType = syntaxParamType as ArrayTypeSyntax;
            syntaxParamType = arrayType == null ? SyntaxFactory.NullableType(syntaxParamType) : arrayType.WithElementType(SyntaxFactory.NullableType(arrayType.ElementType));
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

    private async Task<AttributeArgumentSyntax> ToAttributeArgumentAsync(VBasic.Syntax.ArgumentSyntax arg)
    {
        if (!(arg is VBasic.Syntax.SimpleArgumentSyntax a)) throw new NotSupportedException();
        var attr = SyntaxFactory.AttributeArgument(await a.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor));
        if (a.IsNamed) attr = attr.WithNameEquals(SyntaxFactory.NameEquals(await a.NameColonEquals.Name.AcceptAsync<IdentifierNameSyntax>(TriviaConvertingExpressionVisitor)));
        return attr;
    }

    private ArgumentListSyntax CreateArgList(ISymbol invocationSymbol)
    {
        return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
            GetAdditionalRequiredArgs(Array.Empty<VBSyntax.ArgumentSyntax>(), invocationSymbol))
        );
    }
}
