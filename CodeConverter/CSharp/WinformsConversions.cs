using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
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
            return anyNodePossiblyWithinMethod.GetAncestor<VBSyntax.MethodBlockSyntax>()?.SubOrFunctionStatement.Identifier.Text == "InitializeComponent";
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
    }
}