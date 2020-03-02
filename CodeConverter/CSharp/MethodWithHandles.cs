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
        private IdentifierNameSyntax _methodId;
        public SyntaxToken MethodCSharpId { get; }
        public List<(SyntaxToken EventContainerName, SyntaxToken EventSymbolName)> HandledPropertyEventCSharpIds { get; }
        public List<(SyntaxToken EventContainerName, SyntaxToken EventSymbolName)> HandledClassEventCSharpIds { get; }

        public MethodWithHandles(SyntaxToken methodCSharpId,
            List<(SyntaxToken EventContainerName, SyntaxToken EventSymbolName)> handledPropertyEventCSharpIds,
            List<(SyntaxToken, SyntaxToken)> handledClassEventCSharpIds)
        {
            MethodCSharpId = methodCSharpId;
            HandledPropertyEventCSharpIds = handledPropertyEventCSharpIds;
            HandledClassEventCSharpIds = handledClassEventCSharpIds;
            _methodId = SyntaxFactory.IdentifierName(MethodCSharpId);
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
            var fieldModifiers = SyntaxFactory.TokenList(new[] {SyntaxFactory.Token(SyntaxKind.PrivateKeyword)}.Concat(convertedModifiers.Where(m => !m.IsCsVisibility(false, false))));
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

            if (!methods.Any()) return new[] { assignBackingField };

            return new[]
            {
                IfFieldNotNull(fieldIdSyntax, CreateHandlesUpdaters(propertyIdentifier, fieldIdSyntax, methods, SyntaxKind.SubtractAssignmentExpression)),
                assignBackingField,
                IfFieldNotNull(fieldIdSyntax, CreateHandlesUpdaters(propertyIdentifier, fieldIdSyntax, methods, SyntaxKind.AddAssignmentExpression))
            };
        }

        public static IEnumerable<StatementSyntax> CreateHandlesUpdaters(string propertyIdentifier,
            IdentifierNameSyntax fieldIdSyntax,
            List<MethodWithHandles> handlesSpecs,
            SyntaxKind assignmentExpressionKind)
        {
            return handlesSpecs.SelectMany(hs => hs.CreateHandlesUpdater(propertyIdentifier, fieldIdSyntax, assignmentExpressionKind));
        }

        public IEnumerable<StatementSyntax> GetPostInitializationStatements(string propertyIdentifier,
            IdentifierNameSyntax fieldIdSyntax)
        {
            return CreateHandlesUpdater(propertyIdentifier, fieldIdSyntax, SyntaxKind.AddAssignmentExpression);
        }

        private IEnumerable<StatementSyntax> CreateHandlesUpdater(string propertyIdentifier,
            IdentifierNameSyntax fieldIdSyntax, SyntaxKind assignmentExpressionKind)
        {
            return HandledPropertyEventCSharpIds
                .Where(h => h.EventContainerName.Text == propertyIdentifier)
                .Select(e => CreateHandlesUpdater(fieldIdSyntax, e, assignmentExpressionKind));
        }

        private StatementSyntax CreateHandlesUpdater(IdentifierNameSyntax eventSource,
            (SyntaxToken EventContainerName, SyntaxToken EventSymbolName) e,
            SyntaxKind assignmentExpressionKind)
        {
            var handledFieldMember = MemberAccess(eventSource, e);
            return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(assignmentExpressionKind,
                    handledFieldMember,
                    _methodId));
        }

        private static MemberAccessExpressionSyntax MemberAccess(IdentifierNameSyntax eventSource, (SyntaxToken EventContainerName, SyntaxToken EventSymbolName) e)
        {
            return SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            eventSource, SyntaxFactory.IdentifierName(e.EventSymbolName));
        }

        public IEnumerable<(ExpressionSyntax EventField, SyntaxKind AssignmentKind, ExpressionSyntax HandlerId)> GetPreInitializeComponentEventHandlers()
        {
            return HandledClassEventCSharpIds.Select(e =>
                ((ExpressionSyntax) MemberAccess(SyntaxFactory.IdentifierName(e.EventContainerName), e), SyntaxKind.AddAssignmentExpression, (ExpressionSyntax)_methodId));
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
            var synchronized = SyntaxFactory.IdentifierName("Synchronized"); // Switch to nameof(MethodImplOptions.Synchronized) when upgrading to netstandard 2.0
            var methodImplOptionsSynchronized = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, methodImplOptions, synchronized);
            var attributeArg = SyntaxFactory.AttributeArgument(methodImplOptionsSynchronized);
            var attribute = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("MethodImpl"), CommonConversions.CreateAttributeArgumentList(attributeArg));
            return SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(new[] {attribute}));
        }
    }
}
