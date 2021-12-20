using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class HandledEventsAnalysis
    {
        private readonly CommonConversions _commonConversions;
        private readonly Dictionary<string, (IPropertySymbol Symbol, MethodWithHandles[] MethodWithHandles)> _propertyNameToHandledEvents;
        private readonly ImmutableHashSet<string> _containerFieldsConvertedToProperties;
        private IEnumerable<MethodWithHandles> AllMethodsWithHandles => _propertyNameToHandledEvents.Values.SelectMany(x => x.MethodWithHandles);

        public HandledEventsAnalysis(CommonConversions commonConversions, Dictionary<string, (IPropertySymbol Symbol, MethodWithHandles[])> propertyNameToHandledEvents,
            ImmutableHashSet<string> containerFieldsConvertedToProperties)
        {
            _commonConversions = commonConversions;
            _propertyNameToHandledEvents = propertyNameToHandledEvents;
            _containerFieldsConvertedToProperties = containerFieldsConvertedToProperties;
        }

        public bool AnySynchronizedPropertiesGenerated() => _propertyNameToHandledEvents.Any();
        public bool ShouldGeneratePropertyFor(string propertyIdentifierText) => _propertyNameToHandledEvents.ContainsKey(propertyIdentifierText) ||
                                                                         _containerFieldsConvertedToProperties.Contains(propertyIdentifierText);

        public IEnumerable<MemberDeclarationSyntax> GetDeclarationsForFieldBackedProperty(VariableDeclarationSyntax fieldDecl, SyntaxTokenList convertedModifiers, SyntaxList<AttributeListSyntax> attributes)
        {
            return MethodWithHandles.GetDeclarationsForFieldBackedProperty(fieldDecl, convertedModifiers, attributes, _propertyNameToHandledEvents);
        }

        public IEnumerable<MemberDeclarationSyntax> GetDeclarationsForHandlingBaseMembers(INamedTypeSymbol namedTypeSymbol)
        {
            var handledAncestorMemberNames = _containerFieldsConvertedToProperties
                .Where(p => !namedTypeSymbol.GetMembers(p).Any(p => p.IsKind(SymbolKind.Property)))
                .ToArray();
            if (!handledAncestorMemberNames.Any()) {
                return Array.Empty<MemberDeclarationSyntax>();
            }

            return baseMembers.Select(GetDeclarationsForHandlingBaseMembers);
        }

        private PropertyDeclarationSyntax GetDeclarationsForHandlingBaseMembers(ISymbol symbol)
        {
            var prop = (PropertyDeclarationSyntax) _commonConversions.CsSyntaxGenerator.Declaration(symbol);
            var modifiers = prop.Modifiers.RemoveOnly(m => m.IsKind(SyntaxKind.VirtualKeyword)).Add(SyntaxFactory.Token(SyntaxKind.OverrideKeyword));
            //TODO Stash away methodwithandles in constructor that don't match any symbol from that type, to match here against base symbols
            var methodWithHandles = _propertyNameToHandledEvents.TryGetValue(symbol.Name, out var m) ? m.MethodWithHandles : Array.Empty<MethodWithHandles>();
            return MethodWithHandles.GetDeclarationsForFieldBackedProperty(methodWithHandles, SyntaxFactory.List<SyntaxNode>(), modifiers, 
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
            return AllMethodsWithHandles
                .SelectMany(m => m.GetInitializeComponentClassEventHandlers()).ToArray();
        }

        public Assignment[] GetConstructorEventHandlers()
        {
            return AllMethodsWithHandles
                .SelectMany(m => m.GetConstructorEventHandlers()).ToArray();
        }

        internal IEnumerable<MemberDeclarationSyntax> CreateDelegatingMethodsRequiredByInitializeComponent()
        {
            return AllMethodsWithHandles
                .SelectMany(m => m.CreateDelegatingMethodsRequiredByInitializeComponent())
                .GroupBy(m => (m.Identifier.Text, string.Join(",", m.ParameterList.Parameters.Select(p => p.Type.ToString()))))
                .Select(g => g.First());
        }
    }
}
