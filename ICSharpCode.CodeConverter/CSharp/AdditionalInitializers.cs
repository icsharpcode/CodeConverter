using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using ExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    public class AdditionalInitializers
    {
        public Dictionary<string, ExpressionSyntax> AdditionalStaticInitializers { get; } = new Dictionary<string, ExpressionSyntax>();
        public Dictionary<string, ExpressionSyntax> AdditionalInstanceInitializers { get; } = new Dictionary<string, ExpressionSyntax>();

        public IReadOnlyCollection<MemberDeclarationSyntax> WithAdditionalInitializers(ITypeSymbol parentType,
            List<MemberDeclarationSyntax> convertedMembers, SyntaxToken parentTypeName)
        {
            var constructorsInAllParts = parentType?.GetMembers().OfType<IMethodSymbol>().Where(m => m.IsConstructor()).ToList();
            var hasInstanceConstructors = constructorsInAllParts?.Any(c => !c.IsStatic) == true;
            var hasStaticConstructors = constructorsInAllParts?.Any(c => c.IsStatic) == true;

            var rootConstructors = convertedMembers.OfType<ConstructorDeclarationSyntax>()
                .Where(cds => !cds.Initializer.IsKind(SyntaxKind.ThisConstructorInitializer))
                .ToLookup(cds => cds.IsInStaticCsContext());

            convertedMembers = WithAdditionalInitializers(convertedMembers, parentTypeName, AdditionalInstanceInitializers, SyntaxFactory.TokenList(), rootConstructors[false], !hasInstanceConstructors);

            convertedMembers = WithAdditionalInitializers(convertedMembers, parentTypeName,
                AdditionalStaticInitializers, SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword)), rootConstructors[true], !hasStaticConstructors);

            return convertedMembers;
        }

        private List<MemberDeclarationSyntax> WithAdditionalInitializers(List<MemberDeclarationSyntax> convertedMembers,
            SyntaxToken convertIdentifier, Dictionary<string, ExpressionSyntax> additionalInitializers,
            SyntaxTokenList modifiers, IEnumerable<ConstructorDeclarationSyntax> constructorsEnumerable, bool addConstructor)
        {
            if (!additionalInitializers.Any()) return convertedMembers;
            var constructors = new HashSet<ConstructorDeclarationSyntax>(constructorsEnumerable);
            convertedMembers = convertedMembers.Except(constructors).ToList();
            if (addConstructor) {
                constructors.Add(SyntaxFactory.ConstructorDeclaration(convertIdentifier)
                    .WithBody(SyntaxFactory.Block())
                    .WithModifiers(modifiers));
            }
            foreach (var constructor in constructors) {
                var newConstructor = WithAdditionalInitializers(constructor, additionalInitializers);
                convertedMembers.Insert(0, newConstructor);
            }

            return convertedMembers;
        }

        private ConstructorDeclarationSyntax WithAdditionalInitializers(ConstructorDeclarationSyntax oldConstructor,
            Dictionary<string, ExpressionSyntax> additionalInitializers)
        {
            var staticInitializerStatements = additionalInitializers.Select(kvp =>
                SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName(kvp.Key), kvp.Value))
            ).ToList();
            var oldConstructorBody = oldConstructor.Body ?? SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(oldConstructor.ExpressionBody.Expression));
            var newConstructor = oldConstructor.WithBody(oldConstructorBody.WithStatements(
                oldConstructorBody.Statements.InsertRange(0, staticInitializerStatements)));

            return newConstructor;
        }
    }
}