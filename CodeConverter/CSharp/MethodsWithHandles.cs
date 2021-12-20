using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;
using ICSharpCode.CodeConverter.Util;
using ICSharpCode.CodeConverter.Util.FromRoslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    /// <summary>
    /// MUST call Initialize before using. Alternative implementation in future - embed the analysis, pass a node in the type for each request and make the initialization lazy
    /// </summary>
    internal class MethodsWithHandles
    {
        private readonly CommonConversions _commonConversions;
        private readonly List<MethodWithHandles> _methodWithHandleses;
        private readonly Dictionary<string, (IPropertySymbol Symbol, MethodWithHandles[] MethodWithHandles)> _propertyNameToHandledEvents;
        private readonly ImmutableHashSet<string> _containerFieldsConvertedToProperties;


        private MethodsWithHandles(CommonConversions commonConversions, List<MethodWithHandles> methodWithHandleses,
            Dictionary<string, (IPropertySymbol Symbol, MethodWithHandles[])> propertyNameToHandledEvents,
            ImmutableHashSet<string> containerFieldsConvertedToProperties)
        {
            _commonConversions = commonConversions;
            _methodWithHandleses = methodWithHandleses;
            _propertyNameToHandledEvents = propertyNameToHandledEvents;
            _containerFieldsConvertedToProperties = containerFieldsConvertedToProperties;
        }

        public static MethodsWithHandles Create(CommonConversions commonConversions, List<MethodWithHandles> methodWithHandleses, IPropertySymbol[] writtenWithEventsProperties)
        {
            var handledMethodsFromPropertyWithEventName = methodWithHandleses
                .SelectMany(m => m.HandledPropertyEventCSharpIds.Select(h => (EventPropertyName: h.EventContainerName, MethodWithHandles: m)))
                .ToLookup(m => m.EventPropertyName, m => m.MethodWithHandles);
            var propertiesWithEvents = writtenWithEventsProperties.ToDictionary(p => p.Name, p => (p: p, handledMethodsFromPropertyWithEventName[p.Name].ToArray()));
            var containerFieldsConvertedToProperties = methodWithHandleses
                .SelectMany(m => m.HandledPropertyEventCSharpIds, (_, handled) => handled.EventContainerName)
                .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
            return new MethodsWithHandles(commonConversions, methodWithHandleses, propertiesWithEvents, containerFieldsConvertedToProperties);
        }

        public bool AnySynchronizedPropertiesGenerated() => _propertyNameToHandledEvents.Any();
        public bool ShouldGeneratePropertyFor(string propertyIdentifierText) => _propertyNameToHandledEvents.ContainsKey(propertyIdentifierText) ||
                                                                         _containerFieldsConvertedToProperties.Contains(propertyIdentifierText);

        public IEnumerable<MemberDeclarationSyntax> GetDeclarationsForFieldBackedProperty(VariableDeclarationSyntax fieldDecl, SyntaxTokenList convertedModifiers, SyntaxList<AttributeListSyntax> attributes)
        {
            return MethodWithHandles.GetDeclarationsForFieldBackedProperty(fieldDecl, convertedModifiers, attributes, _methodWithHandleses);
        }

        public IEnumerable<MemberDeclarationSyntax> GetDeclarationsForHandlingBaseMembers(INamedTypeSymbol namedTypeSymbol)
        {
            var handledAncestorMemberNames = _containerFieldsConvertedToProperties
                .Where(p => !namedTypeSymbol.GetMembers(p).Any(MemberCanHandleEvents))
                .ToArray();
            if (!handledAncestorMemberNames.Any()) {
                return Array.Empty<MemberDeclarationSyntax>();
            }

            var ancestorMembersByName = namedTypeSymbol.GetBaseTypes().SelectMany(t => t.GetMembers().Where(MemberCanHandleEvents)).ToLookup(m => m.Name);
            return handledAncestorMemberNames.Select(name => ancestorMembersByName[name].FirstOrDefault()).Where(x => x != null)
                .Select(GetDeclarationsForHandlingBaseMembers);
        }

        private static bool MemberCanHandleEvents(ISymbol m) => m.IsKind(SymbolKind.Property);

        private PropertyDeclarationSyntax GetDeclarationsForHandlingBaseMembers(ISymbol symbol)
        {
            var prop = (PropertyDeclarationSyntax) _commonConversions.CsSyntaxGenerator.Declaration(symbol);
            var modifiers = prop.Modifiers.RemoveOnly(m => m.IsKind(SyntaxKind.VirtualKeyword)).Add(SyntaxFactory.Token(SyntaxKind.OverrideKeyword));
            return MethodWithHandles.GetDeclarationsForFieldBackedProperty(_methodWithHandleses, SyntaxFactory.List<SyntaxNode>(), modifiers, 
                prop.Type, prop.Identifier, SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.BaseExpression(), SyntaxFactory.IdentifierName(prop.Identifier)));
        }

        public SyntaxList<StatementSyntax> GetPostAssignmentStatements(ISymbol potentialPropertySymbol)
        {
            var fieldName = SyntaxFactory.IdentifierName("_" + potentialPropertySymbol.Name);
            var handledMethods = _propertyNameToHandledEvents.TryGetValue(potentialPropertySymbol.Name, out var h) ? h.MethodWithHandles : Array.Empty<MethodWithHandles>();
            var postAssignmentStatements = handledMethods.SelectMany(hm =>
                hm.GetPostInitializationStatements(potentialPropertySymbol.Name, fieldName));
            {
                return SyntaxFactory.List(postAssignmentStatements);
            }
        }

        public IEnumerable<StatementSyntax> GetInitializeComponentClassEventHandlers()
        {
            return _methodWithHandleses.SelectMany(m => m.GetInitializeComponentClassEventHandlers()).ToArray();
        }

        public Assignment[] GetConstructorEventHandlers()
        {
            return _methodWithHandleses.SelectMany(m => m.GetConstructorEventHandlers()).ToArray();
        }

        internal IEnumerable<MemberDeclarationSyntax> CreateDelegatingMethodsRequiredByInitializeComponent()
        {
            return _methodWithHandleses.SelectMany(m => m.CreateDelegatingMethodsRequiredByInitializeComponent())
                .GroupBy(m => (m.Identifier.Text, string.Join(",", m.ParameterList.Parameters.Select(p => p.Type.ToString()))))
                .Select(g => g.First());
        }
    }
}
