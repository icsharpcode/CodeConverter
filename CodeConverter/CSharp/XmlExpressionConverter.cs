using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp;


internal class XmlExpressionConverter
{
    private readonly XmlImportContext _xmlImportContext;
    private readonly HashSet<string> _extraUsingDirectives;

    public XmlExpressionConverter(XmlImportContext xmlImportContext, HashSet<string> extraUsingDirectives, CommentConvertingVisitorWrapper triviaConvertingExpressionVisitor)
    {
        _xmlImportContext = xmlImportContext;
        _extraUsingDirectives = extraUsingDirectives;
        TriviaConvertingExpressionVisitor = triviaConvertingExpressionVisitor;
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

        var xElementMethodName = GetXElementMethodName(node);

        ExpressionSyntax elements = node.Base != null
            ? SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                await node.Base.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor),
                ValidSyntaxFactory.IdentifierName(xElementMethodName)
            )
            : SyntaxFactory.MemberBindingExpression(
                ValidSyntaxFactory.IdentifierName(xElementMethodName)
            );

        return SyntaxFactory.InvocationExpression(elements,
            ExpressionSyntaxExtensions.CreateArgList(
                await node.Name.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor))
        );
    }
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

        if (_xmlImportContext.HasDefaultImport && node.Parent is not VBSyntax.XmlAttributeSyntax) {
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

        if (node.Token2.Text == "@") {
            return "Attributes";
        }

        if (node.Token2.Text == ".") {
            return "Descendants";
        }

        throw new NotImplementedException($"Xml member access operator: '{node.Token1}{node.Token2}{node.Token3}'");
    }

}
