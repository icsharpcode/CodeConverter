using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class DocumentExtensions
    {
        public static async Task<Document> WithSimplifiedRootAsync(this Document doc, SyntaxNode syntaxRoot = null)
        {
            var root = syntaxRoot  ?? await doc.GetSyntaxRootAsync();
            var withSyntaxRoot = doc.WithSyntaxRoot(root.WithAdditionalAnnotations(Simplifier.Annotation));
            try {
                return await Simplifier.ReduceAsync(withSyntaxRoot);
            } catch {
                return doc;
            }
        }

        public static async Task<Document> SimplifyStatements<TUsingDirectiveSyntax, TExpressionSyntax>(this Document convertedDocument, string unresolvedTypeDiagnosticId)
        where TUsingDirectiveSyntax : SyntaxNode where TExpressionSyntax : SyntaxNode
        {
            var root = await convertedDocument.GetSyntaxRootAsync();
            var nodesWithUnresolvedTypes = (await convertedDocument.GetSemanticModelAsync()).GetDiagnostics()
                .Where(d => d.Id == unresolvedTypeDiagnosticId && d.Location.IsInSource)
                .Select(d => root.FindNode(d.Location.SourceSpan).GetAncestor<TUsingDirectiveSyntax>())
                .ToLookup(d => (SyntaxNode) d);

            var toSimplify = root
                .DescendantNodes(n => !(n is TExpressionSyntax) && !nodesWithUnresolvedTypes.Contains(n))
                .Where(n => !nodesWithUnresolvedTypes.Contains(n));
            root = root.ReplaceNodes(toSimplify, (orig, rewritten) =>
                rewritten.WithAdditionalAnnotations(Simplifier.Annotation)
                );

            var document = await convertedDocument.WithSimplifiedRootAsync(root);
            return document;
        }

        public static async Task<Document> WithExpandedRootAsync(this Document document)
        {
            var shouldExpand = document.Project.Language == LanguageNames.VisualBasic
                ? (Func<SyntaxNode, bool>)ShouldExpandVbNode
                : ShouldExpandCsNode;
            var originalRoot = (VBasic.VisualBasicSyntaxNode) await document.GetSyntaxRootAsync();
            originalRoot = await ExpandVbAsync(document, originalRoot, ShouldExpandVbNode);
            return document.WithSyntaxRoot(originalRoot);
        }

        private static async Task<VBasic.VisualBasicSyntaxNode> ExpandVbAsync(Document document,
            VBasic.VisualBasicSyntaxNode root, Func<SyntaxNode, bool> shouldExpand)
        {
            var semanticModel = await document.GetSemanticModelAsync();
            var workspace = document.Project.Solution.Workspace;

            return root.ReplaceNodes(root.DescendantNodes(n => !shouldExpand(n)).Where(shouldExpand),
                (node, rewrittenNode) => TryExpandNode(node, semanticModel, workspace)
            );
        }

        private static SyntaxNode TryExpandNode(SyntaxNode node, SemanticModel semanticModel, Workspace workspace)
        {
            try {
                return Simplifier.Expand(node, semanticModel, workspace);
            } catch (Exception) {
                return node;
            }
        }

        private static bool ShouldExpandVbNode(SyntaxNode node)
        {
            return node is VBSyntax.NameSyntax;
        }

        private static bool ShouldExpandCsNode(SyntaxNode node)
        {
            return false;
        }
    }
}