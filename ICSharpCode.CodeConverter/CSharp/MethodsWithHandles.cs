using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class MethodsWithHandles
    {
        private readonly List<MethodWithHandles> _methodWithHandleses;
        private readonly ILookup<string, MethodWithHandles> _handledMethodsFromPropertyWithEventName;

        public MethodsWithHandles(List<MethodWithHandles> methodWithHandleses)
        {
            _methodWithHandleses = methodWithHandleses;
            _handledMethodsFromPropertyWithEventName = methodWithHandleses
                .SelectMany(m => m.HandledPropertyEventCSharpIds.Select(h => (EventPropertyName: h.Item1.Text, MethodWithHandles: m)))
                .ToLookup(m => m.EventPropertyName, m => m.MethodWithHandles);
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
            if (CommonConversions.MustInlinePropertyWithEventsAccess(node, potentialPropertySymbol))
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

        public SyntaxList<StatementSyntax> GetPreResumeLayoutEventHandlers()
        {
            var handledMethods = _methodWithHandleses.SelectMany(m => m.GetPreResumeLayoutEventHandlers()).ToArray();
            return SyntaxFactory.List(handledMethods);
        }
    }
}