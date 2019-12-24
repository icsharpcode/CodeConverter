using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Simplification;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class DocumentExtensions
    {
        public static async Task<Document> SimplifyStatements<TUsingDirectiveSyntax, TExpressionSyntax>(this Document convertedDocument, string unresolvedTypeDiagnosticId)
        where TUsingDirectiveSyntax : SyntaxNode where TExpressionSyntax : SyntaxNode
        {
            var originalRoot = await convertedDocument.GetSyntaxRootAsync();
            var nodesWithUnresolvedTypes = (await convertedDocument.GetSemanticModelAsync()).GetDiagnostics()
                .Where(d => d.Id == unresolvedTypeDiagnosticId && d.Location.IsInSource)
                .Select(d => originalRoot.FindNode(d.Location.SourceSpan).GetAncestor<TUsingDirectiveSyntax>())
                .ToLookup(d => (SyntaxNode) d);
            var nodesToConsider = originalRoot
                .DescendantNodes(n =>
                    !(n is TExpressionSyntax) && !nodesWithUnresolvedTypes.Contains(n) &&
                    !WouldBeSimplifiedIncorrectly(n))
                .ToArray();
            var doNotSimplify = nodesToConsider
                .Where(n => nodesWithUnresolvedTypes.Contains(n) || WouldBeSimplifiedIncorrectly(n))
                .SelectMany(n => n.AncestorsAndSelf())
                .ToImmutableHashSet();
            var toSimplify = nodesToConsider.Where(n => !doNotSimplify.Contains(n));
            var newRoot = originalRoot.ReplaceNodes(toSimplify, (orig, rewritten) =>
                rewritten.WithAdditionalAnnotations(Simplifier.Annotation)
            );

            var document = await convertedDocument.WithReducedRootAsync(newRoot);
            return document;
        }

        private static bool WouldBeSimplifiedIncorrectly(SyntaxNode n)
        {
            //Sometimes when empty argument list gets removed it changes the behaviour: https://github.com/dotnet/roslyn/issues/40442
            return n is VBSyntax.InvocationExpressionSyntax ies && !ies.ArgumentList.Arguments.Any();
        }

        public static async Task<Document> WithExpandedRootAsync(this Document document)
        {
            return await (document.Project.Language == LanguageNames.VisualBasic
                ? WithVbExpandedRootAsync(document)
                : WithCsExpandedRootAsync(document));
        }

        private static async Task<Document> WithVbExpandedRootAsync(this Document document)
        {
            var shouldExpand = document.Project.Language == LanguageNames.VisualBasic
                ? (Func<SemanticModel, SyntaxNode, bool>)ShouldExpandVbNode
                : ShouldExpandCsNode;
            document = await WorkaroundBugsInExpandVbAsync(document, ShouldExpandVbNode);
            document = await ExpandAsync(document, shouldExpand);
            return document;
        }

        private static async Task<Document> WithCsExpandedRootAsync(this Document document)
        {
            document = await ExpandAsync(document, ShouldExpandCsNode);
            return document;
        }

        private static async Task<Document> WorkaroundBugsInExpandVbAsync(Document document, Func<SemanticModel, SyntaxNode, bool> shouldExpand)
        {
            var semanticModel = await document.GetSemanticModelAsync();
            var root = (VBasic.VisualBasicSyntaxNode)await document.GetSyntaxRootAsync();

            try {
                var newRoot = root.ReplaceNodes(root.DescendantNodes(n => !shouldExpand(semanticModel, n)).Where(n => shouldExpand(semanticModel, n)),
                    (node, rewrittenNode) => {
                        var symbol = semanticModel.GetSymbolInfo(node).Symbol;
                        if (rewrittenNode is VBSyntax.SimpleNameSyntax sns && IsMyBaseBug(semanticModel, root, node, symbol) && semanticModel.GetOperation(node) is IMemberReferenceOperation mro) {
                            return VBasic.SyntaxFactory.MemberAccessExpression(VBasic.SyntaxKind.SimpleMemberAccessExpression,
                                (VBSyntax.ExpressionSyntax) mro.Instance.Syntax,
                                VBasic.SyntaxFactory.Token(VBasic.SyntaxKind.DotToken),
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
        private static bool IsMyBaseBug(SemanticModel semanticModel, VBasic.VisualBasicSyntaxNode root, SyntaxNode node,
            ISymbol symbol)
        {
            if (symbol?.IsStatic == false && (symbol.Kind == SymbolKind.Method || symbol.Kind ==
                                              SymbolKind.Field || symbol.Kind == SymbolKind.Property))
            {
                INamedTypeSymbol nodeEnclosingNamedType = GetEnclosingNamedType(semanticModel, root, node.SpanStart);
                return !nodeEnclosingNamedType.FollowProperty((ITypeSymbol t) => t.BaseType).Contains(symbol.ContainingType);
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

        private static async Task<Document> ExpandAsync(Document document, Func<SemanticModel, SyntaxNode, bool> shouldExpand)
        {
            var semanticModel = await document.GetSemanticModelAsync();
            var workspace = document.Project.Solution.Workspace;
            var root = (VBasic.VisualBasicSyntaxNode) await document.GetSyntaxRootAsync();
            try {
                var newRoot = root.ReplaceNodes(root.DescendantNodes(n => !shouldExpand(semanticModel, n)).Where(n => shouldExpand(semanticModel, n)),
                    (node, rewrittenNode) => TryExpandVbNode(node, semanticModel, workspace)
                );
                return document.WithSyntaxRoot(newRoot);
            } catch (Exception) {
                return document.WithSyntaxRoot(root);
            }
        }

        private static async Task<Document> WithReducedRootAsync(this Document doc, SyntaxNode syntaxRoot = null)
        {
            var root = syntaxRoot ?? await doc.GetSyntaxRootAsync();
            var withSyntaxRoot = doc.WithSyntaxRoot(root);
            try {
                return await Simplifier.ReduceAsync(withSyntaxRoot);
            } catch {
                return doc;
            }
        }


        private static SyntaxNode TryExpandVbNode(SyntaxNode node, SemanticModel semanticModel, Workspace workspace)
        {
            var expandedNode = TryExpandNode(node, semanticModel, workspace);

            //See https://github.com/icsharpcode/CodeConverter/pull/449#issuecomment-561678148
            return IsRedundantConversion(node, semanticModel, expandedNode) ? node : expandedNode;
        }

        private static bool IsRedundantConversion(SyntaxNode node, SemanticModel semanticModel, SyntaxNode expandedNode)
        {
            return IsRedundantConversionToMethod(node, semanticModel, expandedNode) || IsRedundantCastMethod(node, semanticModel, expandedNode);
        }

        private static bool IsRedundantConversionToMethod(SyntaxNode node, SemanticModel semanticModel, SyntaxNode expandedNode)
        {
            if (!(expandedNode is VBSyntax.InvocationExpressionSyntax ies)) return false;
            if (!ies.Expression.ToString().StartsWith("Conversions.To")) return false;
            if (node is VBSyntax.InvocationExpressionSyntax oies && oies.ToString().StartsWith("Conversions.To")) return false;
            var originalTypeInfo = semanticModel.GetTypeInfo(node);
            return originalTypeInfo.Type.Equals(originalTypeInfo.ConvertedType);
        }

        private static bool IsRedundantCastMethod(SyntaxNode node, SemanticModel semanticModel, SyntaxNode expandedNode)
        {
            if (!(expandedNode.IsKind(VBasic.SyntaxKind.PredefinedCastExpression, VBasic.SyntaxKind.CTypeExpression, VBasic.SyntaxKind.DirectCastExpression))) return false;
            if (node.Kind() == expandedNode.Kind()) return false;
            var originalTypeInfo = semanticModel.GetTypeInfo(node);
            return originalTypeInfo.Type.Equals(originalTypeInfo.ConvertedType);
        }

        private static SyntaxNode TryExpandNode(SyntaxNode node, SemanticModel semanticModel, Workspace workspace)
        {
            try {
                return Simplifier.Expand(node, semanticModel, workspace);
            } catch (Exception) {
                return node;
            }
        }

        private static bool ShouldExpandVbNode(SemanticModel semanticModel, SyntaxNode node)
        {
            if (!(node is VBSyntax.NameSyntax || node is VBSyntax.InvocationExpressionSyntax)) return false;

            var symbol = semanticModel.GetSymbolInfo(node).Symbol;
            if (symbol is IMethodSymbol ms && (ms.IsGenericMethod || ms.IsReducedTypeParameterMethod())) return false;

            return true;
        }
        private static bool ShouldExpandCsNode(SemanticModel semanticModel, SyntaxNode node)
        {
            return node is CSSyntax.IdentifierNameSyntax;
        }
    }
}