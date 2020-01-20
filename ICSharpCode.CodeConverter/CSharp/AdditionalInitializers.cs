using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    public class AdditionalInitializers
    {
        public List<(ExpressionSyntax Field, SyntaxKind AssignmentKind, ExpressionSyntax Initializer)> AdditionalStaticInitializers { get; } = new List<(ExpressionSyntax, SyntaxKind, ExpressionSyntax)>();
        public List<(ExpressionSyntax Field, SyntaxKind AssignmentKind, ExpressionSyntax Initializer)> AdditionalInstanceInitializers { get; } = new List<(ExpressionSyntax, SyntaxKind, ExpressionSyntax)>();

        public IReadOnlyCollection<MemberDeclarationSyntax> WithAdditionalInitializers(ITypeSymbol parentType,
            List<MemberDeclarationSyntax> convertedMembers, SyntaxToken parentTypeName, bool shouldAddTypeWideInitToThisPart)
        {
            var constructorsInAllParts = parentType?.GetMembers().OfType<IMethodSymbol>().Where(m => m.IsConstructor()).ToList();
            var hasInstanceConstructors = constructorsInAllParts?.Any(c => !c.IsStatic && !c.IsImplicitlyDeclared) == true;
            var hasStaticConstructors = constructorsInAllParts?.Any(c => c.IsStatic) == true;
            var requiresInitializeComponent = !hasInstanceConstructors &&
                // These constructors are implicitly declared
                parentType.GetBaseTypes().Any(t => t.ContainingNamespace.GetFullMetadataName() == "System.Windows.Forms" && (t.Name == "Form" || t.Name == "UserControl"));
            var rootConstructors = convertedMembers.OfType<ConstructorDeclarationSyntax>()
                .Where(cds => !cds.Initializer.IsKind(SyntaxKind.ThisConstructorInitializer))
                .ToLookup(cds => cds.IsInStaticCsContext());

            convertedMembers = WithAdditionalInitializers(convertedMembers, parentTypeName, AdditionalInstanceInitializers, SyntaxFactory.TokenList(), rootConstructors[false], shouldAddTypeWideInitToThisPart && !hasInstanceConstructors, requiresInitializeComponent);

            convertedMembers = WithAdditionalInitializers(convertedMembers, parentTypeName,
                AdditionalStaticInitializers, SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword)), rootConstructors[true], !hasStaticConstructors, false);

            return convertedMembers;
        }

        private List<MemberDeclarationSyntax> WithAdditionalInitializers(List<MemberDeclarationSyntax> convertedMembers,
            SyntaxToken convertIdentifier, IReadOnlyCollection<(ExpressionSyntax Field, SyntaxKind AssignmentKind, ExpressionSyntax Initializer)> additionalInitializers,
            SyntaxTokenList modifiers, IEnumerable<ConstructorDeclarationSyntax> constructorsEnumerable, bool addConstructor, bool addedConstructorRequiresInitializeComponent)
        {
            if (!additionalInitializers.Any()) return convertedMembers;
            var constructors = new HashSet<ConstructorDeclarationSyntax>(constructorsEnumerable);
            convertedMembers = convertedMembers.Except(constructors).ToList();
            if (addConstructor) {
                var statements = new List<StatementSyntax>();
                if (addedConstructorRequiresInitializeComponent) {
                    statements.Add(SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("InitializeComponent"))));
                    modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
                }
                constructors.Add(SyntaxFactory.ConstructorDeclaration(convertIdentifier)
                    .WithBody(SyntaxFactory.Block(statements.ToArray()))
                    .WithModifiers(modifiers));
            }
            foreach (var constructor in constructors) {
                var newConstructor = WithAdditionalInitializers(constructor, additionalInitializers);
                convertedMembers.Insert(0, newConstructor);
            }

            return convertedMembers;
        }

        private ConstructorDeclarationSyntax WithAdditionalInitializers(ConstructorDeclarationSyntax oldConstructor,
            IReadOnlyCollection<(ExpressionSyntax Field, SyntaxKind AssignmentKind, ExpressionSyntax Initializer)> additionalConstructorAssignments)
        {
            var staticInitializerStatements = additionalConstructorAssignments.Select(assignment =>
                SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                    assignment.AssignmentKind, assignment.Field, assignment.Initializer))
            ).ToList();
            var oldConstructorBody = oldConstructor.Body ?? SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(oldConstructor.ExpressionBody.Expression));
            var newConstructor = oldConstructor.WithBody(oldConstructorBody.WithStatements(
                oldConstructorBody.Statements.InsertRange(0, staticInitializerStatements)));

            return newConstructor;
        }
    }
}