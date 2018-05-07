using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

        public IEnumerable<ExpressionStatementSyntax> CreateHandlesUpdater(string propertyIdentifier,
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

        public static PropertyDeclarationSyntax CreateEventProxyProperty(string propertyIdentifier,
            IEnumerable<AttributeListSyntax> attributes,
            SyntaxTokenList convertedModifiers, TypeSyntax typeSyntax, SyntaxToken backingFieldIdentifier,
            List<MethodWithHandles> methods)
        {
            var fieldIdSyntax = SyntaxFactory.IdentifierName(backingFieldIdentifier);
            var synchronizedAttribute = SyntaxFactory.List(new[] {CreateSynchronizedAttribute()});
            var propIdSyntax = SyntaxFactory.IdentifierName(propertyIdentifier);

            var returnField = SyntaxFactory.ReturnStatement(fieldIdSyntax);
            var getter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, synchronizedAttribute, SyntaxFactory.TokenList(), SyntaxFactory.Block(returnField));

            var block = CreateSetterBlockToUpdateHandles(propertyIdentifier, fieldIdSyntax, methods);
            var setter = SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration, synchronizedAttribute, SyntaxFactory.TokenList(), SyntaxFactory.Block(block));

            var accessorListSyntax = SyntaxFactory.AccessorList(SyntaxFactory.List(new[] {getter, setter}));

            var propModifiers = SyntaxFactory.TokenList(convertedModifiers.Where(m => !m.IsKind(SyntaxKind.ReadOnlyKeyword)));
            var propAttributes = SyntaxFactory.List(attributes);
            return SyntaxFactory.PropertyDeclaration(propAttributes,
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