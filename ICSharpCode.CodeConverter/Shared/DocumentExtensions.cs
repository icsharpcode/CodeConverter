using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Util;
using ICSharpCode.CodeConverter.VB;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Simplification;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.Shared
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
            if (document.Project.Language == LanguageNames.VisualBasic) {
                document = await ExpandAsync(document, VbNameExpander.Instance);
                //document = await ExpandAsync(document, VbInvocationExpander.Instance);
            } else {
                document = await ExpandAsync(document, CsExpander.Instance);
            }

            return document;
        }

        private static async Task<Document> ExpandAsync(Document document, ISyntaxExpander expander)
        {
            var semanticModel = await document.GetSemanticModelAsync();
            var workspace = document.Project.Solution.Workspace;
            var root = await document.GetSyntaxRootAsync();
            try {
                var newRoot = root.ReplaceNodes(root.DescendantNodes(n => expander.ShouldExpandWithinNode(semanticModel, n)).Where(n => expander.ShouldExpandNode(semanticModel, n)),
                    (node, rewrittenNode) => expander.TryExpandNode(node, root, semanticModel, workspace)
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
    }
}