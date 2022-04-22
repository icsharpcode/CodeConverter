using ICSharpCode.CodeConverter.Util.FromRoslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp;

internal record struct Assignment(ExpressionSyntax Field, SyntaxKind AssignmentKind, ExpressionSyntax Initializer, bool PostAssignment = false);

internal class AdditionalInitializers
{
    private readonly bool _shouldAddInstanceConstructor;
    private readonly bool _shouldAddStaticConstructor;

    public AdditionalInitializers(VBSyntax.TypeBlockSyntax typeSyntax, INamedTypeSymbol namedTypeSybol,
        Compilation vbCompilation)
    {
        var (instanceConstructors, staticConstructors) = namedTypeSybol.GetDeclaredConstructorsInAllParts();
        var isBestPartToAddParameterlessConstructor = IsBestPartToAddParameterlessConstructor(typeSyntax, namedTypeSybol);
        _shouldAddInstanceConstructor = !instanceConstructors.Any() && isBestPartToAddParameterlessConstructor;
        _shouldAddStaticConstructor = !staticConstructors.Any() && isBestPartToAddParameterlessConstructor;
        IsBestPartToAddTypeInit = isBestPartToAddParameterlessConstructor;
        HasInstanceConstructorsOutsideThisPart = instanceConstructors.Any(c => c.DeclaringSyntaxReferences.Any(
            reference => !typeSyntax.OverlapsWith(reference)
        )) || !instanceConstructors.Any() && !isBestPartToAddParameterlessConstructor;
        DesignerGeneratedInitializeComponentOrNull = namedTypeSybol.GetDesignerGeneratedInitializeComponentOrNull(vbCompilation);
    }

    public bool HasInstanceConstructorsOutsideThisPart { get; }
    public bool IsBestPartToAddTypeInit { get; }
    public IMethodSymbol DesignerGeneratedInitializeComponentOrNull { get; }

    public List<Assignment> AdditionalStaticInitializers { get; } = new();
    public List<Assignment> AdditionalInstanceInitializers { get; } = new();

    public IReadOnlyCollection<MemberDeclarationSyntax> WithAdditionalInitializers(List<MemberDeclarationSyntax> convertedMembers, SyntaxToken parentTypeName)
    {
        var (rootInstanceConstructors, rootStaticConstructors) = convertedMembers.OfType<ConstructorDeclarationSyntax>()
            .Where(cds => !cds.Initializer.IsKind(SyntaxKind.ThisConstructorInitializer))
            .SplitOn(cds => cds.IsInStaticCsContext());

        convertedMembers = WithAdditionalInitializers(convertedMembers, parentTypeName, AdditionalInstanceInitializers, SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
            rootInstanceConstructors, _shouldAddInstanceConstructor, DesignerGeneratedInitializeComponentOrNull != null);

        convertedMembers = WithAdditionalInitializers(convertedMembers, parentTypeName,
            AdditionalStaticInitializers, SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword)), rootStaticConstructors, _shouldAddStaticConstructor, false);

        return convertedMembers;
    }

    private static List<MemberDeclarationSyntax> WithAdditionalInitializers(List<MemberDeclarationSyntax> convertedMembers,
        SyntaxToken convertIdentifier, IReadOnlyCollection<Assignment> additionalInitializers,
        SyntaxTokenList modifiers, IEnumerable<ConstructorDeclarationSyntax> constructorsEnumerable, bool addConstructor, bool addedConstructorRequiresInitializeComponent)
    {
        if (!additionalInitializers.Any() && (!addConstructor || !addedConstructorRequiresInitializeComponent)) return convertedMembers;
        var constructors = new HashSet<ConstructorDeclarationSyntax>(constructorsEnumerable);

        if (addConstructor) {
            var statements = new List<StatementSyntax>();
            if (addedConstructorRequiresInitializeComponent) {
                statements.Add(SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("InitializeComponent"))));
            }

            constructors.Add(SyntaxFactory.ConstructorDeclaration(convertIdentifier)
                .WithBody(SyntaxFactory.Block(statements.ToArray()))
                .WithModifiers(modifiers));
        }

        foreach (var constructor in constructors) {
            var newConstructor = WithAdditionalInitializers(constructor, additionalInitializers);
            ReplaceOrInsertBeforeFirstConstructor(convertedMembers, constructor, newConstructor);
        }

        return convertedMembers;
    }

    private static void ReplaceOrInsertBeforeFirstConstructor(List<MemberDeclarationSyntax> convertedMembers, ConstructorDeclarationSyntax constructor, ConstructorDeclarationSyntax newConstructor)
    {
        int existingIndex = convertedMembers.IndexOf(constructor);
        if (existingIndex > -1) {
            convertedMembers[existingIndex] = newConstructor;
        } else {
            int constructorIndex = convertedMembers.FindIndex(c => c is ConstructorDeclarationSyntax or MethodDeclarationSyntax);
            convertedMembers.Insert(constructorIndex > -1 ? constructorIndex : convertedMembers.Count, newConstructor);
        }
    }

    private static ConstructorDeclarationSyntax WithAdditionalInitializers(ConstructorDeclarationSyntax oldConstructor,
        IReadOnlyCollection<Assignment> additionalConstructorAssignments)
    {
        var preInitializerStatements = CreateAssignmentStatement(additionalConstructorAssignments.Where(x => !x.PostAssignment));
        var postInitializerStatements = CreateAssignmentStatement(additionalConstructorAssignments.Where(x => x.PostAssignment));
        var oldConstructorBody = oldConstructor.Body ?? SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(oldConstructor.ExpressionBody.Expression));
        var newConstructor = oldConstructor.WithBody(oldConstructorBody.WithStatements(
            oldConstructorBody.Statements.InsertRange(0, preInitializerStatements).AddRange(postInitializerStatements)));

        return newConstructor;
    }

    private static List<ExpressionStatementSyntax> CreateAssignmentStatement(IEnumerable<Assignment> additionalConstructorAssignments)
    {
        return additionalConstructorAssignments.Select(assignment =>
            SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                assignment.AssignmentKind, assignment.Field, assignment.Initializer))
        ).ToList();
    }

    private static bool IsBestPartToAddParameterlessConstructor(VBSyntax.TypeBlockSyntax typeSyntax, INamedTypeSymbol namedTypeSybol)
    {
        if (namedTypeSybol == null) return false;

        var bestPartToAddTo = namedTypeSybol.DeclaringSyntaxReferences
            .OrderByDescending(l => l.SyntaxTree.FilePath?.IsGeneratedFile() == false).ThenBy(l => l.GetSyntax() is VBSyntax.TypeBlockSyntax tbs && HasAttribute(tbs, "DesignerGenerated"))
            .First();
        return typeSyntax.OverlapsWith(bestPartToAddTo);
    }

    private static bool HasAttribute(VBSyntax.TypeBlockSyntax tbs, string attributeName)
    {
        return tbs.BlockStatement.AttributeLists.Any(list => list.Attributes.Any(a => a.Name.GetText().ToString().Contains(attributeName)));
    }
}