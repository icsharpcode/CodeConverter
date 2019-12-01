using System;
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

            var toSimplify = originalRoot
                .DescendantNodes(n => !(n is TExpressionSyntax) && !nodesWithUnresolvedTypes.Contains(n))
                .Where(n => !nodesWithUnresolvedTypes.Contains(n));
            var newRoot = originalRoot.ReplaceNodes(toSimplify, (orig, rewritten) =>
                rewritten.WithAdditionalAnnotations(Simplifier.Annotation)
                );

            var document = await convertedDocument.WithReducedRootAsync(newRoot.WithAdditionalAnnotations(Simplifier.Annotation));
            return document;
        }

        public static async Task<Document> WithExpandedRootAsync(this Document document)
        {
            var shouldExpand = document.Project.Language == LanguageNames.VisualBasic
                ? (Func<SemanticModel, SyntaxNode, bool>)ShouldExpandVbNode
                : ShouldExpandCsNode;
            document = await WorkaroundBugsInExpandVbAsync(document, shouldExpand);
            document = await ExpandVbAsync(document, shouldExpand);
            return await UndoBadVbExpansionsAsync(document);
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
                if (!Equals(nodeEnclosingNamedType, symbol.ContainingType)) {
                    return !Equals(nodeEnclosingNamedType, symbol.ContainingType?.BaseType);
                }
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

        private static async Task<Document> ExpandVbAsync(Document document, Func<SemanticModel, SyntaxNode, bool> shouldExpand)
        {
            var semanticModel = await document.GetSemanticModelAsync();
            var workspace = document.Project.Solution.Workspace;
            var root = (VBasic.VisualBasicSyntaxNode) await document.GetSyntaxRootAsync();
            try {
                var newRoot = root.ReplaceNodes(root.DescendantNodes(n => !shouldExpand(semanticModel, n)).Where(n => shouldExpand(semanticModel, n)),
                    (node, rewrittenNode) => TryExpandNode(node, semanticModel, workspace)
                );
                return document.WithSyntaxRoot(newRoot);
            } catch (Exception) {
                return document.WithSyntaxRoot(root);
            }
        }
        private static async Task<Document> UndoBadVbExpansionsAsync(Document document)
        {
            var root = (VBasic.VisualBasicSyntaxNode)await document.GetSyntaxRootAsync();
            var toSimplify = root.DescendantNodes()
                .Where(n => n.IsKind(VBasic.SyntaxKind.PredefinedCastExpression, VBasic.SyntaxKind.CTypeExpression, VBasic.SyntaxKind.DirectCastExpression))
                .Where(n => n.HasAnnotation(Simplifier.Annotation));
            root = root.ReplaceNodes(toSimplify, (orig, rewritten) =>
                rewritten.WithAdditionalAnnotations(Simplifier.Annotation)
            );
            return await document.WithReducedRootAsync(root);
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
            return node is VBSyntax.NameSyntax || node is VBSyntax.InvocationExpressionSyntax && !semanticModel.GetSymbolInfo(node).Symbol.IsReducedTypeParameterMethod();
        }

        private static bool ShouldExpandCsNode(SemanticModel semanticModel, SyntaxNode node)
        {
            return false;
        }
    }
}