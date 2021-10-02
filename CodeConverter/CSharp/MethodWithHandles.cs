using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ICSharpCode.CodeConverter.Util;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using System.Reflection.Metadata;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class MethodWithHandles
    {
        private readonly IdentifierNameSyntax _methodId;
        private readonly SyntaxGenerator _csSyntaxGenerator;

        public SyntaxToken MethodCSharpId { get; }
        public List<(string EventContainerName, SyntaxToken EventSymbolName, IEventSymbol Event, int ParametersToDiscard)> HandledPropertyEventCSharpIds { get; }
        public List<(string EventContainerName, SyntaxToken EventSymbolName, IEventSymbol Event, int ParametersToDiscard)> HandledClassEventCSharpIds { get; }

        public MethodWithHandles(SyntaxGenerator csSyntaxGenerator,
            SyntaxToken methodCSharpId,
            List<(string EventContainerName, SyntaxToken EventSymbolName, IEventSymbol Event, int ParametersToDiscard)> handledPropertyEventCSharpIds,
            List<(string EventContainerName, SyntaxToken EventSymbolName, IEventSymbol Event, int ParametersToDiscard)> handledClassEventCSharpIds)
        {
            MethodCSharpId = methodCSharpId;
            HandledPropertyEventCSharpIds = handledPropertyEventCSharpIds;
            HandledClassEventCSharpIds = handledClassEventCSharpIds;
            _methodId = SyntaxFactory.IdentifierName(MethodCSharpId);
            _csSyntaxGenerator = csSyntaxGenerator;
        }

        public static IEnumerable<MemberDeclarationSyntax> GetDeclarationsForFieldBackedProperty(
            VariableDeclarationSyntax decl, SyntaxTokenList convertedModifiers,
            SyntaxList<AttributeListSyntax> attributes,
            IReadOnlyCollection<MethodWithHandles> methodsWithHandles)
        {
            // It should be safe to use the underscore name since in VB the compiler generates a backing field with that name, and errors if you try to clash with it
            var (nonRenamedEvents, renamedEvents) = decl.Variables
                .SplitOn(
                    v => HasEvents(methodsWithHandles, v.Identifier),
                    v => (Variable: v, NewId: SyntaxFactory.Identifier("_" + v.Identifier.Text))
                );

            if (nonRenamedEvents.Any()) {
                var nonRenamedDecl = decl.WithVariables(SyntaxFactory.SeparatedList(nonRenamedEvents.Select(v => v.Variable)));
                yield return SyntaxFactory.FieldDeclaration(attributes, convertedModifiers, nonRenamedDecl);
            }

            if (renamedEvents.Any()) {
                var nonRenamedDecl = decl.WithVariables(SyntaxFactory.SeparatedList(renamedEvents.Select(v => v.Variable.WithIdentifier(v.NewId))));
                yield return SyntaxFactory.FieldDeclaration(attributes, MakePrivate(convertedModifiers), nonRenamedDecl);
            }

            foreach (var (variable, newId) in renamedEvents) {
                yield return GetDeclarationsForFieldBackedProperty(methodsWithHandles, attributes, convertedModifiers,
                    decl.Type, variable.Identifier,
                    newId);
            }
        }

        private static SyntaxTokenList MakePrivate(SyntaxTokenList convertedModifiers)
        {
            var noVisibility = convertedModifiers.Where(m => !m.IsCsVisibility(false, false));
            return SyntaxFactory.TokenList(new[] {SyntaxFactory.Token(SyntaxKind.PrivateKeyword)}.Concat(noVisibility));
        }

        private static bool HasEvents(IReadOnlyCollection<MethodWithHandles> methodsWithHandles, SyntaxToken propertyId) => 
            methodsWithHandles.Any(m => m.GetPropertyEvents(propertyId.Text).Any());

        private static PropertyDeclarationSyntax GetDeclarationsForFieldBackedProperty(IReadOnlyCollection<MethodWithHandles> methods,
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
            IReadOnlyCollection<MethodWithHandles> methods)
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

        private static IEnumerable<StatementSyntax> CreateHandlesUpdaters(string propertyIdentifier,
            IdentifierNameSyntax fieldIdSyntax,
            IReadOnlyCollection<MethodWithHandles> handlesSpecs,
            SyntaxKind assignmentExpressionKind)
        {
            return handlesSpecs.SelectMany(hs => hs.CreateHandlesUpdater(propertyIdentifier, fieldIdSyntax, assignmentExpressionKind));
        }

        public IEnumerable<StatementSyntax> GetPostInitializationStatements(string propertyIdentifier,
            IdentifierNameSyntax fieldIdSyntax)
        {
            return CreateHandlesUpdater(propertyIdentifier, fieldIdSyntax, SyntaxKind.AddAssignmentExpression, true);
        }

        private IEnumerable<StatementSyntax> CreateHandlesUpdater(string propertyIdentifier,
            IdentifierNameSyntax fieldIdSyntax, SyntaxKind assignmentExpressionKind, bool requiresNewDelegate = false)
        {
            return GetPropertyEvents(propertyIdentifier)
                .Select(e => CreateHandlesUpdater(fieldIdSyntax, e, assignmentExpressionKind, requiresNewDelegate));
        }

        private IEnumerable<(string EventContainerName, SyntaxToken EventSymbolName, IEventSymbol Event, int ParametersToDiscard)> GetPropertyEvents(string propertyIdentifier)
        {
            return HandledPropertyEventCSharpIds
                .Where(h => h.EventContainerName == propertyIdentifier);
        }

        /// <summary>Use instead of <see cref="GetConstructorEventHandlers"/> for DesignerGenerated classes: https://github.com/icsharpcode/CodeConverter/issues/550</summary>
        public IEnumerable<StatementSyntax> GetInitializeComponentClassEventHandlers()
        {
            return HandledClassEventCSharpIds
                .Select(e => CreateHandlesUpdater(SyntaxFactory.ThisExpression(), e, SyntaxKind.AddAssignmentExpression, true));
        }

        /// <remarks>
        /// <paramref name="requiresNewDelegate"/> must be set to true if this statement will be within InitializeComponent, otherwise C# Winforms designer won't be able to recognize it.
        /// If a lambda has been generated to discard parameters, the C# Winforms designer will throw an exception when trying to load, but it will wprl at runtime, and it's better than silently losing events on regeneration.
        /// </remarks>
        private StatementSyntax CreateHandlesUpdater(ExpressionSyntax eventSource,
            (string EventContainerName, SyntaxToken EventSymbolName, IEventSymbol Event, int ParametersToDiscard) e,
            SyntaxKind assignmentExpressionKind,
            bool requiresNewDelegate)
        {
            var handledFieldMember = MemberAccess(eventSource, e);

            var invocableRight = requiresNewDelegate && e.Event != null ? NewDelegateMethodId(e)
                : Invocable(_methodId, e.ParametersToDiscard);

            return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(assignmentExpressionKind,
                    handledFieldMember,
                    invocableRight)
            );
        }

        private ExpressionSyntax NewDelegateMethodId((string EventContainerName, SyntaxToken EventSymbolName, IEventSymbol Event, int ParametersToDiscard) e)
        {
            return (ExpressionSyntax)_csSyntaxGenerator.ObjectCreationExpression(e.Event.Type, _methodId);
        }

        public IEnumerable<MethodDeclarationSyntax> CreateDelegatingMethodsRequiredByInitializeComponent()
        {
            return HandledPropertyEventCSharpIds.Concat(HandledClassEventCSharpIds).Where(e => e.ParametersToDiscard > 0 && e.Event?.Type.GetDelegateInvokeMethod() != null)
                .Select(e => {
                    var invokeMethod = (MethodDeclarationSyntax)_csSyntaxGenerator.MethodDeclaration(e.Event.Type.GetDelegateInvokeMethod());
                    return DelegatingMethod(invokeMethod);
                });
        }

        /// <remarks>
        /// If such an overload already exists in the source, this will duplicate it. It seems pretty unlikely though, and probably not worth the effort of renaming.
        /// </remarks>
        private MethodDeclarationSyntax DelegatingMethod(MethodDeclarationSyntax invokeMethod)
        {
            var body = SyntaxFactory.ArrowExpressionClause(SyntaxFactory.InvocationExpression(_methodId));
            return invokeMethod.WithIdentifier(_methodId.Identifier)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
                .WithBody(null).WithExpressionBody(body).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        private static ExpressionSyntax MemberAccess(ExpressionSyntax eventSource, (string EventContainerName, SyntaxToken EventSymbolName, IEventSymbol Event, int ParametersToDiscard) e)
        {
            return SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            eventSource, SyntaxFactory.IdentifierName(e.EventSymbolName));
        }

        public IEnumerable<Assignment> GetConstructorEventHandlers()
        {
            return HandledClassEventCSharpIds.Select(e =>
                new Assignment(MemberAccess(EventContainerExpression(e.EventContainerName), e), SyntaxKind.AddAssignmentExpression, Invocable(_methodId, e.ParametersToDiscard))
            );

            ExpressionSyntax EventContainerExpression(string e) =>
                e switch
                {
                    "this" => SyntaxFactory.ThisExpression(),
                    "base" => SyntaxFactory.BaseExpression(),
                    _ => SyntaxFactory.IdentifierName(e)
                };
        }

        private ExpressionSyntax Invocable(IdentifierNameSyntax methodId, int parametersToDiscard)
        {
            return parametersToDiscard > 0
                ? CommonConversions.ThrowawayParameters(methodId, parametersToDiscard)
                : methodId;
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
