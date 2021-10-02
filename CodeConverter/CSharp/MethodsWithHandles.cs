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
    /// <summary>
    /// MUST call Initialize before using. Alternative implementation in future - embed the analysis, pass a node in the type for each request and make the initialization lazy
    /// </summary>
    internal class MethodsWithHandles
    {
        private readonly List<MethodWithHandles> _methodWithHandleses;
        private readonly ILookup<string, MethodWithHandles> _handledMethodsFromPropertyWithEventName;
        private readonly ImmutableHashSet<string> _containerFieldsConvertedToProperties;


        private MethodsWithHandles(List<MethodWithHandles> methodWithHandleses,
            ILookup<string, MethodWithHandles> handledMethodsFromPropertyWithEventName,
            ImmutableHashSet<string> containerFieldsConvertedToProperties)
        {
            _methodWithHandleses = methodWithHandleses;
            _handledMethodsFromPropertyWithEventName = handledMethodsFromPropertyWithEventName;
            _containerFieldsConvertedToProperties = containerFieldsConvertedToProperties;
        }

        public static MethodsWithHandles Create(List<MethodWithHandles> methodWithHandleses)
        {
            var handledMethodsFromPropertyWithEventName = methodWithHandleses
                .SelectMany(m => m.HandledPropertyEventCSharpIds.Select(h => (EventPropertyName: h.EventContainerName, MethodWithHandles: m)))
                .ToLookup(m => m.EventPropertyName, m => m.MethodWithHandles);
            var containerFieldsConvertedToProperties = methodWithHandleses
                .SelectMany(m => m.HandledPropertyEventCSharpIds, (_, handled) => handled.EventContainerName)
                .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
            return new MethodsWithHandles(methodWithHandleses, handledMethodsFromPropertyWithEventName, containerFieldsConvertedToProperties);
        }

        public bool Any() => _methodWithHandleses.Any();
        public bool AnyForPropertyName(string propertyIdentifierText) => _containerFieldsConvertedToProperties.Contains(propertyIdentifierText);

        public IEnumerable<MemberDeclarationSyntax> GetDeclarationsForFieldBackedProperty(VariableDeclarationSyntax fieldDecl, SyntaxTokenList convertedModifiers, SyntaxList<AttributeListSyntax> attributes)
        {
            return MethodWithHandles.GetDeclarationsForFieldBackedProperty(fieldDecl, convertedModifiers, attributes, _methodWithHandleses);
        }

        public SyntaxList<StatementSyntax> GetPostAssignmentStatements(ISymbol potentialPropertySymbol)
        {
            var fieldName = SyntaxFactory.IdentifierName("_" + potentialPropertySymbol.Name);
            var handledMethods = _handledMethodsFromPropertyWithEventName[potentialPropertySymbol.Name].ToArray();
            if (handledMethods.Any())
            {
                var postAssignmentStatements = handledMethods.SelectMany(h =>
                    h.GetPostInitializationStatements(potentialPropertySymbol.Name, fieldName));
                {
                    return SyntaxFactory.List(postAssignmentStatements);
                }
            }

            return SyntaxFactory.List<StatementSyntax>();
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
