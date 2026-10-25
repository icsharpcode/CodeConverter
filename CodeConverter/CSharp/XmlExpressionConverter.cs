using System.Xml.Linq;
using ICSharpCode.CodeConverter.Util.FromRoslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp;


internal class XmlExpressionConverter
{
    private readonly XmlImportContext _xmlImportContext;
    private readonly HashSet<string> _extraUsingDirectives;
    private readonly SemanticModel _semanticModel;
    private readonly Stack<ExpressionSyntax> _withBlockLhs;
    private readonly HashSet<string> _generatedNames;
    private readonly Dictionary<VBSyntax.ConditionalAccessExpressionSyntax, ExpressionSyntax> _conditionalAccessReceivers;
    private readonly CommonConversions _commonConversions;

    public XmlExpressionConverter(XmlImportContext xmlImportContext, HashSet<string> extraUsingDirectives, CommonConversions commonConversions,
        SemanticModel semanticModel, Stack<ExpressionSyntax> withBlockLhs, HashSet<string> generatedNames,
        Dictionary<VBSyntax.ConditionalAccessExpressionSyntax, ExpressionSyntax> conditionalAccessReceivers)
    {
        _xmlImportContext = xmlImportContext;
        _extraUsingDirectives = extraUsingDirectives;
        _commonConversions = commonConversions;
        TriviaConvertingExpressionVisitor = commonConversions.TriviaConvertingExpressionVisitor;
        _semanticModel = semanticModel;
        _withBlockLhs = withBlockLhs;
        _generatedNames = generatedNames;
        _conditionalAccessReceivers = conditionalAccessReceivers;
    }
    public async Task<CSharpSyntaxNode> ConvertXmlEmbeddedExpressionAsync(VBSyntax.XmlEmbeddedExpressionSyntax node) =>
        await node.Expression.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);

    public CommentConvertingVisitorWrapper TriviaConvertingExpressionVisitor { get; }

    public async Task<CSharpSyntaxNode> ConvertXmlDocumentAsync(VBasic.Syntax.XmlDocumentSyntax node)
    {
        _extraUsingDirectives.Add("System.Xml.Linq");
        var arguments = SyntaxFactory.SeparatedList(
            (await node.PrecedingMisc.SelectAsync(async misc => SyntaxFactory.Argument(await misc.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor))))
            .Concat(SyntaxFactory.Argument(await node.Root.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor)).Yield())
            .Concat(await node.FollowingMisc.SelectAsync(async misc => SyntaxFactory.Argument(await misc.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor))))
        );
        return ApplyXmlImportsIfNecessary(node, SyntaxFactory.ObjectCreationExpression(ValidSyntaxFactory.IdentifierName("XDocument")).WithArgumentList(SyntaxFactory.ArgumentList(arguments)));
    }

    public async Task<CSharpSyntaxNode> ConvertXmlElementAsync(VBasic.Syntax.XmlElementSyntax node)
    {
        _extraUsingDirectives.Add("System.Xml.Linq");
        var arguments = SyntaxFactory.SeparatedList(
            SyntaxFactory.Argument(await node.StartTag.Name.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor)).Yield()
                .Concat(await node.StartTag.Attributes.SelectAsync(async attribute => SyntaxFactory.Argument(await attribute.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor))))
                .Concat(await node.Content.SelectAsync(async content => SyntaxFactory.Argument(await content.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor))))
        );
        return ApplyXmlImportsIfNecessary(node, SyntaxFactory.ObjectCreationExpression(ValidSyntaxFactory.IdentifierName("XElement")).WithArgumentList(SyntaxFactory.ArgumentList(arguments)));
    }

    public async Task<CSharpSyntaxNode> ConvertXmlEmptyElementAsync(VBSyntax.XmlEmptyElementSyntax node)
    {
        _extraUsingDirectives.Add("System.Xml.Linq");
        var arguments = SyntaxFactory.SeparatedList(
            SyntaxFactory.Argument(await node.Name.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor)).Yield()
                .Concat(await node.Attributes.SelectAsync(async attribute => SyntaxFactory.Argument(await attribute.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor))))
        );
        return ApplyXmlImportsIfNecessary(node, SyntaxFactory.ObjectCreationExpression(ValidSyntaxFactory.IdentifierName("XElement")).WithArgumentList(SyntaxFactory.ArgumentList(arguments)));
    }


    public async Task<CSharpSyntaxNode> ConvertXmlAttributeAsync(VBasic.Syntax.XmlAttributeSyntax node)
    {
        var arguments = SyntaxFactory.SeparatedList(
            SyntaxFactory.Argument(await node.Name.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor)).Yield()
                .Concat(SyntaxFactory.Argument(await node.Value.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor)).Yield())
        );
        return SyntaxFactory.ObjectCreationExpression(ValidSyntaxFactory.IdentifierName("XAttribute")).WithArgumentList(SyntaxFactory.ArgumentList(arguments));
    }

    public async Task<CSharpSyntaxNode> ConvertXmlStringAsync(VBasic.Syntax.XmlStringSyntax node) =>
        CommonConversions.Literal(string.Join("", node.TextTokens.Select(b => b.Text)));

    public async Task<CSharpSyntaxNode> ConvertXmlTextAsync(VBSyntax.XmlTextSyntax node) =>
        CommonConversions.Literal(string.Join("", node.TextTokens.Select(b => b.Text)));

    public async Task<CSharpSyntaxNode> ConvertXmlCDataSectionAsync(VBSyntax.XmlCDataSectionSyntax node)
    {
        var xcDataTypeSyntax = SyntaxFactory.ParseTypeName(nameof(XCData));
        var argumentListSyntax = CommonConversions.Literal(string.Join("", node.TextTokens.Select(b => b.Text))).Yield().CreateCsArgList();
        return SyntaxFactory.ObjectCreationExpression(xcDataTypeSyntax).WithArgumentList(argumentListSyntax);
    }

    /// <summary>
    /// https://docs.microsoft.com/en-us/dotnet/visual-basic/programming-guide/language-features/xml/accessing-xml
    /// </summary>
    public async Task<CSharpSyntaxNode> ConvertXmlMemberAccessExpressionAsync(
        VBasic.Syntax.XmlMemberAccessExpressionSyntax node)
    {
        _extraUsingDirectives.Add("System.Xml.Linq");

        if (node.Token2.Text == "@") return await ConvertAttributeAxisAsync(node);

        var xElementMethodName = GetXElementMethodName(node);

        var elements = MemberAccess(await ConvertReceiverAsync(node), xElementMethodName);

        return SyntaxFactory.InvocationExpression(elements,
            ExpressionSyntaxExtensions.CreateArgList(
                await node.Name.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor))
        );
    }
    private async Task<ExpressionSyntax> ConvertAttributeAxisAsync(VBSyntax.XmlMemberAccessExpressionSyntax node)
    {
        var conditional = node.Base == null ? NameExpressionNodeVisitor.GetConditionalAccess(node) : null;
        var receiverSyntax = node.Base ?? conditional?.Expression ?? node.GetAncestor<VBSyntax.WithBlockSyntax>()?.WithStatement.Expression;
        var receiverType = _semanticModel.GetTypeInfo(receiverSyntax).Type;
        var receiver = await ConvertReceiverAsync(node);
        var name = await node.Name.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);

        var xElementType = _semanticModel.Compilation.GetTypeByMetadataName("System.Xml.Linq.XElement");
        if (receiverType.InheritsFromOrEquals(xElementType)) {
            receiver = SyntaxFactory.ParenthesizedExpression(SyntaxFactory.CastExpression(_commonConversions.GetTypeSyntax(xElementType), receiver));
            return AttributeValue(receiver, name);
        }

        _extraUsingDirectives.Add("System.Linq");
        if (receiver != null) {
            _extraUsingDirectives.Add("System");
            receiver = SyntaxFactory.ParenthesizedExpression(SyntaxFactory.BinaryExpression(SyntaxKind.CoalesceExpression, SyntaxFactory.ParenthesizedExpression(receiver),
                SyntaxFactory.ThrowExpression(SyntaxFactory.ObjectCreationExpression(_commonConversions.GetTypeSyntax(
                        _semanticModel.Compilation.GetTypeByMetadataName("System.NullReferenceException")))
                    .WithArgumentList(SyntaxFactory.ArgumentList()))));
        }
        var parameter = ValidSyntaxFactory.IdentifierName(NameGenerator.CS.GetUniqueVariableNameInScope(_semanticModel, _generatedNames, node, "element"));
        var selector = SyntaxFactory.SimpleLambdaExpression(SyntaxFactory.Parameter(parameter.Identifier), AttributeValue(parameter, name));
        var enumerable = _commonConversions.GetTypeSyntax(_semanticModel.Compilation.GetTypeByMetadataName("System.Linq.Enumerable"));
        var selectMethod = SyntaxFactory.GenericName("Select").WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
            SyntaxFactory.SeparatedList(new[] {
                _commonConversions.GetTypeSyntax(xElementType),
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword))
            })));
        var select = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, enumerable, selectMethod),
            new[] { receiver, selector }.CreateCsArgList());
        return SyntaxFactory.InvocationExpression(MemberAccess(enumerable, "FirstOrDefault"), select.Yield().CreateCsArgList());
    }

    private async Task<ExpressionSyntax> ConvertReceiverAsync(VBSyntax.XmlMemberAccessExpressionSyntax node)
    {
        if (node.Base != null) return await node.Base.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
        if (NameExpressionNodeVisitor.GetConditionalAccess(node) is { } conditional) {
            return _conditionalAccessReceivers.TryGetValue(conditional, out var receiver) ? receiver : null;
        }
        return _withBlockLhs.Peek();
    }

    private static ExpressionSyntax AttributeValue(ExpressionSyntax receiver, ExpressionSyntax name) =>
        SyntaxFactory.ParenthesizedExpression(SyntaxFactory.CastExpression(
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
            SyntaxFactory.InvocationExpression(MemberAccess(receiver, "Attribute"), name.Yield().CreateCsArgList())));

    private static ExpressionSyntax MemberAccess(ExpressionSyntax receiver, string member) => receiver == null
        ? SyntaxFactory.MemberBindingExpression(ValidSyntaxFactory.IdentifierName(member))
        : SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, receiver, ValidSyntaxFactory.IdentifierName(member));

    public Task<CSharpSyntaxNode> ConvertXmlBracketedNameAsync(VBSyntax.XmlBracketedNameSyntax node)
    {
        return node.Name.AcceptAsync<CSharpSyntaxNode>(TriviaConvertingExpressionVisitor);
    }

    public async Task<CSharpSyntaxNode> ConvertXmlNameAsync(VBSyntax.XmlNameSyntax node)
    {
        if (node.Prefix != null) {
            switch (node.Prefix.Name.ValueText) {
                case "xml":
                case "xmlns":
                    return SyntaxFactory.BinaryExpression(
                        SyntaxKind.AddExpression,
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            ValidSyntaxFactory.IdentifierName("XNamespace"),
                            ValidSyntaxFactory.IdentifierName(node.Prefix.Name.ValueText.ToPascalCase())
                        ),
                        SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(node.LocalName.Text))
                    );
                default:
                    return SyntaxFactory.BinaryExpression(
                        SyntaxKind.AddExpression,
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            XmlImportContext.HelperClassShortIdentifierName,
                            ValidSyntaxFactory.IdentifierName(node.Prefix.Name.ValueText)
                        ),
                        SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(node.LocalName.Text))
                    );
            }
        }

        var nameParent = node.Parent is VBSyntax.XmlBracketedNameSyntax ? node.Parent.Parent : node.Parent;
        var isAttributeName = nameParent is VBSyntax.XmlAttributeSyntax ||
                              nameParent is VBSyntax.XmlMemberAccessExpressionSyntax axis && axis.Token2.Text == "@";
        if (_xmlImportContext.HasDefaultImport && !isAttributeName) {
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


    private CSharpSyntaxNode ApplyXmlImportsIfNecessary(VBSyntax.XmlNodeSyntax vbNode, ObjectCreationExpressionSyntax creation)
    {
        if (!_xmlImportContext.HasImports || vbNode.Parent is VBSyntax.XmlNodeSyntax) return creation;
        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, XmlImportContext.HelperClassShortIdentifierName, ValidSyntaxFactory.IdentifierName("Apply")),
            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(creation))));
    }


    private static string GetXElementMethodName(VBSyntax.XmlMemberAccessExpressionSyntax node)
    {
        if (node.Token2 == default(SyntaxToken)) {
            return "Elements";
        }

        if (node.Token2.Text == ".") {
            return "Descendants";
        }

        throw new NotImplementedException($"Xml member access operator: '{node.Token1}{node.Token2}{node.Token3}'");
    }

}
