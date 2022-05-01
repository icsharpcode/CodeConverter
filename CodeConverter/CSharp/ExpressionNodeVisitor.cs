using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Linq;
using ICSharpCode.CodeConverter.CSharp.Replacements;
using ICSharpCode.CodeConverter.Util.FromRoslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace ICSharpCode.CodeConverter.CSharp;

/// <summary>
/// These could be nested within something like a field declaration, an arrow bodied member, or a statement within a method body
/// To understand the difference between how expressions are expressed, compare:
/// http://source.roslyn.codeplex.com/#Microsoft.CodeAnalysis.CSharp/Binder/Binder_Expressions.cs,365
/// http://source.roslyn.codeplex.com/#Microsoft.CodeAnalysis.VisualBasic/Binding/Binder_Expressions.vb,43
/// </summary>
internal class ExpressionNodeVisitor : VBasic.VisualBasicSyntaxVisitor<Task<CSharpSyntaxNode>>
{
    private static readonly Type ConvertType = typeof(Conversions);
    public CommentConvertingVisitorWrapper TriviaConvertingExpressionVisitor { get; }
    private readonly SemanticModel _semanticModel;
    private readonly HashSet<string> _extraUsingDirectives;
    private readonly XmlImportContext _xmlImportContext;
    private readonly IOperatorConverter _operatorConverter;
    private readonly VisualBasicEqualityComparison _visualBasicEqualityComparison;
    private readonly Stack<ExpressionSyntax> _withBlockLhs = new();
    private readonly ITypeContext _typeContext;
    private readonly QueryConverter _queryConverter;
    private readonly Lazy<IReadOnlyDictionary<ITypeSymbol, string>> _convertMethodsLookupByReturnType;
    private readonly LambdaConverter _lambdaConverter;
    private readonly INamedTypeSymbol _vbBooleanTypeSymbol;
    private readonly VisualBasicNullableExpressionsConverter _visualBasicNullableTypesConverter;

    public ExpressionNodeVisitor(SemanticModel semanticModel,
        VisualBasicEqualityComparison visualBasicEqualityComparison, ITypeContext typeContext, CommonConversions commonConversions,
        HashSet<string> extraUsingDirectives, XmlImportContext xmlImportContext, VisualBasicNullableExpressionsConverter visualBasicNullableTypesConverter)
    {
        CommonConversions = commonConversions;
        _semanticModel = semanticModel;
        _lambdaConverter = new LambdaConverter(commonConversions, semanticModel);
        _visualBasicEqualityComparison = visualBasicEqualityComparison;
        TriviaConvertingExpressionVisitor = new CommentConvertingVisitorWrapper(this, _semanticModel.SyntaxTree);
        _queryConverter = new QueryConverter(commonConversions, _semanticModel, TriviaConvertingExpressionVisitor);
        _typeContext = typeContext;
        _extraUsingDirectives = extraUsingDirectives;
        _xmlImportContext = xmlImportContext;
        _visualBasicNullableTypesConverter = visualBasicNullableTypesConverter;
        _operatorConverter = VbOperatorConversion.Create(TriviaConvertingExpressionVisitor, semanticModel, visualBasicEqualityComparison, commonConversions.TypeConversionAnalyzer);
        // If this isn't needed, the assembly with Conversions may not be referenced, so this must be done lazily
        _convertMethodsLookupByReturnType =
            new Lazy<IReadOnlyDictionary<ITypeSymbol, string>>(() => CreateConvertMethodsLookupByReturnType(semanticModel));
        _vbBooleanTypeSymbol = _semanticModel.Compilation.GetTypeByMetadataName("System.Boolean");
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

    public override async Task<CSharpSyntaxNode> VisitXmlEmbeddedExpression(VBSyntax.XmlEmbeddedExpressionSyntax node) =>
        await node.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);

    public override async Task<CSharpSyntaxNode> VisitXmlDocument(VBasic.Syntax.XmlDocumentSyntax node)
    {
        _extraUsingDirectives.Add("System.Xml.Linq");
        var arguments = SyntaxFactory.SeparatedList(
            (await node.PrecedingMisc.SelectAsync(async misc => SyntaxFactory.Argument(await misc.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor))))
            .Concat(SyntaxFactory.Argument(await node.Root.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor)).Yield())
            .Concat(await node.FollowingMisc.SelectAsync(async misc => SyntaxFactory.Argument(await misc.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor))))
        );
        return ApplyXmlImportsIfNecessary(node, SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName("XDocument")).WithArgumentList(SyntaxFactory.ArgumentList(arguments)));
    }

    public override async Task<CSharpSyntaxNode> VisitXmlElement(VBasic.Syntax.XmlElementSyntax node)
    {
        _extraUsingDirectives.Add("System.Xml.Linq");
        var arguments = SyntaxFactory.SeparatedList(
            SyntaxFactory.Argument(await node.StartTag.Name.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor)).Yield()
                .Concat(await node.StartTag.Attributes.SelectAsync(async attribute => SyntaxFactory.Argument(await attribute.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor))))
                .Concat(await node.Content.SelectAsync(async content => SyntaxFactory.Argument(await content.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor))))
        );
        return ApplyXmlImportsIfNecessary(node, SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName("XElement")).WithArgumentList(SyntaxFactory.ArgumentList(arguments)));
    }

    public override async Task<CSharpSyntaxNode> VisitXmlEmptyElement(VBSyntax.XmlEmptyElementSyntax node)
    {
        _extraUsingDirectives.Add("System.Xml.Linq");
        var arguments = SyntaxFactory.SeparatedList(
            SyntaxFactory.Argument(await node.Name.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor)).Yield()
                .Concat(await node.Attributes.SelectAsync(async attribute => SyntaxFactory.Argument(await attribute.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor))))
        );
        return ApplyXmlImportsIfNecessary(node, SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName("XElement")).WithArgumentList(SyntaxFactory.ArgumentList(arguments)));
    }

    private CSharpSyntaxNode ApplyXmlImportsIfNecessary(VBSyntax.XmlNodeSyntax vbNode, ObjectCreationExpressionSyntax creation)
    {
        if (!_xmlImportContext.HasImports || vbNode.Parent is VBSyntax.XmlNodeSyntax) return creation;
        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, XmlImportContext.HelperClassShortIdentifierName, SyntaxFactory.IdentifierName("Apply")), 
            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(creation))));
    }

    public override async Task<CSharpSyntaxNode> VisitXmlAttribute(VBasic.Syntax.XmlAttributeSyntax node)
    {
        var arguments = SyntaxFactory.SeparatedList(
            SyntaxFactory.Argument(CommonConversions.Literal(node.Name.ToString())).Yield()
                .Concat(SyntaxFactory.Argument(await node.Value.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor)).Yield())
        );
        return SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName("XAttribute")).WithArgumentList(SyntaxFactory.ArgumentList(arguments));
    }

    public override async Task<CSharpSyntaxNode> VisitXmlString(VBasic.Syntax.XmlStringSyntax node) =>
        CommonConversions.Literal(node.TextTokens.Aggregate("", (a, b) => a + LiteralConversions.EscapeVerbatimQuotes(b.Text)));

    public override async Task<CSharpSyntaxNode> VisitXmlText(VBSyntax.XmlTextSyntax node) =>
        CommonConversions.Literal(node.TextTokens.Aggregate("", (a, b) => a + LiteralConversions.EscapeVerbatimQuotes(b.Text)));

    /// <summary>
    /// https://docs.microsoft.com/en-us/dotnet/visual-basic/programming-guide/language-features/xml/accessing-xml
    /// </summary>
    public override async Task<CSharpSyntaxNode> VisitXmlMemberAccessExpression(
        VBasic.Syntax.XmlMemberAccessExpressionSyntax node)
    {
        _extraUsingDirectives.Add("System.Xml.Linq");

        var xElementMethodName = GetXElementMethodName(node);

        ExpressionSyntax elements = node.Base != null ? SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            await node.Base.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor),
            SyntaxFactory.IdentifierName(xElementMethodName)
        ) : SyntaxFactory.MemberBindingExpression(
            SyntaxFactory.IdentifierName(xElementMethodName)
        );

        return SyntaxFactory.InvocationExpression(elements,
            ExpressionSyntaxExtensions.CreateArgList(
                await node.Name.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor))
        );
    }

    private static string GetXElementMethodName(VBSyntax.XmlMemberAccessExpressionSyntax node)
    {
        if (node.Token2 == default(SyntaxToken)) {
            return "Elements";
        }

        if (node.Token2.Text == "@") {
            return "Attributes";
        }

        if (node.Token2.Text == ".") {
            return "Descendants";
        }
        throw new NotImplementedException($"Xml member access operator: '{node.Token1}{node.Token2}{node.Token3}'");
    }

    public override Task<CSharpSyntaxNode> VisitXmlBracketedName(VBSyntax.XmlBracketedNameSyntax node)
    {
        return node.Name.AcceptAsync<CSharpSyntaxNode>(TriviaConvertingExpressionVisitor);
    }

    public override async Task<CSharpSyntaxNode> VisitXmlName(VBSyntax.XmlNameSyntax node)
    {
        if (node.Prefix != null) {
            return SyntaxFactory.BinaryExpression(
                SyntaxKind.AddExpression,
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    XmlImportContext.HelperClassShortIdentifierName,
                    SyntaxFactory.IdentifierName(node.Prefix.Name.ValueText)
                ),
                SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(node.LocalName.Text))
            );
        }

        if (_xmlImportContext.HasDefaultImport) {
            return SyntaxFactory.BinaryExpression(
                SyntaxKind.AddExpression,
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    XmlImportContext.HelperClassShortIdentifierName,
                    XmlImportContext.DefaultIdentifierName
                ),
                SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(node.LocalName.Text))
            );
        }

        return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(node.LocalName.Text));
    }

    public override async Task<CSharpSyntaxNode> VisitGetTypeExpression(VBasic.Syntax.GetTypeExpressionSyntax node)
    {
        return SyntaxFactory.TypeOfExpression(await node.Type.AcceptAsync<TypeSyntax>(TriviaConvertingExpressionVisitor));
    }

    public override async Task<CSharpSyntaxNode> VisitGlobalName(VBasic.Syntax.GlobalNameSyntax node)
    {
        return SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword));
    }

    public override async Task<CSharpSyntaxNode> VisitAwaitExpression(VBasic.Syntax.AwaitExpressionSyntax node)
    {
        return SyntaxFactory.AwaitExpression(await node.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor));
    }

    public override async Task<CSharpSyntaxNode> VisitCatchBlock(VBasic.Syntax.CatchBlockSyntax node)
    {
        var stmt = node.CatchStatement;
        CatchDeclarationSyntax catcher = null;
        if (stmt.AsClause != null) {
            catcher = SyntaxFactory.CatchDeclaration(
                ConvertTypeSyntax(stmt.AsClause.Type),
                ConvertIdentifier(stmt.IdentifierName.Identifier)
            );
        }

        var filter = await stmt.WhenClause.AcceptAsync<CatchFilterClauseSyntax>(TriviaConvertingExpressionVisitor);
        var methodBodyVisitor = await CreateMethodBodyVisitorAsync(node); //Probably should actually be using the existing method body visitor in order to get variable name generation correct
        var stmts = await node.Statements.SelectManyAsync(async s => (IEnumerable<StatementSyntax>) await s.Accept(methodBodyVisitor));
        return SyntaxFactory.CatchClause(
            catcher,
            filter,
            SyntaxFactory.Block(stmts)
        );
    }

    private TypeSyntax ConvertTypeSyntax(VBSyntax.TypeSyntax vbType)
    {
        if (_semanticModel.GetSymbolInfo(vbType).Symbol is ITypeSymbol typeSymbol)
            return CommonConversions.GetTypeSyntax(typeSymbol);
        return SyntaxFactory.ParseTypeName(vbType.ToString());
    }

    public override async Task<CSharpSyntaxNode> VisitCatchFilterClause(VBasic.Syntax.CatchFilterClauseSyntax node)
    {
        return SyntaxFactory.CatchFilterClause(await node.Filter.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor));
    }

    public override async Task<CSharpSyntaxNode> VisitFinallyBlock(VBasic.Syntax.FinallyBlockSyntax node)
    {
        var methodBodyVisitor = await CreateMethodBodyVisitorAsync(node); //Probably should actually be using the existing method body visitor in order to get variable name generation correct
        var stmts = await node.Statements.SelectManyAsync(async s => (IEnumerable<StatementSyntax>) await s.Accept(methodBodyVisitor));
        return SyntaxFactory.FinallyClause(SyntaxFactory.Block(stmts));
    }

    public override async Task<CSharpSyntaxNode> VisitCTypeExpression(VBasic.Syntax.CTypeExpressionSyntax node)
    {
        var csharpArg = await node.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
        var typeInfo = _semanticModel.GetTypeInfo(node);
        var forceTargetType = typeInfo.ConvertedType;
        return CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Expression, csharpArg, forceTargetType: forceTargetType, defaultToCast: true).AddParens();
    }

    public override async Task<CSharpSyntaxNode> VisitDirectCastExpression(VBasic.Syntax.DirectCastExpressionSyntax node)
    {
        return await ConvertCastExpressionAsync(node, castToOrNull: node.Type);
    }

    public override async Task<CSharpSyntaxNode> VisitPredefinedCastExpression(VBasic.Syntax.PredefinedCastExpressionSyntax node)
    {
        var simplifiedOrNull = await WithRemovedRedundantConversionOrNullAsync(node, node.Expression);
        if (simplifiedOrNull != null) return simplifiedOrNull;

        var expressionSyntax = await node.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
        if (SyntaxTokenExtensions.IsKind(node.Keyword, VBasic.SyntaxKind.CDateKeyword)) {

            _extraUsingDirectives.Add("Microsoft.VisualBasic.CompilerServices");
            return SyntaxFactory.InvocationExpression(SyntaxFactory.ParseExpression("Conversions.ToDate"), SyntaxFactory.ArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(expressionSyntax))));
        }

        var withConversion = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Expression, expressionSyntax, false, false, forceTargetType: _semanticModel.GetTypeInfo(node).Type);
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
                        SyntaxFactory.IdentifierName(
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
        }
        if (left == null) {
            if (IsSubPartOfConditionalAccess(node)) {
                return isDefaultProperty ? SyntaxFactory.ElementBindingExpression()
                    : await AdjustForImplicitInvocationAsync(node, SyntaxFactory.MemberBindingExpression(simpleNameSyntax));
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
            //Hoist out the container so we can assign back to the same one after (like VB does)
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
            case RefKind.None:
                token = default(SyntaxToken);
                break;
            case RefKind.Ref:
                token = SyntaxFactory.Token(SyntaxKind.RefKeyword);
                break;
            case RefKind.Out:
                token = SyntaxFactory.Token(SyntaxKind.OutKeyword);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(refKind), refKind, null);
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

    public override async Task<CSharpSyntaxNode> VisitNameOfExpression(VBasic.Syntax.NameOfExpressionSyntax node)
    {
        return SyntaxFactory.InvocationExpression(ValidSyntaxFactory.NameOf(), SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(await node.Argument.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor)))));
    }

    public override async Task<CSharpSyntaxNode> VisitEqualsValue(VBasic.Syntax.EqualsValueSyntax node)
    {
        return SyntaxFactory.EqualsValueClause(await node.Value.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor));
    }

    public override async Task<CSharpSyntaxNode> VisitObjectMemberInitializer(VBasic.Syntax.ObjectMemberInitializerSyntax node)
    {
        var initializers = await node.Initializers.AcceptSeparatedListAsync<VBSyntax.FieldInitializerSyntax, ExpressionSyntax>(TriviaConvertingExpressionVisitor);
        return SyntaxFactory.InitializerExpression(SyntaxKind.ObjectInitializerExpression, initializers);
    }

    public override async Task<CSharpSyntaxNode> VisitAnonymousObjectCreationExpression(VBasic.Syntax.AnonymousObjectCreationExpressionSyntax node)
    {
        var initializers = await node.Initializer.Initializers.AcceptSeparatedListAsync<VBSyntax.FieldInitializerSyntax, AnonymousObjectMemberDeclaratorSyntax>(TriviaConvertingExpressionVisitor);
        return SyntaxFactory.AnonymousObjectCreationExpression(initializers);
    }

    public override async Task<CSharpSyntaxNode> VisitInferredFieldInitializer(VBasic.Syntax.InferredFieldInitializerSyntax node)
    {
        return SyntaxFactory.AnonymousObjectMemberDeclarator(await node.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor));
    }

    public override async Task<CSharpSyntaxNode> VisitObjectCreationExpression(VBasic.Syntax.ObjectCreationExpressionSyntax node)
    {
        return SyntaxFactory.ObjectCreationExpression(
            await node.Type.AcceptAsync<TypeSyntax>(TriviaConvertingExpressionVisitor),
            // VB can omit empty arg lists:
            await ConvertArgumentListOrEmptyAsync(node, node.ArgumentList),
            await node.Initializer.AcceptAsync<InitializerExpressionSyntax>(TriviaConvertingExpressionVisitor)
        );
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
                SyntaxFactory.IdentifierName(nameof(Array)), SyntaxFactory.GenericName(nameof(Array.Empty)).WithTypeArgumentList(arrayTypeArgs));
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

    public override async Task<CSharpSyntaxNode> VisitNamedFieldInitializer(VBasic.Syntax.NamedFieldInitializerSyntax node)
    {
        var csExpressionSyntax = await node.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
        csExpressionSyntax =
            CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Expression, csExpressionSyntax);
        if (node.Parent?.Parent is VBasic.Syntax.AnonymousObjectCreationExpressionSyntax) {
            return SyntaxFactory.AnonymousObjectMemberDeclarator(
                SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName(ConvertIdentifier(node.Name.Identifier))),
                csExpressionSyntax);
        }

        return SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
            await node.Name.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor),
            csExpressionSyntax
        );
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
        condition = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Condition, condition, forceTargetType: _vbBooleanTypeSymbol);

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
        if (await _operatorConverter.ConvertReferenceOrNothingComparisonOrNullAsync(node.Operand.SkipIntoParens(), true) is { } nothingComparison) {
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

    public override async Task<CSharpSyntaxNode> VisitBinaryExpression(VBasic.Syntax.BinaryExpressionSyntax node)
    {
        if (await _operatorConverter.ConvertRewrittenBinaryOperatorOrNullAsync(node) is { } operatorNode) {
            return operatorNode;
        }

        var lhsTypeInfo = _semanticModel.GetTypeInfo(node.Left);
        var rhsTypeInfo = _semanticModel.GetTypeInfo(node.Right);

        var lhs = await node.Left.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
        var rhs = await node.Right.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);

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
                    forceLhsTargetType = _semanticModel.Compilation.GetTypeByMetadataName("System.String");
                }
            }
        }

        var objectEqualityType = VisualBasicEqualityComparison.GetObjectEqualityType(node, lhsTypeInfo, rhsTypeInfo);
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
                return _visualBasicEqualityComparison.GetFullExpressionForVbObjectComparison(lhs, rhs, node.IsKind(VBasic.SyntaxKind.NotEqualsExpression));
        }

        omitConversion |= lhsTypeInfo.Type != null && rhsTypeInfo.Type != null &&
                          lhsTypeInfo.Type.IsEnumType() && SymbolEqualityComparer.IncludeNullability.Equals(lhsTypeInfo.Type, rhsTypeInfo.Type)
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

        // If written by the user (i.e. not generated during expand phase), maintain intended semantics which could throw sometimes e.g. object o = (int) (object) long.MaxValue;
        var writtenByUser = !conversionNode.HasAnnotation(Simplifier.Annotation);
        var forceTargetType = typeInfo.ConvertedType;
        // TypeConversionAnalyzer can't figure out which type is required for operator/method overloads, inferred func returns or inferred variable declarations
        //      (currently overapproximates for numeric and gets it wrong in non-numeric cases).
        // Future: Avoid more redundant conversions by still calling AddExplicitConversion when writtenByUser avoiding the above and forcing typeInfo.Type
        return writtenByUser ? null : CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(conversionArg, csharpArg,
            forceTargetType: forceTargetType, defaultToCast: true);
    }


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
        //TODO: Decide if the above override should be subject to the rest of this method's adjustments (probably)


        // VB doesn't have a specialized node for element access because the syntax is ambiguous. Instead, it just uses an invocation expression or dictionary access expression, then figures out using the semantic model which one is most likely intended.
        // https://github.com/dotnet/roslyn/blob/master/src/Workspaces/VisualBasic/Portable/LanguageServices/VisualBasicSyntaxFactsService.vb#L768
        (var convertedExpression, bool shouldBeElementAccess) = await ConvertInvocationSubExpressionAsync(node, operation, expressionSymbol, expressionReturnType, expr);
        if (shouldBeElementAccess)
        {
            return await CreateElementAccessAsync(node, convertedExpression);
        }

        if (expressionSymbol != null && expressionSymbol.IsKind(SymbolKind.Property) &&
            invocationSymbol != null && invocationSymbol.GetParameters().Length == 0 && node.ArgumentList.Arguments.Count == 0)
        {
            return convertedExpression; //Parameterless property access
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
                SyntaxFactory.IdentifierName(CommonConversions.CsEscapedIdentifier(invocationSymbol.Name)));
        }

        if (invocationSymbol is IMethodSymbol m && convertedExpression is LambdaExpressionSyntax) {
            convertedExpression = SyntaxFactory.ObjectCreationExpression(CommonConversions.GetFuncTypeSyntax(m), ExpressionSyntaxExtensions.CreateArgList(convertedExpression), null);
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
            // Special case where structure changes due to conditional access (See VisitMemberAccessExpression)
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
        if (expressionType.Name == nameof(DataTable))
        {
            _extraUsingDirectives.Add(nameof(System) + "." + nameof(System.Data));

            expression = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression, expression,
                SyntaxFactory.IdentifierName(nameof(DataTableExtensions.AsEnumerable))));
        }

        var newExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
            expression, SyntaxFactory.IdentifierName(nameof(Enumerable.ElementAtOrDefault)));

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


    /// <summary>
    /// The VB compiler actually just hoists the conditions within the same method, but that leads to the original logic looking very different.
    /// This should be equivalent but keep closer to the look of the original source code.
    /// See https://github.com/icsharpcode/CodeConverter/issues/310 and https://github.com/icsharpcode/CodeConverter/issues/324
    /// </summary>
    private async Task<InvocationExpressionSyntax> HoistAndCallLocalFunctionAsync(VBSyntax.InvocationExpressionSyntax invocation, IMethodSymbol invocationSymbol, ExpressionSyntax csExpression)
    {
        const string retVariableName = "ret";
        var localFuncName = $"local{invocationSymbol.Name}";
        var generatedNames = new HashSet<string>();//TODO: Populate from local scope

        var callAndStoreResult = CommonConversions.CreateLocalVariableDeclarationAndAssignment(retVariableName, csExpression);

        var statements = await _typeContext.PerScopeState.CreateLocalsAsync(invocation, new[] { callAndStoreResult }, generatedNames, _semanticModel);

        var block = SyntaxFactory.Block(
            statements.Concat(SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(retVariableName)).Yield())
        );
        var returnType = CommonConversions.GetTypeSyntax(invocationSymbol.ReturnType);
            
        var localFunc = _typeContext.PerScopeState.Hoist(new HoistedParameterlessFunction(localFuncName, returnType, block));
        return SyntaxFactory.InvocationExpression(localFunc.TempIdentifier, SyntaxFactory.ArgumentList());
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
            var refConversion = GetRefConversionType(a, invocation.ArgumentList, invocationSymbol.Parameters, out string _, out _);
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
            switch (parent)
            {
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
            VBSyntax.MemberAccessExpressionSyntax maes => maes.Expression is {} exp && !MayThrow(exp),
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

    public override async Task<CSharpSyntaxNode> VisitSingleLineLambdaExpression(VBasic.Syntax.SingleLineLambdaExpressionSyntax node)
    {
        IReadOnlyCollection<StatementSyntax> convertedStatements;
        if (node.Body is VBasic.Syntax.StatementSyntax statement) {
            convertedStatements = await statement.Accept(await CreateMethodBodyVisitorAsync(node));
        } else {
            var csNode = await node.Body.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
            convertedStatements = new[] { SyntaxFactory.ExpressionStatement(csNode)};
        }
        var param = await node.SubOrFunctionHeader.ParameterList.AcceptAsync<ParameterListSyntax>(TriviaConvertingExpressionVisitor);
        return await _lambdaConverter.ConvertAsync(node, param, convertedStatements);
    }

    public override async Task<CSharpSyntaxNode> VisitMultiLineLambdaExpression(VBasic.Syntax.MultiLineLambdaExpressionSyntax node)
    {
        var methodBodyVisitor = await CreateMethodBodyVisitorAsync(node);
        var body = await node.Statements.SelectManyAsync(async s => (IEnumerable<StatementSyntax>) await s.Accept(methodBodyVisitor));
        var param = await node.SubOrFunctionHeader.ParameterList.AcceptAsync<ParameterListSyntax>(TriviaConvertingExpressionVisitor);
        return await _lambdaConverter.ConvertAsync(node, param, body.ToList());
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
                        SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Optional")),
                        SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("DateTimeConstant"), dateTimeArg)
                    };
                    attributes.Insert(0,
                        SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(optionalDateTimeAttributes)));
                }
            } else if (node.Modifiers.Any(m => m.IsKind(VBasic.SyntaxKind.ByRefKeyword))) {
                var defaultExpression = await node.Default.Value.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
                var arg = CommonConversions.CreateAttributeArgumentList(SyntaxFactory.AttributeArgument(defaultExpression));
                _extraUsingDirectives.Add("System.Runtime.InteropServices");
                _extraUsingDirectives.Add("System.Runtime.CompilerServices");
                var optionalAttributes = new[] {
                    SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Optional")),
                    SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("DefaultParameterValue"), arg)
                };
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
    }

    private async Task<TypeSyntax> SyntaxOnlyConvertParamAsync(VBSyntax.ParameterSyntax node)
    {
        var syntaxParamType = await (node.AsClause?.Type).AcceptAsync<TypeSyntax>(TriviaConvertingExpressionVisitor)
                              ?? SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword));

        var rankSpecifiers = await CommonConversions.ConvertArrayRankSpecifierSyntaxesAsync(node.Identifier.ArrayRankSpecifiers, node.Identifier.ArrayBounds, false);
        if (rankSpecifiers.Any()) {
            syntaxParamType = SyntaxFactory.ArrayType(syntaxParamType, rankSpecifiers);
        }

        if (!SyntaxTokenExtensions.IsKind(node.Identifier.Nullable, SyntaxKind.None)) {
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
        if (SyntaxTokenExtensions.IsKind(node.Keyword, VBasic.SyntaxKind.DateKeyword)) {
            return SyntaxFactory.IdentifierName(nameof(DateTime));
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

    /// <remarks>PERF: This is a hot code path, try to avoid using things like GetOperation except where needed.</remarks>
    public override async Task<CSharpSyntaxNode> VisitIdentifierName(VBasic.Syntax.IdentifierNameSyntax node)
    {
        var identifier = SyntaxFactory.IdentifierName(ConvertIdentifier(node.Identifier, node.GetAncestor<VBasic.Syntax.AttributeSyntax>() != null));

        bool requiresQualification = !node.Parent.IsKind(VBasic.SyntaxKind.SimpleMemberAccessExpression, VBasic.SyntaxKind.QualifiedName, VBasic.SyntaxKind.NameColonEquals, VBasic.SyntaxKind.ImportsStatement, VBasic.SyntaxKind.NamespaceStatement, VBasic.SyntaxKind.NamedFieldInitializer) ||
                                     node.Parent is VBSyntax.NamedFieldInitializerSyntax nfs && nfs.Expression == node ||
                                     node.Parent is VBasic.Syntax.MemberAccessExpressionSyntax maes && maes.Expression == node;
        var qualifiedIdentifier = requiresQualification
            ? QualifyNode(node, identifier) : identifier;

        var sym = GetSymbolInfoInDocument<ISymbol>(node);
        if (sym is ILocalSymbol) {
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

    private async Task<CSharpSyntaxNode> AdjustForImplicitInvocationAsync(SyntaxNode node, ExpressionSyntax qualifiedIdentifier)
    {
        //PERF: Avoid calling expensive GetOperation when it's easy
        bool nonExecutableNode = node.IsParentKind(VBasic.SyntaxKind.QualifiedName);
        if (nonExecutableNode || _semanticModel.SyntaxTree != node.SyntaxTree) return qualifiedIdentifier;

        if (await TryConvertParameterizedPropertyAsync(_semanticModel.GetOperation(node), node, qualifiedIdentifier) is {}
                invocation)
        {
            return invocation;
        }

        return AddEmptyArgumentListIfImplicit(node, qualifiedIdentifier);
    }

    public override async Task<CSharpSyntaxNode> VisitQualifiedName(VBasic.Syntax.QualifiedNameSyntax node)
    {
        var symbol = GetSymbolInfoInDocument<ITypeSymbol>(node);
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

    public override async Task<CSharpSyntaxNode> VisitGenericName(VBasic.Syntax.GenericNameSyntax node)
    {
        var symbol = GetSymbolInfoInDocument<ISymbol>(node);
        var genericNameSyntax = await GenericNameAccountingForReducedParametersAsync(node, symbol);
        return await AdjustForImplicitInvocationAsync(node, genericNameSyntax);
    }

    /// <summary>
    /// Adjusts for Visual Basic's omission of type arguments that can be inferred in reduced generic method invocations
    /// The upfront WithExpandedRootAsync pass should ensure this only happens on broken syntax trees.
    /// In those cases, just comment the errant information. It would only cause a compiling change in behaviour if it can be inferred, was not set to the inferred value, and was reflected upon within the method body
    /// </summary>
    private async Task<SimpleNameSyntax> GenericNameAccountingForReducedParametersAsync(VBSyntax.GenericNameSyntax node, ISymbol symbol)
    {
        SyntaxToken convertedIdentifier = ConvertIdentifier(node.Identifier);
        if (symbol is IMethodSymbol vbMethod && vbMethod.IsReducedTypeParameterMethod()) {
            var allTypeArgs = GetOrNullAllTypeArgsIncludingInferred(vbMethod);
            if (allTypeArgs != null) {
                return (SimpleNameSyntax)CommonConversions.CsSyntaxGenerator.GenericName(convertedIdentifier.Text, allTypeArgs);
            }
            var commentedText = "/* " + (await ConvertTypeArgumentListAsync(node)).ToFullString() + " */";
            var error = SyntaxFactory.ParseLeadingTrivia($"#error Conversion error: Could not convert all type parameters, so they've been commented out. Inferred type may be different{Environment.NewLine}");
            var partialConversion = SyntaxFactory.Comment(commentedText);
            return SyntaxFactory.IdentifierName(convertedIdentifier).WithPrependedLeadingTrivia(error).WithTrailingTrivia(partialConversion);
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

    public override async Task<CSharpSyntaxNode> VisitTypeArgumentList(VBasic.Syntax.TypeArgumentListSyntax node)
    {
        var args = await node.Arguments.SelectAsync(async a => await a.AcceptAsync<TypeSyntax>(TriviaConvertingExpressionVisitor));
        return SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(args));
    }

    public async Task<VBasic.VisualBasicSyntaxVisitor<Task<SyntaxList<StatementSyntax>>>> CreateMethodBodyVisitorAsync(VBasic.VisualBasicSyntaxNode node, bool isIterator = false, IdentifierNameSyntax csReturnVariable = null)
    {
        var methodBodyVisitor = await MethodBodyExecutableStatementVisitor.CreateAsync(node, _semanticModel, TriviaConvertingExpressionVisitor, CommonConversions, _withBlockLhs, _extraUsingDirectives, _typeContext, isIterator, csReturnVariable);
        return methodBodyVisitor.CommentConvertingVisitor;
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

    private async Task<IEnumerable<ArgumentSyntax>> ConvertArgumentsAsync(VBasic.Syntax.ArgumentListSyntax node)
    {
        ISymbol invocationSymbol = GetInvocationSymbol(node.Parent);
        bool forceNamedParameters = false;
        var argumentSyntaxs = (await node.Arguments.SelectAsync(async (a, i) => await ConvertArg(a, i)))
            .Where(a => a != null);
        argumentSyntaxs = argumentSyntaxs.Concat(GetAdditionalRequiredArgs(invocationSymbol, node.Arguments));

        return argumentSyntaxs;

        async Task<ArgumentSyntax> ConvertArg(VBSyntax.ArgumentSyntax a, int i)
        {
            if (a.IsOmitted) {
                if (invocationSymbol != null) {
                    forceNamedParameters = true;
                    return null; //Prefer to skip omitted and use named parameters when the symbol is available
                }

                var defaultLiteral = SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression);
                return SyntaxFactory.Argument(defaultLiteral);
            }

            var argumentSyntax = await a.AcceptAsync<ArgumentSyntax>(TriviaConvertingExpressionVisitor);

            if (forceNamedParameters && !a.IsNamed) {
                var elementAtOrDefault = invocationSymbol.GetParameters().ElementAt(i).Name;
                return argumentSyntax.WithNameColon(SyntaxFactory.NameColon(elementAtOrDefault));
            }

            return argumentSyntax;
        }
    }

    private IEnumerable<ArgumentSyntax> GetAdditionalRequiredArgs(ISymbol invocationSymbol, IReadOnlyCollection<VBasic.Syntax.ArgumentSyntax> existingArgs)
    {
        int vbPositionalArgs = existingArgs.TakeWhile(a => !a.IsNamed).Count();
        var namedArgNames = existingArgs
            .OfType<VBasic.Syntax.SimpleArgumentSyntax>()
            .Where(a => a.IsNamed)
            .Select(a => a.NameColonEquals.Name.Identifier.Text)
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        var requiresCompareMethod = _visualBasicEqualityComparison.OptionCompareTextCaseInsensitive && RequiresStringCompareMethodToBeAppended(invocationSymbol);

        if (invocationSymbol == null) {
            return Enumerable.Empty<ArgumentSyntax>();
        }

        var requiredInCs = invocationSymbol.GetParameters()
            .Select((p, i) => CreateExtraArgOrNull(p, i, vbPositionalArgs, namedArgNames, requiresCompareMethod));
        return requiredInCs.Where(x => x != null);

    }

    private ArgumentSyntax CreateExtraArgOrNull(IParameterSymbol p, int i, int vbPositionalArgs, ImmutableHashSet<string> namedArgNames, bool requiresCompareMethod)
    {
        if (i < vbPositionalArgs || namedArgNames.Contains(p.Name) || !p.HasExplicitDefaultValue) {
            return null;
        }

        var csRefKind = CommonConversions.GetCsRefKind(p);
        if (csRefKind != RefKind.None) {
            return CreateOptionalRefArg(p, csRefKind);
        }
        if (requiresCompareMethod && p.Type.Name == "CompareMethod") return (ArgumentSyntax)CommonConversions.CsSyntaxGenerator.Argument(p.Name, RefKind.None, _visualBasicEqualityComparison.CompareMethodExpression);
        return null;
    }

    private ArgumentSyntax CreateOptionalRefArg(IParameterSymbol p, RefKind refKind)
    {
        string prefix = $"arg{p.Name}";
        var local = _typeContext.PerScopeState.Hoist(new AdditionalDeclaration(prefix, CommonConversions.Literal(p.ExplicitDefaultValue), CommonConversions.GetTypeSyntax(p.Type)));
        return (ArgumentSyntax)CommonConversions.CsSyntaxGenerator.Argument(p.Name, refKind, local.IdentifierName);
    }

    private RefConversion NeedsVariableForArgument(VBasic.Syntax.ArgumentSyntax node, RefKind refKind)
    {
        if (refKind == RefKind.None) return RefConversion.Inline;
        if (!(node is VBSyntax.SimpleArgumentSyntax sas)) return RefConversion.PreAssigment;
        var expression = sas.Expression.SkipIntoParens();

        return GetRefConversion(expression);

        RefConversion GetRefConversion(VBSyntax.ExpressionSyntax expression)
        {
            var symbolInfo = GetSymbolInfoInDocument<ISymbol>(expression);
            if (symbolInfo is IPropertySymbol propertySymbol) {
                return propertySymbol.IsReadOnly ? RefConversion.PreAssigment : RefConversion.PreAndPostAssignment;
            }

            if (DeclaredInUsing(symbolInfo)) return RefConversion.PreAssigment;

            if (expression is VBasic.Syntax.IdentifierNameSyntax || expression is VBSyntax.MemberAccessExpressionSyntax ||
                IsRefArrayAcces(expression)) {

                var typeInfo = _semanticModel.GetTypeInfo(expression);
                bool isTypeMismatch = typeInfo.Type == null || !typeInfo.Type.Equals(typeInfo.ConvertedType, SymbolEqualityComparer.IncludeNullability);

                if (isTypeMismatch) {
                    return RefConversion.PreAndPostAssignment;
                }

                return RefConversion.Inline;
            }

            return RefConversion.PreAssigment;
        }

        bool IsRefArrayAcces(VBSyntax.ExpressionSyntax expression)
        {
            if (!(expression is VBSyntax.InvocationExpressionSyntax ies)) return false;
            return _semanticModel.GetOperation(ies).IsArrayElementAccess() && GetRefConversion(ies.Expression) == RefConversion.Inline;
        }
    }

    private static bool DeclaredInUsing(ISymbol symbolInfo)
    {
        return symbolInfo?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax()?.Parent?.Parent?.IsKind(VBasic.SyntaxKind.UsingStatement) == true;
    }

    /// <summary>
    /// https://github.com/icsharpcode/CodeConverter/issues/324
    /// https://github.com/icsharpcode/CodeConverter/issues/310
    /// </summary>
    private enum RefConversion
    {
        /// <summary>
        /// e.g. Normal field, parameter or local
        /// </summary>
        Inline,
        /// <summary>
        /// Needs assignment before and/or after
        /// e.g. Method/Property result
        /// </summary>
        PreAssigment,
        /// <summary>
        /// Needs assignment before and/or after
        /// i.e. Property
        /// </summary>
        PreAndPostAssignment
    }

    private ISymbol GetInvocationSymbol(SyntaxNode invocation)
    {
        var symbol = invocation.TypeSwitch(
            (VBSyntax.InvocationExpressionSyntax e) => _semanticModel.GetSymbolInfo(e).ExtractBestMatch<ISymbol>(),
            (VBSyntax.ObjectCreationExpressionSyntax e) => _semanticModel.GetSymbolInfo(e).ExtractBestMatch<ISymbol>(),
            (VBSyntax.RaiseEventStatementSyntax e) => _semanticModel.GetSymbolInfo(e.Name).ExtractBestMatch<ISymbol>(),
            (VBSyntax.MidExpressionSyntax _) => _semanticModel.Compilation.GetTypeByMetadataName("Microsoft.VisualBasic.CompilerServices.StringType")?.GetMembers("MidStmtStr").FirstOrDefault(),
            _ => { throw new NotSupportedException(); }
        );
        return symbol;
    }

    private async Task<AttributeArgumentSyntax> ToAttributeArgumentAsync(VBasic.Syntax.ArgumentSyntax arg)
    {
        if (!(arg is VBasic.Syntax.SimpleArgumentSyntax))
            throw new NotSupportedException();
        var a = (VBasic.Syntax.SimpleArgumentSyntax)arg;
        var attr = SyntaxFactory.AttributeArgument(await a.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor));
        if (a.IsNamed) {
            attr = attr.WithNameEquals(SyntaxFactory.NameEquals(await a.NameColonEquals.Name.AcceptAsync<IdentifierNameSyntax>(TriviaConvertingExpressionVisitor)));
        }
        return attr;
    }

    private SyntaxToken ConvertIdentifier(SyntaxToken identifierIdentifier, bool isAttribute = false)
    {
        return CommonConversions.ConvertIdentifier(identifierIdentifier, isAttribute);
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
               node.ArgumentList?.Arguments.OnlyOrDefault()?.GetExpression() is {} arg &&
               _semanticModel.GetTypeInfo(arg).Type.IsNumericType();
    }

    private async Task<ArgumentListSyntax> ConvertArgumentListOrEmptyAsync(SyntaxNode node, VBSyntax.ArgumentListSyntax argumentList)
    {
        return await argumentList.AcceptAsync<ArgumentListSyntax>(TriviaConvertingExpressionVisitor) ?? CreateArgList(_semanticModel.GetSymbolInfo(node).Symbol);
    }

    private ArgumentListSyntax CreateArgList(ISymbol invocationSymbol)
    {
        return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
            GetAdditionalRequiredArgs(invocationSymbol, Array.Empty<VBSyntax.ArgumentSyntax>()))
        );
    }

    private async Task<CSharpSyntaxNode> SubstituteVisualBasicMethodOrNullAsync(VBSyntax.InvocationExpressionSyntax node, ISymbol symbol)
    {
        ExpressionSyntax cSharpSyntaxNode = null;
        if (symbol?.ContainingNamespace.MetadataName == nameof(Microsoft.VisualBasic) && symbol.Name == "ChrW" || symbol?.Name == "Chr") {
            var vbArg = node.ArgumentList.Arguments.Single().GetExpression();
            var constValue = _semanticModel.GetConstantValue(vbArg);
            if (IsCultureInvariant(symbol, constValue)) {
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


    private static bool RequiresStringCompareMethodToBeAppended(ISymbol symbol) =>
        symbol?.ContainingType.Name == nameof(Strings) &&
        symbol.ContainingType.ContainingNamespace.Name == nameof(Microsoft.VisualBasic) &&
        symbol.ContainingType.ContainingNamespace.ContainingNamespace.Name == nameof(Microsoft) &&
        symbol.Name is "InStr" or "InStrRev" or "Replace" or "Split" or "StrComp";

    /// <summary>
    /// https://github.com/icsharpcode/CodeConverter/issues/745
    /// </summary>
    private static bool IsCultureInvariant(ISymbol symbol, Optional<object> constValue) =>
        symbol.Name == "ChrW" || symbol.Name == "Chr" && constValue.HasValue && Convert.ToUInt64(constValue.Value, CultureInfo.InvariantCulture) <= 127;

    private CSharpSyntaxNode AddEmptyArgumentListIfImplicit(SyntaxNode node, ExpressionSyntax id)
    {
        if (_semanticModel.SyntaxTree != node.SyntaxTree) return id;
        return _semanticModel.GetOperation(node) switch {
            IInvocationOperation invocation => SyntaxFactory.InvocationExpression(id, CreateArgList(invocation.TargetMethod)),
            IPropertyReferenceOperation propReference when propReference.Property.Parameters.Any() => SyntaxFactory.InvocationExpression(id, CreateArgList(propReference.Property)),
            _ => id
        };
    }

    /// <summary>
    /// The pre-expansion phase <see cref="DocumentExtensions.WithExpandedRootAsync(Document, System.Threading.CancellationToken)"/> should handle this for compiling nodes.
    /// This is mainly targeted at dealing with missing semantic info.
    /// </summary>
    /// <returns></returns>
    private ExpressionSyntax QualifyNode(SyntaxNode node, SimpleNameSyntax left)
    {
        var nodeSymbolInfo = GetSymbolInfoInDocument<ISymbol>(node);
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

    private bool ContextImplicitlyQualfiesSymbol(SyntaxNode syntaxNodeContext, INamespaceOrTypeSymbol symbolToCheck)
    {
        return symbolToCheck is INamespaceSymbol ns && ns.IsGlobalNamespace ||
               EnclosingTypeImplicitlyQualifiesSymbol(syntaxNodeContext, symbolToCheck);
    }

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

    /// <returns>The ISymbol if available in this document, otherwise null</returns>
    /// <remarks>It's possible to use _semanticModel.GetSpeculativeSymbolInfo(...) if you know (or can approximate) the position where the symbol would have been in the original document.</remarks>
    private TSymbol GetSymbolInfoInDocument<TSymbol>(SyntaxNode node) where TSymbol: class, ISymbol
    {
        return _semanticModel.SyntaxTree == node.SyntaxTree ? _semanticModel.GetSymbolInfo(node).ExtractBestMatch<TSymbol>(): null;
    }
}