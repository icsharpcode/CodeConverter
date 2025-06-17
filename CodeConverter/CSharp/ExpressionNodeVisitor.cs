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
using System.Linq;
using System.Collections.Generic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

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
    private readonly XmlImportContext _xmlImportContext;
    private readonly ITypeContext _typeContext;

    // Fields defined in other partial classes, but initialized by the constructor here
    private readonly LambdaConverter _lambdaConverter;
    private readonly IOperatorConverter _operatorConverter;
    private readonly VisualBasicEqualityComparison _visualBasicEqualityComparison;
    private readonly QueryConverter _queryConverter;
    private readonly Lazy<IReadOnlyDictionary<ITypeSymbol, string>> _convertMethodsLookupByReturnType;
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
        _convertMethodsLookupByReturnType =
            new Lazy<IReadOnlyDictionary<ITypeSymbol, string>>(() => CreateConvertMethodsLookupByReturnType(semanticModel));
    }

    private static IReadOnlyDictionary<ITypeSymbol, string> CreateConvertMethodsLookupByReturnType(
        SemanticModel semanticModel)
    {
        var symbolsWithName = semanticModel.Compilation
            .GetSymbolsWithName(n => n.Equals(ConvertType.Name, StringComparison.Ordinal), SymbolFilter.Type).ToList();
        
        var convertType =
            semanticModel.Compilation.GetTypeByMetadataName(ConvertType.FullName) ??
            (ITypeSymbol)symbolsWithName.FirstOrDefault(s =>
                    s.ContainingNamespace.ToDisplayString().Equals(ConvertType.Namespace, StringComparison.Ordinal));

        if (convertType is null) return ImmutableDictionary<ITypeSymbol, string>.Empty;

        var convertMethods = convertType.GetMembers().Where(m =>
            m.Name.StartsWith("To", StringComparison.Ordinal) && m.GetParameters().Length == 1);

#pragma warning disable RS1024 // Compare symbols correctly
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

    public override async Task<CSharpSyntaxNode> VisitAwaitExpression(VBasic.Syntax.AwaitExpressionSyntax node)
    {
        return SyntaxFactory.AwaitExpression(await node.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor));
    }

    // Cast-related methods remain here as they call helpers now in Expressions.cs
    public override async Task<CSharpSyntaxNode> VisitCTypeExpression(VBasic.Syntax.CTypeExpressionSyntax node)
    {
        var csharpArg = await node.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
        var typeInfo = _semanticModel.GetTypeInfo(node.Type);
        var forceTargetType = typeInfo.ConvertedType;
        // Relies on ConvertCastExpressionAsync in Expressions.cs - this call should resolve via partial class
        return CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Expression, csharpArg, forceTargetType: forceTargetType, defaultToCast: true).AddParens();
    }

    public override async Task<CSharpSyntaxNode> VisitDirectCastExpression(VBasic.Syntax.DirectCastExpressionSyntax node)
    {
        // Relies on ConvertCastExpressionAsync in Expressions.cs
        return await ConvertCastExpressionAsync(node, castToOrNull: node.Type);
    }

    public override async Task<CSharpSyntaxNode> VisitPredefinedCastExpression(VBasic.Syntax.PredefinedCastExpressionSyntax node)
    {
        // Relies on WithRemovedRedundantConversionOrNullAsync in Expressions.cs
        var simplifiedOrNull = await WithRemovedRedundantConversionOrNullAsync(node, node.Expression);
        if (simplifiedOrNull != null) return simplifiedOrNull;

        var expressionSyntax = await node.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
        if (node.Keyword.IsKind(VBasic.SyntaxKind.CDateKeyword)) {
            _extraUsingDirectives.Add("Microsoft.VisualBasic.CompilerServices");
            return SyntaxFactory.InvocationExpression(SyntaxFactory.ParseExpression("Conversions.ToDate"), SyntaxFactory.ArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(expressionSyntax))));
        }

        var withConversion = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Expression, expressionSyntax, false, true, forceTargetType: _semanticModel.GetTypeInfo(node).Type);
        return node.ParenthesizeIfPrecedenceCouldChange(withConversion);
    }

    public override async Task<CSharpSyntaxNode> VisitTryCastExpression(VBasic.Syntax.TryCastExpressionSyntax node)
    {
        return node.ParenthesizeIfPrecedenceCouldChange(SyntaxFactory.BinaryExpression(
            SyntaxKind.AsExpression,
            await node.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor),
            await node.Type.AcceptAsync<TypeSyntax>(TriviaConvertingExpressionVisitor)
        ));
    }
    
    // General Helpers
    private SyntaxToken ConvertIdentifier(SyntaxToken identifierIdentifier, bool isAttribute = false) => CommonConversions.ConvertIdentifier(identifierIdentifier, isAttribute);
    private static CSharpSyntaxNode ReplaceRightmostIdentifierText(CSharpSyntaxNode expr, SyntaxToken idToken, string overrideIdentifier) => expr.ReplaceToken(idToken, SyntaxFactory.Identifier(overrideIdentifier).WithTriviaFrom(idToken).WithAdditionalAnnotations(idToken.GetAnnotations()));

    private TSymbol GetSymbolInfoInDocument<TSymbol>(SyntaxNode node) where TSymbol: class, ISymbol => _semanticModel.SyntaxTree == node.SyntaxTree ? _semanticModel.GetSymbolInfo(node).ExtractBestMatch<TSymbol>(): null;
}
