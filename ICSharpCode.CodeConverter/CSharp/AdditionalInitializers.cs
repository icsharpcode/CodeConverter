using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    public class AdditionalInitializers
    {
        public Dictionary<string, ExpressionSyntax> AdditionalStaticInitializers { get; } = new Dictionary<string, ExpressionSyntax>();
        public Dictionary<string, ExpressionSyntax> AdditionalInstanceInitializers { get; } = new Dictionary<string, ExpressionSyntax>();

        public IReadOnlyCollection<MemberDeclarationSyntax> WithAdditionalInitializers(List<MemberDeclarationSyntax> convertedMembers, SyntaxToken parentTypeName)
        {
            var parameterlessConstructors = convertedMembers.OfType<ConstructorDeclarationSyntax>()
                .Where(cds => !cds.Initializer.IsKind(SyntaxKind.ThisConstructorInitializer))
                .ToLookup(cds => cds.IsInStaticCsContext());

            convertedMembers = WithAdditionalInitializers(convertedMembers, parameterlessConstructors, parentTypeName, AdditionalInstanceInitializers, SyntaxFactory.TokenList(), false);
            return WithAdditionalInitializers(convertedMembers, parameterlessConstructors, parentTypeName, AdditionalStaticInitializers, SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword)), true);
        }

        private List<MemberDeclarationSyntax> WithAdditionalInitializers(List<MemberDeclarationSyntax> convertedMembers,
            ILookup<bool, ConstructorDeclarationSyntax> parameterlessConstructors,
            SyntaxToken convertIdentifier, Dictionary<string, ExpressionSyntax> additionalInitializers,
            SyntaxTokenList modifiers, bool isStatic)
        {
            if (!additionalInitializers.Any()) return convertedMembers;
            var constructors = new HashSet<ConstructorDeclarationSyntax>(parameterlessConstructors[isStatic]);
            convertedMembers = convertedMembers.Except(constructors).ToList();
            if (!constructors.Any()) {
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