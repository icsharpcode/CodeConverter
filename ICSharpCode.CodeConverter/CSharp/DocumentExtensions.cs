using System;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
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
            var root = (VBasic.VisualBasicSyntaxNode) await document.GetSyntaxRootAsync();
            root = await ExpandVbAsync(document, root, shouldExpand);
            return await UndoBadVbExpansionsAsync(document, root);
        }

        private static async Task<VBasic.VisualBasicSyntaxNode> ExpandVbAsync(Document document,
            VBasic.VisualBasicSyntaxNode root, Func<SemanticModel, SyntaxNode, bool> shouldExpand)
        {
            var semanticModel = await document.GetSemanticModelAsync();
            var workspace = document.Project.Solution.Workspace;

            try {
                return root.ReplaceNodes(root.DescendantNodes(n => !shouldExpand(semanticModel, n)).Where(n => shouldExpand(semanticModel, n)),
                    (node, rewrittenNode) => TryExpandNode(node, semanticModel, workspace)
                );
            } catch (Exception) {
                return root;
            }
        }
        private static async Task<Document> UndoBadVbExpansionsAsync(Document document,
            VBasic.VisualBasicSyntaxNode root)
        {
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