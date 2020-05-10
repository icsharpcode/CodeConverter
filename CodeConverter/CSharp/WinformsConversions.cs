using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class WinformsConversions
    {
        /// <remarks>
        /// Co-ordinates inlining property events, see <see cref="MethodBodyExecutableStatementVisitor.GetPostAssignmentStatements"/>
        /// Also see usages of IsDesignerGeneratedTypeWithInitializeComponent
        /// </remarks>
        public static bool MustInlinePropertyWithEventsAccess(SyntaxNode anyNodePossiblyWithinMethod, ISymbol potentialPropertySymbol)
        {
            return InMethodCalledInitializeComponent(anyNodePossiblyWithinMethod) && potentialPropertySymbol is IPropertySymbol prop && prop.IsWithEvents;
        }

        public static bool InMethodCalledInitializeComponent(SyntaxNode anyNodePossiblyWithinMethod)
        {
            var methodBlockSyntax = anyNodePossiblyWithinMethod.GetAncestor<VBSyntax.MethodBlockSyntax>();
            return IsInitializeComponent(methodBlockSyntax);
        }

        private static bool IsInitializeComponent(VBSyntax.MethodBlockSyntax methodBlockSyntax)
        {
            return methodBlockSyntax?.SubOrFunctionStatement.Identifier.Text == "InitializeComponent";
        }

        /// <summary>
        /// We replace a field with a property to handle event subscription, so need to update the name so the winforms designer regenerates the file correctly in future
        /// </summary>
        /// <returns></returns>
        public static bool ShouldPrefixAssignedNameWithUnderscore(VBSyntax.StatementSyntax statementOrNull)
        {
            return statementOrNull is VBSyntax.AssignmentStatementSyntax assignment && InMethodCalledInitializeComponent(assignment) &&
                            assignment.Left is VBSyntax.MemberAccessExpressionSyntax maes &&
                                !(maes.Expression is VBSyntax.MeExpressionSyntax) &&
                            maes.Name.ToString() == "Name";
        }

        private static T LastOrDefaultDescendant<T>(this VBasic.VisualBasicSyntaxNode syntaxNode) {
            return syntaxNode.DescendantNodes().OfType<T>().LastOrDefault();
        }

        internal static IEnumerable<Assignment> GetNameAssignments((VBSyntax.TypeBlockSyntax Type, SemanticModel SemanticModel)[] otherPartsOfType)
        {
            return otherPartsOfType.SelectMany(typePart => 
                typePart.Type.Members.OfType<VBSyntax.MethodBlockSyntax>()
                    .Where(IsInitializeComponent)
                    .SelectMany(GetAssignments)
            );
        }

        private static IEnumerable<Assignment> GetAssignments(VBSyntax.MethodBlockSyntax initializeComponent)
        {
            return initializeComponent.Statements
                .OfType<VBSyntax.AssignmentStatementSyntax>()
                .Where(ShouldPrefixAssignedNameWithUnderscore)
                .Select(s => (s.Left as VBSyntax.MemberAccessExpressionSyntax)?.Expression.LastOrDefaultDescendant<VBSyntax.IdentifierNameSyntax>())
                .Where(s => s != null)
                .Select(id => {
                    var nameAccess = ValidSyntaxFactory.MemberAccess(SyntaxFactory.IdentifierName("_" + id.Identifier.Text), "Name");
                    var originalRuntimeNameToRestore = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(id.Identifier.Text));
                    return new Assignment(nameAccess, SyntaxKind.SimpleAssignmentExpression, originalRuntimeNameToRestore, true);
                });
        }
    }
}