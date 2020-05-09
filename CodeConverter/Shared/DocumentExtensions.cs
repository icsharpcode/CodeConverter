using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Util;
using ICSharpCode.CodeConverter.VB;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.Shared
{
    internal static class DocumentExtensions
    {
        public static async Task<Document> SimplifyStatementsAsync<TUsingDirectiveSyntax, TExpressionSyntax>(this Document convertedDocument, string unresolvedTypeDiagnosticId, CancellationToken cancellationToken)
        where TUsingDirectiveSyntax : SyntaxNode where TExpressionSyntax : SyntaxNode
        {
            Func<SyntaxNode, bool> wouldBeSimplifiedIncorrectly =
                convertedDocument.Project.Language == LanguageNames.VisualBasic
                    ? (Func<SyntaxNode, bool>) VbWouldBeSimplifiedIncorrectly
                    : CsWouldBeSimplifiedIncorrectly;
            var originalRoot = await convertedDocument.GetSyntaxRootAsync();
            var nodesWithUnresolvedTypes = (await convertedDocument.GetSemanticModelAsync()).GetDiagnostics()
                .Where(d => d.Id == unresolvedTypeDiagnosticId && d.Location.IsInSource)
                .Select(d => originalRoot.FindNode(d.Location.SourceSpan).GetAncestor<TUsingDirectiveSyntax>())
                .ToLookup(d => (SyntaxNode) d);
            var nodesToConsider = originalRoot
                .DescendantNodes()
                .Where(n => nodesWithUnresolvedTypes.Contains(n) || wouldBeSimplifiedIncorrectly(n))
                .ToArray();
            var doNotSimplify = nodesToConsider
                .SelectMany(n => n.AncestorsAndSelf())
                .ToImmutableHashSet();
            var toSimplify = originalRoot
                .DescendantNodes()
                .Where(n => !doNotSimplify.Contains(n));
            var newRoot = originalRoot.ReplaceNodes(toSimplify, (orig, rewritten) =>
                rewritten.WithAdditionalAnnotations(Simplifier.Annotation)
            );

            var document = await convertedDocument.WithReducedRootAsync(newRoot, cancellationToken);
            return document;
        }

        private static bool VbWouldBeSimplifiedIncorrectly(SyntaxNode n)
        {
            //Roslyn bug: empty argument list gets removed and changes behaviour: https://github.com/dotnet/roslyn/issues/40442
            // (Also null Expression blows up even though that's how conditional invocation on an IdentifierName happens)
            return n is VBSyntax.InvocationExpressionSyntax ies && (!ies.ArgumentList.Arguments.Any() || ies.Expression == null)
                || n is VBSyntax.TryCastExpressionSyntax
                // Roslyn bug: Tries to simplify to "InferredFieldInitializerSyntax" which cannot be placed within an ObjectCreationExpression https://github.com/icsharpcode/CodeConverter/issues/484
                || n is VBSyntax.ObjectCreationExpressionSyntax;
        }

        private static bool CsWouldBeSimplifiedIncorrectly(SyntaxNode n)
        {
            return false;
        }

        public static async Task<Document> WithExpandedRootAsync(this Document document, CancellationToken cancellationToken)
        {
            if (document.Project.Language == LanguageNames.VisualBasic) {
                document = await ExpandAsync(document, VbNameExpander.Instance, cancellationToken);
            } else {
                document = await ExpandAsync(document, CsExpander.Instance, cancellationToken);
            }

            return document;
        }

        private static async Task<Document> ExpandAsync(Document document, ISyntaxExpander expander, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var workspace = document.Project.Solution.Workspace;
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            try {
                var newRoot = root.ReplaceNodes(root.DescendantNodes(n => expander.ShouldExpandWithinNode(n, root, semanticModel)).Where(n => expander.ShouldExpandNode(n, root, semanticModel)),
                    (node, rewrittenNode) => TryExpandNode(expander, node, root, semanticModel, workspace, cancellationToken)
                );
                return document.WithSyntaxRoot(newRoot);
            } catch (Exception ex) {
                var warningText = "Conversion warning: Qualified name reduction failed for this file. " + ex;
                return document.WithSyntaxRoot(WithWarningAnnotation(root, warningText));
            }
        }

        private static SyntaxNode TryExpandNode(ISyntaxExpander expander, SyntaxNode node, SyntaxNode root, SemanticModel semanticModel, Workspace workspace, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try {
                return expander.ExpandNode(node, root, semanticModel, workspace);
            } catch (Exception ex) {
                var warningText = new ExceptionWithNodeInformation(ex, node, "Conversion warning").ToString();
                return WithWarningAnnotation(node, warningText);
            }
        }

        private static async Task<Document> WithReducedRootAsync(this Document doc, SyntaxNode syntaxRoot, CancellationToken cancellationToken)
        {
            var root = syntaxRoot ?? await doc.GetSyntaxRootAsync(cancellationToken);
            var withSyntaxRoot = doc.WithSyntaxRoot(root);
            try {
                var options = await doc.GetOptionsAsync(cancellationToken);
                var newOptions = doc.Project.Language == LanguageNames.VisualBasic ? GetVBOptions(options) : GetCSOptions(options);
                return await Simplifier.ReduceAsync(withSyntaxRoot, newOptions, cancellationToken: cancellationToken);
            } catch (Exception ex) {
                var warningText = "Conversion warning: Qualified name reduction failed for this file. " + ex;
                return doc.WithSyntaxRoot(WithWarningAnnotation(root, warningText));
            }
        }

        private static SyntaxNode WithWarningAnnotation(SyntaxNode node, string warningText)
        {
            return node.WithAdditionalAnnotations(new SyntaxAnnotation(AnnotationConstants.ConversionErrorAnnotationKind, warningText));
        }
        private static OptionSet GetVBOptions(DocumentOptionSet options) {
            return options.WithChangedOption(SimplificationOptions.PreferImplicitTypeInLocalDeclaration, false);
        }
        private static OptionSet GetCSOptions(DocumentOptionSet options) {
            return options;
        }
    }
}