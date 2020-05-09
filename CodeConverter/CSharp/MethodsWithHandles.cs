using System;
using System.Collections.Generic;
using System.Linq;
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
        private List<MethodWithHandles> _methodWithHandleses;
        private ILookup<string, MethodWithHandles> _handledMethodsFromPropertyWithEventName;


        public MethodsWithHandles(List<MethodWithHandles> methodWithHandleses, ILookup<string, MethodWithHandles> handledMethodsFromPropertyWithEventName)
        {
            _methodWithHandleses = methodWithHandleses;
            _handledMethodsFromPropertyWithEventName = handledMethodsFromPropertyWithEventName;
        }

        public static MethodsWithHandles Create(List<MethodWithHandles> methodWithHandleses)
        {
            var handledMethodsFromPropertyWithEventName = methodWithHandleses
                .SelectMany(m => m.HandledPropertyEventCSharpIds.Select(h => (EventPropertyName: h.Item1.Text, MethodWithHandles: m)))
                .ToLookup(m => m.EventPropertyName, m => m.MethodWithHandles);
            return new MethodsWithHandles(methodWithHandleses, handledMethodsFromPropertyWithEventName);
        }

        public bool Any()
        {
            return _methodWithHandleses.Any();
        }

        public IEnumerable<MemberDeclarationSyntax> GetDeclarationsForFieldBackedProperty(VariableDeclarationSyntax fieldDecl, SyntaxTokenList convertedModifiers, SyntaxList<AttributeListSyntax> list)
        {
            return MethodWithHandles.GetDeclarationsForFieldBackedProperty(fieldDecl, convertedModifiers, list,
                _methodWithHandleses);
        }


        /// <summary>
        /// Make winforms designer work: https://github.com/icsharpcode/CodeConverter/issues/321
        /// </summary>
        public SyntaxList<StatementSyntax> GetPostAssignmentStatements(Microsoft.CodeAnalysis.VisualBasic.Syntax.AssignmentStatementSyntax node, ISymbol potentialPropertySymbol)
        {
            if (WinformsConversions.MustInlinePropertyWithEventsAccess(node, potentialPropertySymbol))
            {
                var fieldName = SyntaxFactory.IdentifierName("_" + potentialPropertySymbol.Name);
                var handledMethods = _handledMethodsFromPropertyWithEventName[potentialPropertySymbol.Name].ToArray();
                if (handledMethods.Any())
                {
                    var postAssignmentStatements = handledMethods.SelectMany(h =>
                        h.GetPostInitializationStatements(potentialPropertySymbol.Name, fieldName));
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
