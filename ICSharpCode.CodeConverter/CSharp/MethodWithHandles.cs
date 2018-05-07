using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ICSharpCode.CodeConverter.Util;

namespace ICSharpCode.CodeConverter.CSharp
{
    public class MethodWithHandles
    {
        public SyntaxToken MethodCSharpId { get; }
        public List<(SyntaxToken, SyntaxToken)> HandledEventCSharpIds { get; }

        public MethodWithHandles(SyntaxToken methodCSharpId, List<(SyntaxToken, SyntaxToken)> handledEventCSharpIds)
        {
            MethodCSharpId = methodCSharpId;
            HandledEventCSharpIds = handledEventCSharpIds;
        }

        public static IEnumerable<MemberDeclarationSyntax> GetDeclarationsForFieldBackedProperty(
            VariableDeclarationSyntax decl, SyntaxTokenList convertedModifiers,
            SyntaxList<AttributeListSyntax> attributes,
            List<MethodWithHandles> methodsWithHandles)
        {
            // It should be safe to use the underscore name since in VB the compiler generates a backing field with that name, and errors if you try to clash with it
            var propertyToBackingFieldMapping = decl.Variables.Select(v => v.Identifier.Text)
                .ToDictionary(n => n, n => SyntaxFactory.Identifier("_" + n));
            var fieldDecl = decl.WithVariables(SyntaxFactory.SeparatedList(decl.Variables.Select(v =>
                v.WithIdentifier(propertyToBackingFieldMapping[v.Identifier.Text])
            )));
            var fieldModifiers = SyntaxFactory.TokenList(new[] {SyntaxFactory.Token(SyntaxKind.PrivateKeyword)}.Concat(convertedModifiers.Where(m => !m.IsCsVisibility(false))));
            yield return SyntaxFactory.FieldDeclaration(attributes, fieldModifiers, fieldDecl);
            foreach (var variable in decl.Variables) {
                yield return GetDeclarationsForFieldBackedProperty(methodsWithHandles, attributes, convertedModifiers,
                    decl.Type, variable.Identifier,
                    propertyToBackingFieldMapping[variable.Identifier.Text]);
            }
        }

        private static PropertyDeclarationSyntax GetDeclarationsForFieldBackedProperty(List<MethodWithHandles> methods,
            SyntaxList<AttributeListSyntax> attributes, SyntaxTokenList convertedModifiers, TypeSyntax typeSyntax,
            SyntaxToken propertyIdentifier, SyntaxToken fieldIdentifier)
        {
            var fieldIdSyntax = SyntaxFactory.IdentifierName(fieldIdentifier);
            var synchronizedAttribute = SyntaxFactory.List(new[] {CreateSynchronizedAttribute()});
            var propIdSyntax = SyntaxFactory.IdentifierName(propertyIdentifier);

            var returnField = SyntaxFactory.ReturnStatement(fieldIdSyntax);
            var getter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, synchronizedAttribute, SyntaxFactory.TokenList(), SyntaxFactory.Block(returnField));

            var block = CreateSetterBlockToUpdateHandles(propertyIdentifier.Text, fieldIdSyntax, methods);
            var setter = SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration, synchronizedAttribute, SyntaxFactory.TokenList(), SyntaxFactory.Block(block));

            var accessorListSyntax = SyntaxFactory.AccessorList(SyntaxFactory.List(new[] {getter, setter}));

            var propModifiers = SyntaxFactory.TokenList(convertedModifiers.Where(m => !m.IsKind(SyntaxKind.ReadOnlyKeyword)));
            return SyntaxFactory.PropertyDeclaration(attributes,
                propModifiers, typeSyntax, null, propIdSyntax.Identifier, accessorListSyntax);
        }

        private static StatementSyntax[] CreateSetterBlockToUpdateHandles(string propertyIdentifier,
            IdentifierNameSyntax fieldIdSyntax,
            List<MethodWithHandles> methods)
        {
            var assignBackingField = SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression, fieldIdSyntax,
                SyntaxFactory.IdentifierName("value")));
            return new[]
            {
                IfFieldNotNull(fieldIdSyntax, CreateHandlesUpdaters(propertyIdentifier, fieldIdSyntax, methods, SyntaxKind.SubtractAssignmentExpression)),
                assignBackingField,
                IfFieldNotNull(fieldIdSyntax, CreateHandlesUpdaters(propertyIdentifier, fieldIdSyntax, methods, SyntaxKind.AddAssignmentExpression))
            };
        }

        private static IEnumerable<StatementSyntax> CreateHandlesUpdaters(string propertyIdentifier,
            IdentifierNameSyntax fieldIdSyntax,
            List<MethodWithHandles> handlesSpecs,
            SyntaxKind assignmentExpressionKind)
        {
            return handlesSpecs.SelectMany(hs => hs.CreateHandlesUpdater(propertyIdentifier, fieldIdSyntax, assignmentExpressionKind, hs));
        }

        private IEnumerable<ExpressionStatementSyntax> CreateHandlesUpdater(string propertyIdentifier,
            IdentifierNameSyntax fieldIdSyntax, SyntaxKind assignmentExpressionKind,
            MethodWithHandles hs)
        {
            var methodId = SyntaxFactory.IdentifierName(MethodCSharpId);
            return HandledEventCSharpIds
                .Where(h => h.Item1.Text == propertyIdentifier)
                .Select(e =>
                {
                    var handledFieldMember = SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        fieldIdSyntax, SyntaxFactory.IdentifierName(e.Item2));
                    return SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(assignmentExpressionKind,
                            handledFieldMember,
                            methodId));
                });
        }


        private static StatementSyntax IfFieldNotNull(IdentifierNameSyntax fieldIdSyntax, IEnumerable<StatementSyntax> statements)
        {
            return SyntaxFactory.IfStatement(SyntaxFactory.BinaryExpression(
                    SyntaxKind.NotEqualsExpression,
                    fieldIdSyntax, SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                SyntaxFactory.Block(statements));
        }

        private static AttributeListSyntax CreateSynchronizedAttribute()
        {
            var methodImplOptions = SyntaxFactory.IdentifierName(nameof(MethodImplOptions));
            var synchronized = SyntaxFactory.IdentifierName(nameof(MethodImplOptions.Synchronized));
            var methodImplOptionsSynchronized = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, methodImplOptions, synchronized);
            var attributeArg = SyntaxFactory.AttributeArgument(methodImplOptionsSynchronized);
            var attribute = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("MethodImpl"), CommonConversions.CreateAttributeArgumentList(attributeArg));
            return SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(new[] {attribute}));
        }
    }
}