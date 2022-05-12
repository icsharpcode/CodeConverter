using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using CSSyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;


namespace ICSharpCode.CodeConverter.CSharp;

internal class XmlImportContext
{
    private readonly List<FieldDeclarationSyntax> _xNamespaceFields = new();
    public IdentifierNameSyntax HelperClassUniqueIdentifierName { get; }
    public static IdentifierNameSyntax HelperClassShortIdentifierName => SyntaxFactory.IdentifierName("XmlImports");

    public XmlImportContext(Document document)            
    {
        HelperClassUniqueIdentifierName = SyntaxFactory.IdentifierName(HelperClassShortIdentifierName + String.Join("", document.Name.Where(c=>Char.IsLetterOrDigit(c))));
    }

    public async Task<XmlImportContext> HandleImportsAsync(List<VBSyntax.ImportsClauseSyntax> importsClauses, Func<VBSyntax.XmlNamespaceImportsClauseSyntax, Task<FieldDeclarationSyntax>> declarationConversion)
    {
        var xmlImports = importsClauses.OfType<VBSyntax.XmlNamespaceImportsClauseSyntax>().ToList();            
        importsClauses.RemoveAll(x => x is VBSyntax.XmlNamespaceImportsClauseSyntax);
        _xNamespaceFields.AddRange(await xmlImports.SelectAsync(declarationConversion));
        return this;
    }

    public bool HasImports => _xNamespaceFields.Any();

    public static IdentifierNameSyntax DefaultIdentifierName => SyntaxFactory.IdentifierName("Default");

    public bool HasDefaultImport => _xNamespaceFields.Any(x => x.Declaration.Variables.Single().Identifier.IsEquivalentTo(DefaultIdentifierName.Identifier));

    public ClassDeclarationSyntax GenerateHelper()
    {
        if (!HasImports) return null;

        var xAttributeList = SyntaxFactory.FieldDeclaration(
            SyntaxFactory.List<AttributeListSyntax>(),
            SyntaxFactory.TokenList(SyntaxFactory.Token(CSSyntaxKind.PrivateKeyword), SyntaxFactory.Token(CSSyntaxKind.StaticKeyword), SyntaxFactory.Token(CSSyntaxKind.ReadOnlyKeyword)),
            CommonConversions.CreateVariableDeclarationAndAssignment(
                "namespaceAttributes", SyntaxFactory.InitializerExpression(
                    CSSyntaxKind.ArrayInitializerExpression,
                    SyntaxFactory.SeparatedList<ExpressionSyntax>(
                        from x in _xNamespaceFields
                        let fieldIdentifierName = SyntaxFactory.IdentifierName(x.Declaration.Variables.Single().Identifier)
                        let namespaceNameExpression = SyntaxFactory.MemberAccessExpression(CSSyntaxKind.SimpleMemberAccessExpression, fieldIdentifierName, SyntaxFactory.IdentifierName("NamespaceName"))
                        let attributeNameExpression = fieldIdentifierName.IsEquivalentTo(DefaultIdentifierName) ? CommonConversions.Literal("xmlns") : BuildXmlnsAttributeName(fieldIdentifierName)
                        let arguments = SyntaxFactory.Argument(attributeNameExpression).Yield().Concat(SyntaxFactory.Argument(namespaceNameExpression))
                        select SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName("XAttribute")).WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments))))),
                SyntaxFactory.ArrayType(SyntaxFactory.IdentifierName("XAttribute"), SyntaxFactory.SingletonList(SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(SyntaxFactory.OmittedArraySizeExpression()))))));


        var boilerplate = new[] {
            SyntaxFactory.ParseStatement(@"
                XElement Apply(XElement x)
                {
                    foreach (var d in x.DescendantsAndSelf()) {
                        foreach (var n in namespaceAttributes) {
                            var a = d.Attribute(n.Name);
                            if (a != null && a.Value == n.Value) {
                                a.Remove();
                            }
                        }
                    }
                    x.Add(namespaceAttributes);
                    return x;
                }") as LocalFunctionStatementSyntax,
            SyntaxFactory.ParseStatement(@"
                XDocument Apply(XDocument x)
                {
                    Apply(x.Root);
                    return x;
                }") as LocalFunctionStatementSyntax,
        };

        var applyMethods = from functionStatement in boilerplate select SyntaxFactory.MethodDeclaration(
            SyntaxFactory.List<AttributeListSyntax>(),
            SyntaxFactory.TokenList(SyntaxFactory.Token(CSSyntaxKind.InternalKeyword), SyntaxFactory.Token(CSSyntaxKind.StaticKeyword)),
            functionStatement.ReturnType,
            null,
            functionStatement.Identifier,
            functionStatement.TypeParameterList,
            functionStatement.ParameterList,
            functionStatement.ConstraintClauses,
            functionStatement.Body,
            functionStatement.ExpressionBody);

        return SyntaxFactory.ClassDeclaration(
            SyntaxFactory.List<AttributeListSyntax>(),
            SyntaxFactory.TokenList(SyntaxFactory.Token(CSSyntaxKind.InternalKeyword), SyntaxFactory.Token(CSSyntaxKind.StaticKeyword)),
            HelperClassUniqueIdentifierName.Identifier,
            null, null,
            SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
            SyntaxFactory.List(_xNamespaceFields.Concat<MemberDeclarationSyntax>(xAttributeList).Concat(applyMethods))
        );
            
    }

    private static ExpressionSyntax BuildXmlnsAttributeName(IdentifierNameSyntax fieldIdentifierName)
    {
        var xmlns = SyntaxFactory.MemberAccessExpression(CSSyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("XNamespace"), SyntaxFactory.IdentifierName("Xmlns"));
        return SyntaxFactory.BinaryExpression(CSSyntaxKind.AddExpression, xmlns, CommonConversions.Literal(fieldIdentifierName.Identifier.ValueText));
    }
}