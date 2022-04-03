using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using CSSyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;


namespace ICSharpCode.CodeConverter.CSharp;

internal class XmlImportContext
{
    private readonly List<FieldDeclarationSyntax> _xNamespaceFields = new();
    public IdentifierNameSyntax HelperClassUniqueIdentifierName { get; }
    public IdentifierNameSyntax HelperClassShortIdentifierName => SyntaxFactory.IdentifierName("XmlImports");

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

    public IdentifierNameSyntax DefaultIdentifierName => SyntaxFactory.IdentifierName("Default");

    public bool HasDefaultImport => _xNamespaceFields.Any(x => x.Declaration.Variables.Single().Identifier.IsEquivalentTo(DefaultIdentifierName.Identifier));

    public ClassDeclarationSyntax GenerateHelper()
    {
        if (!HasImports) return null;

        var xAttributeList = SyntaxFactory.FieldDeclaration(
            SyntaxFactory.List<AttributeListSyntax>(),
            SyntaxFactory.TokenList(SyntaxFactory.Token(CSSyntaxKind.PrivateKeyword), SyntaxFactory.Token(CSSyntaxKind.StaticKeyword)),
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


        var boilerplate = SyntaxFactory.ParseStatement(@"
                TContainer Apply<TContainer>(TContainer x) where TContainer : XContainer
                {
                    foreach (var d in x.Descendants()) {
                        foreach (var n in namespaceAttributes) {
                            var a = d.Attribute(n.Name);
                            if (a != null && a.Value == n.Value) {
                                a.Remove();
                            }
                        }
                    }
                    x.Add(namespaceAttributes);
                    return x;
                }") as LocalFunctionStatementSyntax;

        var applyMethod = SyntaxFactory.MethodDeclaration(
            SyntaxFactory.List<AttributeListSyntax>(),
            SyntaxFactory.TokenList(SyntaxFactory.Token(CSSyntaxKind.InternalKeyword), SyntaxFactory.Token(CSSyntaxKind.StaticKeyword)),
            boilerplate.ReturnType,
            null,
            boilerplate.Identifier,
            boilerplate.TypeParameterList,
            boilerplate.ParameterList,
            boilerplate.ConstraintClauses,
            boilerplate.Body,
            boilerplate.ExpressionBody);

        return SyntaxFactory.ClassDeclaration(
            SyntaxFactory.List<AttributeListSyntax>(),
            SyntaxFactory.TokenList(SyntaxFactory.Token(CSSyntaxKind.InternalKeyword), SyntaxFactory.Token(CSSyntaxKind.StaticKeyword)),
            HelperClassUniqueIdentifierName.Identifier,
            null, null,
            SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
            SyntaxFactory.List(_xNamespaceFields.Concat<MemberDeclarationSyntax>(xAttributeList).Concat(applyMethod))
        );
            
    }

    private ExpressionSyntax BuildXmlnsAttributeName(IdentifierNameSyntax fieldIdentifierName)
    {
        var xmlns = SyntaxFactory.MemberAccessExpression(CSSyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("XNamespace"), SyntaxFactory.IdentifierName("Xmlns"));
        return SyntaxFactory.BinaryExpression(CSSyntaxKind.AddExpression, xmlns, CommonConversions.Literal(fieldIdentifierName.Identifier.ValueText));
    }
}