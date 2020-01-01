using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class VbInvocationExpander : ISyntaxExpander
    {
        public static ISyntaxExpander Instance { get; } = new VbInvocationExpander();

        public bool ShouldExpandWithinNode(SyntaxNode node, SyntaxNode root, SemanticModel semanticModel)
        {
            return !IsRoslynInstanceExpressionBug(node) &&
                   !ShouldExpandNode(node, root, semanticModel);
        }

        public bool ShouldExpandNode(SyntaxNode node, SyntaxNode root, SemanticModel semanticModel)
        {
            return (node is InvocationExpressionSyntax) &&
                   !IsRoslynInstanceExpressionBug(node) && !IsOriginalSymbolGenericMethod(semanticModel, node);
        }

        public SyntaxNode TryExpandNode(SyntaxNode node, SyntaxNode root, SemanticModel semanticModel,
            Workspace workspace)
        {
            var expandedNode = Expander.TryExpandNode(node, semanticModel, workspace);

            //See https://github.com/icsharpcode/CodeConverter/pull/449#issuecomment-561678148
            return IsRedundantConversion(node, semanticModel, expandedNode) ? node : expandedNode;
        }

        private static bool IsRedundantConversion(SyntaxNode node, SemanticModel semanticModel, SyntaxNode expandedNode)
        {
            return IsRedundantConversionToMethod(node, semanticModel, expandedNode) || IsRedundantCastMethod(node, semanticModel, expandedNode);
        }

        private static bool IsRedundantConversionToMethod(SyntaxNode node, SemanticModel semanticModel, SyntaxNode expandedNode)
        {
            if (!(expandedNode is InvocationExpressionSyntax ies)) return false;
            if (!ies.Expression.ToString().StartsWith("Conversions.To")) return false;
            if (node is InvocationExpressionSyntax oies && oies.ToString().StartsWith("Conversions.To")) return false;
            var originalTypeInfo = semanticModel.GetTypeInfo(node);
            return originalTypeInfo.Type.Equals(originalTypeInfo.ConvertedType);
        }

        private static bool IsRedundantCastMethod(SyntaxNode node, SemanticModel semanticModel, SyntaxNode expandedNode)
        {
            if (!(expandedNode.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.PredefinedCastExpression, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.CTypeExpression, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.DirectCastExpression))) return false;
            if (node.Kind() == expandedNode.Kind()) return false;
            var originalTypeInfo = semanticModel.GetTypeInfo(node);
            return originalTypeInfo.Type.Equals(originalTypeInfo.ConvertedType);
        }

        /// <summary>
        /// Roslyn bug - accidentally expands "New" into an identifier causing compile error
        /// </summary>
        public static bool IsRoslynInstanceExpressionBug(SyntaxNode node)
        {
            return node is InvocationExpressionSyntax ies && IsRoslynInstanceExpressionBug(ies.Expression as MemberAccessExpressionSyntax);
        }

        /// <summary>
        /// Roslyn bug - accidentally expands "New" into an identifier causing compile error
        /// </summary>
        public static bool IsRoslynInstanceExpressionBug(MemberAccessExpressionSyntax node)
        {
            return node?.Expression is InstanceExpressionSyntax;
        }

        /// <summary>
        /// Roslyn bug - accidentally expands anonymous types to just "Global."
        /// Since the C# reducer also doesn't seem to reduce generic extension methods, it's best to avoid those too, so let's just avoid all generic methods
        /// </summary>
        private static bool IsOriginalSymbolGenericMethod(SemanticModel semanticModel, SyntaxNode node)
        {
            var symbol = semanticModel.GetSymbolInfo(node).Symbol;
            return symbol is IMethodSymbol ms && (ms.IsGenericMethod || ms.IsReducedTypeParameterMethod());
        }
    }
}