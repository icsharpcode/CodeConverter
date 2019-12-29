using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Operations;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class VbExpander : ISyntaxExpander
    {
        public static ISyntaxExpander Instance { get; } = new VbExpander();

        public async Task<Document> WorkaroundBugsInExpandAsync(Document document)
        {
            var semanticModel = await document.GetSemanticModelAsync();
            var root = (Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxNode)await document.GetSyntaxRootAsync();

            try {
                var newRoot = root.ReplaceNodes(root.DescendantNodes(n => ShouldExpandWithinNode(semanticModel, n)).Where(n => ShouldExpandNode(semanticModel, n)),
                    (node, rewrittenNode) => {
                        var symbol = semanticModel.GetSymbolInfo(node).Symbol;
                        if (rewrittenNode is Microsoft.CodeAnalysis.VisualBasic.Syntax.SimpleNameSyntax sns && IsMyBaseBug(semanticModel, root, node, symbol) && semanticModel.GetOperation(node) is IMemberReferenceOperation mro) {
                            return Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory.MemberAccessExpression(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.SimpleMemberAccessExpression,
                                (Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax) mro.Instance.Syntax,
                                Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory.Token(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.DotToken),
                                sns);
                        };
                        return rewrittenNode;
                    });
                return document.WithSyntaxRoot(newRoot);
            } catch (Exception) {
                return document.WithSyntaxRoot(root);
            }
        }

        /// <returns>True iff calling Expand would qualify with MyBase when the symbol isn't in the base type
        /// See https://github.com/dotnet/roslyn/blob/97123b393c3a5a91cc798b329db0d7fc38634784/src/Workspaces/VisualBasic/Portable/Simplification/VisualBasicSimplificationService.Expander.vb#L657</returns>
        private static bool IsMyBaseBug(SemanticModel semanticModel, Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxNode root, SyntaxNode node,
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

        /// <summary>
        /// Pasted from AbstractGenerateFromMembersCodeRefactoringProvider
        /// Gets the enclosing named type for the specified position.  We can't use
        /// <see cref="SemanticModel.GetEnclosingSymbol"/> because that doesn't return
        /// the type you're current on if you're on the header of a class/interface.
        /// </summary>
        private static INamedTypeSymbol GetEnclosingNamedType(
            SemanticModel semanticModel, SyntaxNode root, int start, CancellationToken cancellationToken = default(CancellationToken))
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

        public SyntaxNode TryExpandNode(SyntaxNode node, SemanticModel semanticModel, Workspace workspace)
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
            if (!(expandedNode is Microsoft.CodeAnalysis.VisualBasic.Syntax.InvocationExpressionSyntax ies)) return false;
            if (!ies.Expression.ToString().StartsWith("Conversions.To")) return false;
            if (node is Microsoft.CodeAnalysis.VisualBasic.Syntax.InvocationExpressionSyntax oies && oies.ToString().StartsWith("Conversions.To")) return false;
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

        public bool ShouldExpandWithinNode(SemanticModel semanticModel, SyntaxNode node)
        {
            return !IsRoslynInstanceExpressionBug(node) &&
                   !ShouldExpandNode(semanticModel, node);
        }

        /// <summary>
        /// Roslyn bug - accidentally expands "New" into an identifier causing compile error
        /// </summary>
        private static bool IsRoslynInstanceExpressionBug(SyntaxNode node)
        {
            return node is Microsoft.CodeAnalysis.VisualBasic.Syntax.InvocationExpressionSyntax ies && ies.Expression is Microsoft.CodeAnalysis.VisualBasic.Syntax.MemberAccessExpressionSyntax maes && maes.Expression is Microsoft.CodeAnalysis.VisualBasic.Syntax.InstanceExpressionSyntax;
        }

        public bool ShouldExpandNode(SemanticModel semanticModel, SyntaxNode node)
        {
            if (!(node is Microsoft.CodeAnalysis.VisualBasic.Syntax.NameSyntax || node is Microsoft.CodeAnalysis.VisualBasic.Syntax.InvocationExpressionSyntax) || IsRoslynInstanceExpressionBug(node)) return false;

            var symbol = semanticModel.GetSymbolInfo(node).Symbol;
            if (symbol is IMethodSymbol ms && (ms.IsGenericMethod || ms.IsReducedTypeParameterMethod())) return false;

            return true;
        }
    }
}