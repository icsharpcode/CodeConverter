using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class AdditionalInitializers
    {
        public AdditionalInitializers(bool shouldAddTypeWideInitToThisPart)
        {
            ShouldAddTypeWideInitToThisPart = shouldAddTypeWideInitToThisPart;
        }

        public List<Assignment> AdditionalStaticInitializers { get; } = new List<Assignment>();
        public List<Assignment> AdditionalInstanceInitializers { get; } = new List<Assignment>();
        public bool ShouldAddTypeWideInitToThisPart { get; }

        public IReadOnlyCollection<MemberDeclarationSyntax> WithAdditionalInitializers(ITypeSymbol parentType,
            List<MemberDeclarationSyntax> convertedMembers, SyntaxToken parentTypeName, bool requiresInitializeComponent)
        {
            var constructorsInAllParts = parentType?.GetMembers().OfType<IMethodSymbol>().Where(m => m.IsConstructor()).ToList();
            var parameterlessConstructorsInAllParts = constructorsInAllParts?.Where(c => !c.IsImplicitlyDeclared && !c.Parameters.Any()) ?? Array.Empty<IMethodSymbol>();
            var requiresInstanceConstructor = !parameterlessConstructorsInAllParts.Any(c => !c.IsStatic);
            var requiresStaticConstructor = !parameterlessConstructorsInAllParts.Any(c => c.IsStatic);
            var rootConstructors = convertedMembers.OfType<ConstructorDeclarationSyntax>()
                .Where(cds => !cds.Initializer.IsKind(SyntaxKind.ThisConstructorInitializer))
                .ToLookup(cds => cds.IsInStaticCsContext());

            convertedMembers = WithAdditionalInitializers(convertedMembers, parentTypeName, AdditionalInstanceInitializers, SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)), rootConstructors[false], ShouldAddTypeWideInitToThisPart && requiresInstanceConstructor, requiresInitializeComponent);

            convertedMembers = WithAdditionalInitializers(convertedMembers, parentTypeName,
                AdditionalStaticInitializers, SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword)), rootConstructors[true], requiresStaticConstructor, false);

            return convertedMembers;
        }

        private List<MemberDeclarationSyntax> WithAdditionalInitializers(List<MemberDeclarationSyntax> convertedMembers,
            SyntaxToken convertIdentifier, IReadOnlyCollection<Assignment> additionalInitializers,
            SyntaxTokenList modifiers, IEnumerable<ConstructorDeclarationSyntax> constructorsEnumerable, bool addConstructor, bool addedConstructorRequiresInitializeComponent)
        {
            if (!additionalInitializers.Any() && (!addConstructor || !addedConstructorRequiresInitializeComponent)) return convertedMembers;
            var constructors = new HashSet<ConstructorDeclarationSyntax>(constructorsEnumerable);
            convertedMembers = convertedMembers.Except(constructors).ToList();
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
                convertedMembers.Insert(0, newConstructor);
            }

            return convertedMembers;
        }

        private ConstructorDeclarationSyntax WithAdditionalInitializers(ConstructorDeclarationSyntax oldConstructor,
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
    }
}