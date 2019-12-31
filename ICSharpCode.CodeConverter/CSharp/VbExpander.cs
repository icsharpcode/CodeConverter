using System;
using System.Linq;
using System.Threading;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class VbNameExpander : ISyntaxExpander
    {
        private static readonly SyntaxToken _dotToken = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory.Token(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.DotToken);
        public static ISyntaxExpander Instance { get; } = new VbNameExpander();

        public bool ShouldExpandWithinNode(SemanticModel semanticModel, SyntaxNode node)
        {
            return !ShouldExpandNode(semanticModel, node) &&
                   !VbInvocationExpander.IsRoslynInstanceExpressionBug(node as MemberAccessExpressionSyntax); ;
        }

        public bool ShouldExpandNode(SemanticModel semanticModel, SyntaxNode node)
        {
            return node is NameSyntax || node is MemberAccessExpressionSyntax maes && IsMyBaseBug(semanticModel, node.SyntaxTree.GetRoot(), node, semanticModel.GetSymbolInfo(node).Symbol) &&
                   !VbInvocationExpander.IsRoslynInstanceExpressionBug(maes);
        }

        public SyntaxNode TryExpandNode(SyntaxNode node, SyntaxNode root, SemanticModel semanticModel,
            Workspace workspace)
        {
            var symbol = semanticModel.GetSymbolInfo(node).Symbol;
            if (node is SimpleNameSyntax sns && IsMyBaseBug(semanticModel, root, node, symbol) && semanticModel.GetOperation(node) is IMemberReferenceOperation mro) {
                var expressionSyntax = (ExpressionSyntax)mro.Instance.Syntax;
                return MemberAccess(expressionSyntax, sns);
            };
            if (node is MemberAccessExpressionSyntax maes && IsMyBaseBug(semanticModel, root, node, symbol) && semanticModel.GetOperation(node) is IMemberReferenceOperation mro2) {
                var expressionSyntax = (ExpressionSyntax)mro2.Instance.Syntax;
                return MemberAccess(expressionSyntax, SyntaxFactory.IdentifierName(mro2.Member.Name));
            };
            return Expander.TryExpandNode(node, semanticModel, workspace);
        }

        /// <returns>True iff calling Expand would qualify with MyBase when the symbol isn't in the base type
        /// See https://github.com/dotnet/roslyn/blob/97123b393c3a5a91cc798b329db0d7fc38634784/src/Workspaces/VisualBasic/Portable/Simplification/VisualBasicSimplificationService.Expander.vb#L657</returns>
        private static bool IsMyBaseBug(SemanticModel semanticModel, SyntaxNode root, SyntaxNode node,
            ISymbol symbol)
        {
            if (symbol?.IsStatic == false && (symbol.Kind == SymbolKind.Method || symbol.Kind ==
                                              SymbolKind.Field || symbol.Kind == SymbolKind.Property))
            {
                INamedTypeSymbol nodeEnclosingNamedType = GetEnclosingNamedType(semanticModel, root, node.SpanStart);
                return !nodeEnclosingNamedType.FollowProperty(t => t.BaseType).Contains(symbol.ContainingType);
            }

            return false;
        }

        private static MemberAccessExpressionSyntax MemberAccess(ExpressionSyntax expressionSyntax, SimpleNameSyntax sns)
        {
            return Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory.MemberAccessExpression(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.SimpleMemberAccessExpression,
                expressionSyntax,
                _dotToken,
                sns);
        }

        /// <summary>
        /// Pasted from AbstractGenerateFromMembersCodeRefactoringProvider
        /// Gets the enclosing named type for the specified position.  We can't use
        /// <see cref="SemanticModel.GetEnclosingSymbol"/> because that doesn't return
        /// the type you're current on if you're on the header of a class/interface.
        /// </summary>
        private static INamedTypeSymbol GetEnclosingNamedType(
            SemanticModel semanticModel, SyntaxNode root, int start,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var token = root.FindToken(start);
            if (token == ((ICompilationUnitSyntax)root).EndOfFileToken) {
                token = token.GetPreviousToken();
            }

            for (var node = token.Parent; node != null; node = node.Parent) {
                if (semanticModel.GetDeclaredSymbol(node) is INamedTypeSymbol declaration) {
                    return declaration;
                }
            }

            return null;
        }
    }
    internal class VbInvocationExpander : ISyntaxExpander
    {
        public static ISyntaxExpander Instance { get; } = new VbInvocationExpander();

        public bool ShouldExpandWithinNode(SemanticModel semanticModel, SyntaxNode node)
        {
            return !IsRoslynInstanceExpressionBug(node) &&
                   !ShouldExpandNode(semanticModel, node);
        }

        public bool ShouldExpandNode(SemanticModel semanticModel, SyntaxNode node)
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