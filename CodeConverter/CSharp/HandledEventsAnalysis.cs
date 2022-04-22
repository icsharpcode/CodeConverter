using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp;

internal class HandledEventsAnalysis
{
    public enum EventContainerKind { Base, This, Property }
    public record EventContainer(EventContainerKind Kind, string PropertyName);

    private readonly Dictionary<string, (EventContainer EventContainer, (IPropertySymbol Property, bool IsNeverWrittenOrOverridden) PropertyDetails, (EventDescriptor Event, IMethodSymbol HandlingMethod, int ParametersToDiscard)[] HandledMethods)> _handlingMethodsByPropertyName;
    private readonly (EventContainer EventContainer, (IPropertySymbol Property, bool IsNeverWrittenOrOverridden) PropertyDetails, (EventDescriptor Event, IMethodSymbol HandlingMethod, int ParametersToDiscard)[] HandledMethods)[] _handlingMethodsForInstance;
    private readonly CommonConversions _commonConversions;
    private readonly INamedTypeSymbol _type;


    public HandledEventsAnalysis(CommonConversions commonConversions, INamedTypeSymbol type,
        IEnumerable<(EventContainer EventContainer, (IPropertySymbol Property, bool IsNeverWrittenOrOverridden) PropertyDetails, (EventDescriptor Event, IMethodSymbol HandlingMethod, int
            ParametersToDiscard)[] HandledMethods)> csharpEventContainerToHandlingMethods)
    {
        _commonConversions = commonConversions;
        _type = type;
        var (handlingMethodsForInstance, handlingMethodsForPropertyEvents) = csharpEventContainerToHandlingMethods
            .Select(m => (m.EventContainer, m.PropertyDetails, m.HandledMethods))
            .SplitOn(m => m.EventContainer.Kind == EventContainerKind.Property);

        _handlingMethodsForInstance = handlingMethodsForInstance;
        _handlingMethodsByPropertyName = handlingMethodsForPropertyEvents.ToDictionary(h => h.EventContainer.PropertyName, StringComparer.OrdinalIgnoreCase);
    }

    public bool AnySynchronizedPropertiesGenerated() => _handlingMethodsByPropertyName.Any(p => !p.Value.PropertyDetails.IsNeverWrittenOrOverridden);
    public bool ShouldGeneratePropertyFor(string propertyIdentifierText) => _handlingMethodsByPropertyName.TryGetValue(propertyIdentifierText, out var handled) && !handled.PropertyDetails.IsNeverWrittenOrOverridden;

    public IEnumerable<Assignment> GetConstructorEventHandlers()
    {
        return _handlingMethodsForInstance.SelectMany(e => e.HandledMethods, (e, m) => {
            var methodId = SyntaxFactory.IdentifierName(CommonConversions.CsEscapedIdentifier(m.HandlingMethod.Name));
            return new Assignment(MemberAccess(EventContainerExpression(e.EventContainer), m.Event), SyntaxKind.AddAssignmentExpression, Invokable(methodId, m.ParametersToDiscard));
        });

        ExpressionSyntax EventContainerExpression(EventContainer eventContainer) =>
            eventContainer.Kind switch {
                EventContainerKind.Base => SyntaxFactory.BaseExpression(),
                EventContainerKind.This => SyntaxFactory.ThisExpression(),
                _ => SyntaxFactory.IdentifierName(CommonConversions.CsEscapedIdentifier(eventContainer.PropertyName))
            };
    }

    /// <summary>Use instead of <see cref="GetConstructorEventHandlers"/> for DesignerGenerated classes: https://github.com/icsharpcode/CodeConverter/issues/550</summary>
    public IEnumerable<StatementSyntax> GetInitializeComponentClassEventHandlers()
    {
        return _handlingMethodsForInstance
            .SelectMany(eventContainer => eventContainer.HandledMethods, (_, subscription) => CreateHandlesUpdater(SyntaxFactory.ThisExpression(), subscription.Event, SyntaxKind.AddAssignmentExpression,
                subscription.HandlingMethod,
                subscription.ParametersToDiscard, true));
    }

    public IEnumerable<MethodDeclarationSyntax> CreateDelegatingMethodsRequiredByInitializeComponent()
    {
        return _handlingMethodsForInstance.Concat(_handlingMethodsByPropertyName.Values).SelectMany(x => x.HandledMethods, (c, s) => (Container: c, Subscription: s))
            .Where(h => h.Subscription.ParametersToDiscard > 0 && h.Subscription.Event.SymbolOrNull?.Type.GetDelegateInvokeMethod() != null)
            .Select(e => CreateDelegatingMethod(SyntaxFactory.IdentifierName(CommonConversions.CsEscapedIdentifier(e.Subscription.HandlingMethod.Name)), e.Subscription.Event))
            .GroupBy(m => (m.Identifier.Text, string.Join(",", m.ParameterList.Parameters.Select(p => p.Type.ToString()))))
            .Select(g => g.First());
    }

    public IEnumerable<MemberDeclarationSyntax> GetDeclarationsForHandlingBaseMembers()
    {
        return _handlingMethodsByPropertyName.Values
            .Where(m => m.EventContainer.Kind == EventContainerKind.Property && !_type.Equals(m.PropertyDetails.Property?.ContainingType, SymbolEqualityComparer.IncludeNullability))
            .Select(x => GetDeclarationsForHandlingBaseMembers(x));
    }

    private PropertyDeclarationSyntax GetDeclarationsForHandlingBaseMembers((EventContainer EventContainer, (IPropertySymbol Property, bool IsNeverWrittenOrOverridden) PropertyDetails, (EventDescriptor Event, IMethodSymbol HandlingMethod, int ParametersToDiscard)[] HandledMethods) basePropertyEventSubscription)
    {
        var prop = (PropertyDeclarationSyntax) _commonConversions.CsSyntaxGenerator.Declaration(basePropertyEventSubscription.PropertyDetails.Property);
        var modifiers = prop.Modifiers.RemoveWhere(m => m.IsKind(SyntaxKind.VirtualKeyword)).Add(SyntaxFactory.Token(SyntaxKind.OverrideKeyword));
        //TODO Stash away methodwithandles in constructor that don't match any symbol from that type, to match here against base symbols
        return GetDeclarationsForFieldBackedProperty(basePropertyEventSubscription.HandledMethods, SyntaxFactory.List<SyntaxNode>(), modifiers, 
            prop.Type, prop.Identifier, SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.BaseExpression(), SyntaxFactory.IdentifierName(prop.Identifier)));
    }

    public SyntaxList<StatementSyntax> GetPostAssignmentStatements(ISymbol potentialPropertySymbol)
    {
        if (!_handlingMethodsByPropertyName.TryGetValue(potentialPropertySymbol.Name, out var h)) return SyntaxFactory.List<StatementSyntax>();
        var prefix = h.PropertyDetails.IsNeverWrittenOrOverridden ? "" : "_";
        var fieldName = SyntaxFactory.IdentifierName(CommonConversions.CsEscapedIdentifier(prefix + potentialPropertySymbol.Name));
        var postAssignmentStatements = h.HandledMethods.Select(hm =>
            CreateHandlesUpdater(fieldName, hm.Event, SyntaxKind.AddAssignmentExpression, hm.HandlingMethod, hm.ParametersToDiscard, true));
        {
            return SyntaxFactory.List(postAssignmentStatements);
        }
    }

    public IEnumerable<MemberDeclarationSyntax> GetDeclarationsForFieldBackedProperty(
        VariableDeclarationSyntax decl, SyntaxTokenList convertedModifiers,
        SyntaxList<AttributeListSyntax> attributes)
    {
        // It should be safe to use the underscore name since in VB the compiler generates a backing field with that name, and errors if you try to clash with it
        var (nonRenamedEvents, renamedEvents) = decl.Variables
            .SplitOn(v => ShouldGeneratePropertyFor(v.Identifier.Text),
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
            var toHandle = _handlingMethodsByPropertyName[variable.Identifier.Text];
            var propertySymbol = toHandle.PropertyDetails.Property;

            // This is overridden when a inheriting class handles the event - see other use of GetDeclarationsForFieldBackedProperty
            var propModifiers = !propertySymbol.ContainingType.IsSealed && propertySymbol.DeclaredAccessibility != Accessibility.Private
                ? convertedModifiers.Add(SyntaxFactory.Token(SyntaxKind.VirtualKeyword))
                : convertedModifiers;

            yield return GetDeclarationsForFieldBackedProperty(toHandle.HandledMethods, attributes, propModifiers,
                decl.Type, variable.Identifier,
                SyntaxFactory.IdentifierName(newId));
        }
    }

    private static SyntaxTokenList MakePrivate(SyntaxTokenList convertedModifiers)
    {
        var noVisibility = convertedModifiers
            .RemoveWhere(m => m.IsCsVisibility(false, false))
            .Insert(0, SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
        return noVisibility;
    }

    private PropertyDeclarationSyntax GetDeclarationsForFieldBackedProperty((EventDescriptor Event, IMethodSymbol HandlingMethod, int ParametersToDiscard)[] methods,
        SyntaxList<AttributeListSyntax> attributes, SyntaxTokenList convertedModifiers, TypeSyntax typeSyntax,
        SyntaxToken propertyIdentifier, ExpressionSyntax internalEventTarget)
    {
        var synchronizedAttribute = SyntaxFactory.List(new[] { CreateSynchronizedAttribute() });

        var returnField = SyntaxFactory.ReturnStatement(internalEventTarget);
        var getter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, synchronizedAttribute, SyntaxFactory.TokenList(), SyntaxFactory.Block(returnField));

        var block = CreateSetterBlockToUpdateHandles(internalEventTarget, methods);
        var setter = SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration, synchronizedAttribute, SyntaxFactory.TokenList(), SyntaxFactory.Block(block));

        var accessorListSyntax = SyntaxFactory.AccessorList(SyntaxFactory.List(new[] { getter, setter }));

        var propModifiers = SyntaxFactory.TokenList(convertedModifiers.Where(m => !m.IsKind(SyntaxKind.ReadOnlyKeyword)));
        return SyntaxFactory.PropertyDeclaration(attributes,
            propModifiers, typeSyntax, null, propertyIdentifier, accessorListSyntax);
    }

    private StatementSyntax[] CreateSetterBlockToUpdateHandles(ExpressionSyntax fieldIdSyntax,
        (EventDescriptor Event, IMethodSymbol HandlingMethod, int ParametersToDiscard)[] methods)
    {
        var assignBackingField = SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression, fieldIdSyntax,
            SyntaxFactory.IdentifierName("value")));

        if (!methods.Any()) return new StatementSyntax[] { assignBackingField };

        return new[]
        {
            IfFieldNotNull(fieldIdSyntax, CreateHandlesUpdaters(fieldIdSyntax, methods, SyntaxKind.SubtractAssignmentExpression)),
            assignBackingField,
            IfFieldNotNull(fieldIdSyntax, CreateHandlesUpdaters(fieldIdSyntax, methods, SyntaxKind.AddAssignmentExpression))
        };
    }

    private IEnumerable<StatementSyntax> CreateHandlesUpdaters(ExpressionSyntax fieldIdSyntax,
        (EventDescriptor Event, IMethodSymbol HandlingMethod, int ParametersToDiscard)[] methodsToHandle,
        SyntaxKind assignmentExpressionKind) =>
        methodsToHandle.Select(hs => CreateHandlesUpdater(fieldIdSyntax, hs.Event, assignmentExpressionKind, hs.HandlingMethod, hs.ParametersToDiscard, false));


    /// <remarks>
    /// <paramref name="requiresNewDelegate"/> must be set to true if this statement will be within InitializeComponent, otherwise C# Winforms designer won't be able to recognize it.
    /// If a lambda has been generated to discard parameters, the C# Winforms designer will throw an exception when trying to load, but it will work at runtime, and it's better than silently losing events on regeneration.
    /// </remarks>
    private StatementSyntax CreateHandlesUpdater(ExpressionSyntax eventSource,
        EventDescriptor e,
        SyntaxKind assignmentExpressionKind,
        IMethodSymbol methodSymbol,
        int parametersToDiscard,
        bool requiresNewDelegate)
    {
        var handledFieldMember = MemberAccess(eventSource, e);
        var methodId = SyntaxFactory.IdentifierName(CommonConversions.CsEscapedIdentifier(methodSymbol.Name));
        var invocableRight = requiresNewDelegate && e.SymbolOrNull != null ? NewDelegateMethodId(methodId, e.SymbolOrNull)
            : Invokable(methodId, parametersToDiscard);

        return AssignExpr(handledFieldMember, assignmentExpressionKind, invocableRight);
    }

    private static ExpressionStatementSyntax AssignExpr(ExpressionSyntax left, SyntaxKind assignmentExpressionKind, ExpressionSyntax right)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(assignmentExpressionKind,
                left,
                right)
        );
    }

    private ExpressionSyntax NewDelegateMethodId(IdentifierNameSyntax methodId, IEventSymbol e)
    {
        return (ExpressionSyntax)_commonConversions.CsSyntaxGenerator.ObjectCreationExpression(e.Type, methodId);
    }

    private MethodDeclarationSyntax CreateDelegatingMethod(IdentifierNameSyntax methodId,
        EventDescriptor e)
    {
        var invokeMethod = (MethodDeclarationSyntax) (_commonConversions.CsSyntaxGenerator.MethodDeclaration(e.SymbolOrNull.Type.GetDelegateInvokeMethod()));
        return DelegatingMethod(methodId, invokeMethod);
    }

    /// <remarks>
    /// If such an overload already exists in the source, this will duplicate it. It seems pretty unlikely though, and probably not worth the effort of renaming.
    /// </remarks>
    private static MethodDeclarationSyntax DelegatingMethod(IdentifierNameSyntax methodId, MethodDeclarationSyntax invokeMethod)
    {
        var body = SyntaxFactory.ArrowExpressionClause(SyntaxFactory.InvocationExpression(methodId));
        return invokeMethod.WithIdentifier(methodId.Identifier)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
            .WithBody(null).WithExpressionBody(body).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }

    private ExpressionSyntax MemberAccess(ExpressionSyntax eventContainer, EventDescriptor e)
    {
        var csEventName = SyntaxFactory.IdentifierName(_commonConversions.ConvertIdentifier(e.VBEventName.Identifier).WithoutSourceMapping());
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            eventContainer, csEventName);
    }

    private static ExpressionSyntax Invokable(IdentifierNameSyntax methodId, int parametersToDiscard)
    {
        return parametersToDiscard > 0
            ? CommonConversions.ThrowawayParameters(methodId, parametersToDiscard)
            : methodId;
    }

    private static StatementSyntax IfFieldNotNull(ExpressionSyntax fieldIdSyntax, IEnumerable<StatementSyntax> statements)
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
        return SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(new[] { attribute }));
    }
}