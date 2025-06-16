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
// Required for Task<CSharpSyntaxNode>
using System.Threading.Tasks;
// Required for NotImplementedException
using System;
using System.Linq;
using System.Collections.Generic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;


namespace ICSharpCode.CodeConverter.CSharp;

internal partial class ExpressionNodeVisitor : VBasic.VisualBasicSyntaxVisitor<Task<CSharpSyntaxNode>>
{
    public override async Task<CSharpSyntaxNode> VisitGetTypeExpression(VBasic.Syntax.GetTypeExpressionSyntax node)
    {
        return SyntaxFactory.TypeOfExpression(await node.Type.AcceptAsync<TypeSyntax>(TriviaConvertingExpressionVisitor));
    }

    public override async Task<CSharpSyntaxNode> VisitGlobalName(VBasic.Syntax.GlobalNameSyntax node)
    {
        return ValidSyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword));
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

    public override async Task<CSharpSyntaxNode> VisitIdentifierName(VBasic.Syntax.IdentifierNameSyntax node)
    {
        var identifier = SyntaxFactory.IdentifierName(CommonConversions.ConvertIdentifier(node.Identifier, node.GetAncestor<VBasic.Syntax.AttributeSyntax>() != null));

        bool requiresQualification = !node.Parent.IsKind(VBasic.SyntaxKind.SimpleMemberAccessExpression, VBasic.SyntaxKind.QualifiedName, VBasic.SyntaxKind.NameColonEquals, VBasic.SyntaxKind.ImportsStatement, VBasic.SyntaxKind.NamespaceStatement, VBasic.SyntaxKind.NamedFieldInitializer) ||
                                     node.Parent is VBSyntax.NamedFieldInitializerSyntax nfs && nfs.Expression == node ||
                                     node.Parent is VBasic.Syntax.MemberAccessExpressionSyntax maes && maes.Expression == node;
        var qualifiedIdentifier = requiresQualification
            ? QualifyNode(node, identifier) : identifier;

        var sym = GetSymbolInfoInDocument<ISymbol>(node); // Assumes GetSymbolInfoInDocument is available (kept in main or accessible)
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

    private async Task<CSharpSyntaxNode> AdjustForImplicitInvocationAsync(SyntaxNode node, ExpressionSyntax qualifiedIdentifier)
    {
        bool nonExecutableNode = node.IsParentKind(VBasic.SyntaxKind.QualifiedName);
        if (nonExecutableNode || _semanticModel.SyntaxTree != node.SyntaxTree) return qualifiedIdentifier;

        if (await TryConvertParameterizedPropertyAsync(_semanticModel.GetOperation(node), node, qualifiedIdentifier) is {} invocation) // Assumes TryConvertParameterizedPropertyAsync is accessible
        {
            return invocation;
        }

        return AddEmptyArgumentListIfImplicit(node, qualifiedIdentifier);
    }

    private CSharpSyntaxNode AddEmptyArgumentListIfImplicit(SyntaxNode node, ExpressionSyntax id)
    {
        if (_semanticModel.SyntaxTree != node.SyntaxTree) return id;
        return _semanticModel.GetOperation(node) switch {
            IInvocationOperation invocation => SyntaxFactory.InvocationExpression(id, CreateArgList(invocation.TargetMethod)), // Assumes CreateArgList is accessible
            IPropertyReferenceOperation propReference when propReference.Property.Parameters.Any() => SyntaxFactory.InvocationExpression(id, CreateArgList(propReference.Property)), // Assumes CreateArgList is accessible
            _ => id
        };
    }

    public override async Task<CSharpSyntaxNode> VisitQualifiedName(VBasic.Syntax.QualifiedNameSyntax node)
    {
        var symbol = GetSymbolInfoInDocument<ITypeSymbol>(node); // Assumes GetSymbolInfoInDocument is accessible
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
        var symbol = GetSymbolInfoInDocument<ISymbol>(node); // Assumes GetSymbolInfoInDocument is accessible
        var genericNameSyntax = await GenericNameAccountingForReducedParametersAsync(node, symbol);
        return await AdjustForImplicitInvocationAsync(node, genericNameSyntax);
    }

    private async Task<SimpleNameSyntax> GenericNameAccountingForReducedParametersAsync(VBSyntax.GenericNameSyntax node, ISymbol symbol)
    {
        SyntaxToken convertedIdentifier = CommonConversions.ConvertIdentifier(node.Identifier); // Assumes CommonConversions is accessible
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

    private ITypeSymbol[] GetOrNullAllTypeArgsIncludingInferred(IMethodSymbol vbMethod)
    {
        if (!(CommonConversions.GetCsOriginalSymbolOrNull(vbMethod) is IMethodSymbol csSymbolWithInferredTypeParametersSet)) return null; // Assumes CommonConversions is accessible
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

    private ExpressionSyntax QualifyNode(SyntaxNode node, SimpleNameSyntax left)
    {
        var nodeSymbolInfo = GetSymbolInfoInDocument<ISymbol>(node); // Assumes GetSymbolInfoInDocument is accessible
        if (left != null &&
            nodeSymbolInfo != null &&
            nodeSymbolInfo.MatchesKind(SymbolKind.TypeParameter) == false &&
            nodeSymbolInfo.ContainingSymbol is INamespaceOrTypeSymbol containingSymbol &&
            !ContextImplicitlyQualfiesSymbol(node, containingSymbol)) {

            if (containingSymbol is ITypeSymbol containingTypeSymbol &&
                !nodeSymbolInfo.IsConstructor()) {
                var qualification = CommonConversions.GetTypeSyntax(containingTypeSymbol); // Assumes CommonConversions is accessible
                return Qualify(qualification.ToString(), left);
            }

            if (nodeSymbolInfo.IsNamespace()) {
                var qualification = containingSymbol.ToCSharpDisplayString();
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
        ISymbol typeContext = syntaxNodeContext.GetEnclosingDeclaredTypeSymbol(_semanticModel); // Assumes _semanticModel is accessible
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
}
