using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CS = Microsoft.CodeAnalysis.CSharp;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using AnonymousObjectCreationExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.AnonymousObjectCreationExpressionSyntax;
using ArgumentListSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ArgumentListSyntax;
using ArrayRankSpecifierSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ArrayRankSpecifierSyntax;
using AttributeListSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.AttributeListSyntax;
using CastExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.CastExpressionSyntax;
using CompilationUnitSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax;
using ConditionalAccessExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ConditionalAccessExpressionSyntax;
using CSharpExtensions = Microsoft.CodeAnalysis.CSharp.CSharpExtensions;
using VBSyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory;
using VBSyntaxKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;
using CSSyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using DoStatementSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.DoStatementSyntax;
using EmptyStatementSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.EmptyStatementSyntax;
using EnumMemberDeclarationSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.EnumMemberDeclarationSyntax;
using FieldDeclarationSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax;
using ForEachStatementSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ForEachStatementSyntax;
using ForStatementSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ForStatementSyntax;
using IfStatementSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax;
using ParameterListSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ParameterListSyntax;
using ParenthesizedExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ParenthesizedExpressionSyntax;
using StatementSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxFacts = Microsoft.CodeAnalysis.CSharp.SyntaxFacts;
using TypeOfExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.TypeOfExpressionSyntax;
using TypeSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax;
using UsingStatementSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.UsingStatementSyntax;
using WhileStatementSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.WhileStatementSyntax;
using VBCommonConversions = ICSharpCode.CodeConverter.VB.CommonConversions;

namespace ICSharpCode.CodeConverter.Util
{
#if NR6
    public
#endif
    internal static partial class SyntaxNodeExtensions
    {
        public static IEnumerable<SyntaxNode> GetAncestors(this SyntaxNode node)
        {
            var current = node.Parent;

            while (current != null) {
                yield return current;

                current = current is IStructuredTriviaSyntax
                    ? ((IStructuredTriviaSyntax)current).ParentTrivia.Token.Parent
                    : current.Parent;
            }
        }

        public static IEnumerable<TNode> GetAncestors<TNode>(this SyntaxNode node)
            where TNode : SyntaxNode
        {
            var current = node.Parent;
            while (current != null) {
                if (current is TNode) {
                    yield return (TNode)current;
                }

                current = current is IStructuredTriviaSyntax
                    ? ((IStructuredTriviaSyntax)current).ParentTrivia.Token.Parent
                    : current.Parent;
            }
        }

        public static TNode GetAncestor<TNode>(this SyntaxNode node)
            where TNode : SyntaxNode
        {
            if (node == null) {
                return default(TNode);
            }

            return node.GetAncestors<TNode>().FirstOrDefault();
        }

        public static TNode GetAncestorOrThis<TNode>(this SyntaxNode node)
            where TNode : SyntaxNode
        {
            if (node == null) {
                return default(TNode);
            }

            return node.GetAncestorsOrThis<TNode>().FirstOrDefault();
        }

        public static IEnumerable<TNode> GetAncestorsOrThis<TNode>(this SyntaxNode node)
            where TNode : SyntaxNode
        {
            var current = node;
            while (current != null) {
                if (current is TNode) {
                    yield return (TNode)current;
                }

                current = current is IStructuredTriviaSyntax
                    ? ((IStructuredTriviaSyntax)current).ParentTrivia.Token.Parent
                    : current.Parent;
            }
        }

        public static bool HasAncestor<TNode>(this SyntaxNode node)
            where TNode : SyntaxNode
        {
            return node.GetAncestors<TNode>().Any();
        }

        public static bool CheckParent<T>(this SyntaxNode node, Func<T, bool> valueChecker) where T : SyntaxNode
        {
            if (node == null) {
                return false;
            }

            var parentNode = node.Parent as T;
            if (parentNode == null) {
                return false;
            }

            return valueChecker(parentNode);
        }

        /// <summary>
        /// Returns true if is a given token is a child token of of a certain type of parent node.
        /// </summary>
        /// <typeparam name="TParent">The type of the parent node.</typeparam>
        /// <param name="node">The node that we are testing.</param>
        /// <param name="childGetter">A function that, when given the parent node, returns the child token we are interested in.</param>
        public static bool IsChildNode<TParent>(this SyntaxNode node, Func<TParent, SyntaxNode> childGetter)
            where TParent : SyntaxNode
        {
            var ancestor = node.GetAncestor<TParent>();
            if (ancestor == null) {
                return false;
            }

            var ancestorNode = childGetter(ancestor);

            return node == ancestorNode;
        }

        /// <summary>
        /// Returns true if this node is found underneath the specified child in the given parent.
        /// </summary>
        public static bool IsFoundUnder<TParent>(this SyntaxNode node, Func<TParent, SyntaxNode> childGetter)
            where TParent : SyntaxNode
        {
            var ancestor = node.GetAncestor<TParent>();
            if (ancestor == null) {
                return false;
            }

            var child = childGetter(ancestor);

            // See if node passes through child on the way up to ancestor.
            return node.GetAncestorsOrThis<SyntaxNode>().Contains(child);
        }

        public static SyntaxNode GetCommonRoot(this SyntaxNode node1, SyntaxNode node2)
        {
            //Contract.ThrowIfTrue(node1.RawKind == 0 || node2.RawKind == 0);

            // find common starting node from two nodes.
            // as long as two nodes belong to same tree, there must be at least one common root (Ex, compilation unit)
            var ancestors = node1.GetAncestorsOrThis<SyntaxNode>();
            var set = new HashSet<SyntaxNode>(node2.GetAncestorsOrThis<SyntaxNode>());

            return ancestors.First(set.Contains);
        }

        public static int Width(this SyntaxNode node)
        {
            return node.Span.Length;
        }

        public static int FullWidth(this SyntaxNode node)
        {
            return node.FullSpan.Length;
        }

        public static SyntaxNode FindInnermostCommonNode(
            this IEnumerable<SyntaxNode> nodes,
            Func<SyntaxNode, bool> predicate)
        {
            IEnumerable<SyntaxNode> blocks = null;
            foreach (var node in nodes) {
                blocks = blocks == null
                    ? node.AncestorsAndSelf().Where(predicate)
                    : blocks.Intersect(node.AncestorsAndSelf().Where(predicate));
            }

            return blocks == null ? null : blocks.First();
        }

        public static TSyntaxNode FindInnermostCommonNode<TSyntaxNode>(this IEnumerable<SyntaxNode> nodes)
            where TSyntaxNode : SyntaxNode
        {
            return (TSyntaxNode)nodes.FindInnermostCommonNode(n => n is TSyntaxNode);
        }

        public static ISymbol GetEnclosingDeclaredTypeSymbol(this SyntaxNode node, SemanticModel semanticModel)
        {
            var typeBlockSyntax = (SyntaxNode)node.GetAncestor<TypeBlockSyntax>()
                ?? node.GetAncestor<TypeSyntax>();
            if (typeBlockSyntax == null) return null;
            return semanticModel.GetDeclaredSymbol(typeBlockSyntax);
        }

        public static SyntaxList<T> WithSourceMappingFrom<T>(this SyntaxList<T> converted, SyntaxNode node) where T : SyntaxNode
        {
            if (!converted.Any()) return converted;
            var origLinespan = node.SyntaxTree.GetLineSpan(node.Span);
            var first = converted.First();
            converted = converted.Replace(first, node.CopyAnnotationsTo(first).WithSourceStartLineAnnotation(origLinespan));
            var last = converted.Last();
            return converted.Replace(last, last.WithSourceEndLineAnnotation(origLinespan));
        }

        public static T WithSourceMappingFrom<T>(this T converted, SyntaxNodeOrToken fromSource) where T : SyntaxNode
        {
            if (converted == null) return null;
            var startLinespan = fromSource.SyntaxTree.GetLineSpan(fromSource.Span);
            return converted
                .WithSourceStartLineAnnotation(startLinespan)
                .WithSourceEndLineAnnotation(startLinespan);
        }

        public static T WithSourceStartLineAnnotation<T>(this T node, FileLinePositionSpan sourcePosition) where T : SyntaxNode
        {
            return node.WithAdditionalAnnotations(AnnotationConstants.SourceStartLine(sourcePosition));
        }

        public static T WithSourceEndLineAnnotation<T>(this T node, FileLinePositionSpan sourcePosition) where T : SyntaxNode
        {
            return node.WithAdditionalAnnotations(AnnotationConstants.SourceEndLine(sourcePosition));
        }

        public static T WithoutSourceMapping<T>(this T converted) where T : SyntaxNode
        {
            converted = converted.ReplaceTokens(converted.DescendantTokens(), (o, r) =>
                r.WithoutAnnotations(AnnotationConstants.SourceStartLineAnnotationKind).WithoutAnnotations(AnnotationConstants.SourceEndLineAnnotationKind)
            );
            return converted.ReplaceNodes(converted.DescendantNodes(), (o, r) =>
                r.WithoutAnnotations(AnnotationConstants.SourceStartLineAnnotationKind).WithoutAnnotations(AnnotationConstants.SourceEndLineAnnotationKind)
            );
        }

        /// <summary>
        /// create a new root node from the given root after adding annotations to the tokens
        ///
        /// tokens should belong to the given root
        /// </summary>
        public static SyntaxNode AddAnnotations(this SyntaxNode root, IEnumerable<Tuple<SyntaxToken, SyntaxAnnotation>> pairs)
        {
            //            Contract.ThrowIfNull(root);
            //            Contract.ThrowIfNull(pairs);

            var tokenMap = pairs.GroupBy(p => p.Item1, p => p.Item2).ToDictionary(g => g.Key, g => g.ToArray());
            return root.ReplaceTokens(tokenMap.Keys, (o, n) => o.WithAdditionalAnnotations(tokenMap[o]));
        }

        /// <summary>
        /// create a new root node from the given root after adding annotations to the nodes
        ///
        /// nodes should belong to the given root
        /// </summary>
        public static SyntaxNode AddAnnotations(this SyntaxNode root, IEnumerable<Tuple<SyntaxNode, SyntaxAnnotation>> pairs)
        {
            //            Contract.ThrowIfNull(root);
            //            Contract.ThrowIfNull(pairs);

            var tokenMap = pairs.GroupBy(p => p.Item1, p => p.Item2).ToDictionary(g => g.Key, g => g.ToArray());
            return root.ReplaceNodes(tokenMap.Keys, (o, n) => o.WithAdditionalAnnotations(tokenMap[o]));
        }

        public static TextSpan GetContainedSpan(this IEnumerable<SyntaxNode> nodes)
        {
            TextSpan fullSpan = nodes.First().Span;
            foreach (var node in nodes) {
                fullSpan = TextSpan.FromBounds(
                    Math.Min(fullSpan.Start, node.SpanStart),
                    Math.Max(fullSpan.End, node.Span.End));
            }

            return fullSpan;
        }

        public static IEnumerable<TextSpan> GetContiguousSpans(
            this IEnumerable<SyntaxNode> nodes, Func<SyntaxNode, SyntaxToken> getLastToken = null)
        {
            SyntaxNode lastNode = null;
            TextSpan? textSpan = null;
            foreach (var node in nodes) {
                if (lastNode == null) {
                    textSpan = node.Span;
                } else {
                    var lastToken = getLastToken == null
                        ? lastNode.GetLastToken()
                        : getLastToken(lastNode);
                    if (lastToken.GetNextToken(includeDirectives: true) == node.GetFirstToken()) {
                        // Expand the span
                        textSpan = TextSpan.FromBounds(textSpan.Value.Start, node.Span.End);
                    } else {
                        // Return the last span, and start a new one
                        yield return textSpan.Value;
                        textSpan = node.Span;
                    }
                }

                lastNode = node;
            }

            if (textSpan.HasValue) {
                yield return textSpan.Value;
            }
        }

        public static IEnumerable<T> GetAnnotatedNodes<T>(this SyntaxNode node, SyntaxAnnotation syntaxAnnotation) where T : SyntaxNode
        {
            return node.GetAnnotatedNodesAndTokens(syntaxAnnotation).Select(n => n.AsNode()).OfType<T>();
        }

        /// <summary>
        /// Creates a new tree of nodes from the existing tree with the specified old nodes replaced with a newly computed nodes.
        /// </summary>
        /// <param name="root">The root of the tree that contains all the specified nodes.</param>
        /// <param name="nodes">The nodes from the tree to be replaced.</param>
        /// <param name="computeReplacementAsync">A function that computes a replacement node for
        /// the argument nodes. The first argument is one of the original specified nodes. The second argument is
        /// the same node possibly rewritten with replaced descendants.</param>
        /// <param name="cancellationToken"></param>
        public static Task<TRootNode> ReplaceNodesAsync<TRootNode>(
            this TRootNode root,
            IEnumerable<SyntaxNode> nodes,
            Func<SyntaxNode, SyntaxNode, CancellationToken, Task<SyntaxNode>> computeReplacementAsync,
            CancellationToken cancellationToken) where TRootNode : SyntaxNode
        {
            return root.ReplaceSyntaxAsync(
                nodes: nodes, computeReplacementNodeAsync: computeReplacementAsync,
                tokens: null, computeReplacementTokenAsync: null,
                trivia: null, computeReplacementTriviaAsync: null,
                cancellationToken: cancellationToken);
        }

        public static async Task<TRoot> ReplaceSyntaxAsync<TRoot>(
            this TRoot root,
            IEnumerable<SyntaxNode> nodes,
            Func<SyntaxNode, SyntaxNode, CancellationToken, Task<SyntaxNode>> computeReplacementNodeAsync,
            IEnumerable<SyntaxToken> tokens,
            Func<SyntaxToken, SyntaxToken, CancellationToken, Task<SyntaxToken>> computeReplacementTokenAsync,
            IEnumerable<SyntaxTrivia> trivia,
            Func<SyntaxTrivia, SyntaxTrivia, CancellationToken, Task<SyntaxTrivia>> computeReplacementTriviaAsync,
            CancellationToken cancellationToken)
            where TRoot : SyntaxNode
        {
            // index all nodes, tokens and trivia by the full spans they cover
            var nodesToReplace = nodes != null ? nodes.ToDictionary(n => n.FullSpan) : new Dictionary<TextSpan, SyntaxNode>();
            var tokensToReplace = tokens != null ? tokens.ToDictionary(t => t.FullSpan) : new Dictionary<TextSpan, SyntaxToken>();
            var triviaToReplace = trivia != null ? trivia.ToDictionary(t => t.FullSpan) : new Dictionary<TextSpan, SyntaxTrivia>();

            var nodeReplacements = new Dictionary<SyntaxNode, SyntaxNode>();
            var tokenReplacements = new Dictionary<SyntaxToken, SyntaxToken>();
            var triviaReplacements = new Dictionary<SyntaxTrivia, SyntaxTrivia>();

            var retryAnnotations = new AnnotationTable<object>("RetryReplace");

            var spans = new List<TextSpan>(nodesToReplace.Count + tokensToReplace.Count + triviaToReplace.Count);
            spans.AddRange(nodesToReplace.Keys);
            spans.AddRange(tokensToReplace.Keys);
            spans.AddRange(triviaToReplace.Keys);

            while (spans.Count > 0) {
                // sort the spans of the items to be replaced so we can tell if any overlap
                spans.Sort((x, y) => {
                    // order by end offset, and then by length
                    var d = x.End - y.End;

                    if (d == 0) {
                        d = x.Length - y.Length;
                    }

                    return d;
                });

                // compute replacements for all nodes that will go in the same batch
                // only spans that do not overlap go in the same batch.
                TextSpan previous = default(TextSpan);
                foreach (var span in spans) {
                    // only add to replacement map if we don't intersect with the previous node. This taken with the sort order
                    // should ensure that parent nodes are not processed in the same batch as child nodes.
                    if (previous == default(TextSpan) || !previous.IntersectsWith(span)) {
                        SyntaxNode currentNode;
                        SyntaxToken currentToken;
                        SyntaxTrivia currentTrivia;

                        if (nodesToReplace.TryGetValue(span, out currentNode)) {
                            var original = (SyntaxNode)retryAnnotations.GetAnnotations(currentNode).SingleOrDefault() ?? currentNode;
                            var newNode = await computeReplacementNodeAsync(original, currentNode, cancellationToken).ConfigureAwait(false);
                            nodeReplacements[currentNode] = newNode;
                        } else if (tokensToReplace.TryGetValue(span, out currentToken)) {
                            var original = (SyntaxToken)retryAnnotations.GetAnnotations(currentToken).SingleOrDefault();
                            if (original == default(SyntaxToken)) {
                                original = currentToken;
                            }

                            var newToken = await computeReplacementTokenAsync(original, currentToken, cancellationToken).ConfigureAwait(false);
                            tokenReplacements[currentToken] = newToken;
                        } else if (triviaToReplace.TryGetValue(span, out currentTrivia)) {
                            var original = (SyntaxTrivia)retryAnnotations.GetAnnotations(currentTrivia).SingleOrDefault();
                            if (original == default(SyntaxTrivia)) {
                                original = currentTrivia;
                            }

                            var newTrivia = await computeReplacementTriviaAsync(original, currentTrivia, cancellationToken).ConfigureAwait(false);
                            triviaReplacements[currentTrivia] = newTrivia;
                        }
                    }

                    previous = span;
                }

                bool retryNodes = false;
                bool retryTokens = false;
                bool retryTrivia = false;

                // replace nodes in batch
                // submit all nodes so we can annotate the ones we don't replace
                root = root.ReplaceSyntax(
                    nodes: nodesToReplace.Values,
                    computeReplacementNode: (original, rewritten) => {
                        SyntaxNode replaced;
                        if (rewritten != original || !nodeReplacements.TryGetValue(original, out replaced)) {
                            // the subtree did change, or we didn't have a replacement for it in this batch
                            // so we need to add an annotation so we can find this node again for the next batch.
                            replaced = retryAnnotations.WithAdditionalAnnotations(rewritten, original);
                            retryNodes = true;
                        }

                        return replaced;
                    },
                    tokens: tokensToReplace.Values,
                    computeReplacementToken: (original, rewritten) => {
                        SyntaxToken replaced;
                        if (rewritten != original || !tokenReplacements.TryGetValue(original, out replaced)) {
                            // the subtree did change, or we didn't have a replacement for it in this batch
                            // so we need to add an annotation so we can find this node again for the next batch.
                            replaced = retryAnnotations.WithAdditionalAnnotations(rewritten, original);
                            retryTokens = true;
                        }

                        return replaced;
                    },
                    trivia: triviaToReplace.Values,
                    computeReplacementTrivia: (original, rewritten) => {
                        SyntaxTrivia replaced;
                        if (!triviaReplacements.TryGetValue(original, out replaced)) {
                            // the subtree did change, or we didn't have a replacement for it in this batch
                            // so we need to add an annotation so we can find this node again for the next batch.
                            replaced = retryAnnotations.WithAdditionalAnnotations(rewritten, original);
                            retryTrivia = true;
                        }

                        return replaced;
                    });

                nodesToReplace.Clear();
                tokensToReplace.Clear();
                triviaToReplace.Clear();
                spans.Clear();

                // prepare next batch out of all remaining annotated nodes
                if (retryNodes) {
                    nodesToReplace = retryAnnotations.GetAnnotatedNodes(root).ToDictionary(n => n.FullSpan);
                    spans.AddRange(nodesToReplace.Keys);
                }

                if (retryTokens) {
                    tokensToReplace = retryAnnotations.GetAnnotatedTokens(root).ToDictionary(t => t.FullSpan);
                    spans.AddRange(tokensToReplace.Keys);
                }

                if (retryTrivia) {
                    triviaToReplace = retryAnnotations.GetAnnotatedTrivia(root).ToDictionary(t => t.FullSpan);
                    spans.AddRange(triviaToReplace.Keys);
                }
            }

            return root;
        }

        public static bool IsKind(this SyntaxNode node, CSSyntaxKind kind1, CSSyntaxKind kind2)
        {
            if (node == null) {
                return false;
            }

            var csharpKind = CSharpExtensions.Kind(node);
            return csharpKind == kind1 || csharpKind == kind2;
        }

        public static bool IsKind(this SyntaxNode node, CSSyntaxKind kind1, CSSyntaxKind kind2, CSSyntaxKind kind3)
        {
            if (node == null) {
                return false;
            }

            var csharpKind = CSharpExtensions.Kind(node);
            return csharpKind == kind1 || csharpKind == kind2 || csharpKind == kind3;
        }

        public static bool IsKind(this SyntaxNode node, CSSyntaxKind kind1, CSSyntaxKind kind2, CSSyntaxKind kind3, CSSyntaxKind kind4)
        {
            if (node == null) {
                return false;
            }

            var csharpKind = CSharpExtensions.Kind(node);
            return csharpKind == kind1 || csharpKind == kind2 || csharpKind == kind3 || csharpKind == kind4;
        }

        public static bool IsKind(this SyntaxNode node, CSSyntaxKind kind1, CSSyntaxKind kind2, CSSyntaxKind kind3, CSSyntaxKind kind4, CSSyntaxKind kind5)
        {
            if (node == null) {
                return false;
            }

            var csharpKind = CSharpExtensions.Kind(node);
            return csharpKind == kind1 || csharpKind == kind2 || csharpKind == kind3 || csharpKind == kind4 || csharpKind == kind5;
        }

        /// <summary>
        /// Returns the list of using directives that affect <paramref name="node"/>. The list will be returned in
        /// top down order.
        /// </summary>
        public static IEnumerable<UsingDirectiveSyntax> GetEnclosingUsingDirectives(this SyntaxNode node)
        {
            return node.GetAncestorOrThis<CompilationUnitSyntax>().Usings
                .Concat(node.GetAncestorsOrThis<NamespaceDeclarationSyntax>()
                    .Reverse()
                    .SelectMany(n => n.Usings));
        }

        public static bool IsUnsafeContext(this SyntaxNode node)
        {
            if (node.GetAncestor<UnsafeStatementSyntax>() != null) {
                return true;
            }

            return node.GetAncestors<MemberDeclarationSyntax>().Any(
                m => m.GetModifiers().Any(CSSyntaxKind.UnsafeKeyword));
        }

        public static bool IsInStaticCsContext(this SyntaxNode node)
        {
            // this/base calls are always static.
            if (node.FirstAncestorOrSelf<ConstructorInitializerSyntax>() != null) {
                return true;
            }

            var memberDeclaration = node.FirstAncestorOrSelf<MemberDeclarationSyntax>();
            if (memberDeclaration == null) {
                return false;
            }

            switch (memberDeclaration.Kind()) {
                case CSSyntaxKind.MethodDeclaration:
                case CSSyntaxKind.ConstructorDeclaration:
                case CSSyntaxKind.PropertyDeclaration:
                case CSSyntaxKind.EventDeclaration:
                case CSSyntaxKind.IndexerDeclaration:
                    return memberDeclaration.GetModifiers().Any(CSSyntaxKind.StaticKeyword);

                case CSSyntaxKind.FieldDeclaration:
                    // Inside a field one can only access static members of a type.
                    return true;

                case CSSyntaxKind.DestructorDeclaration:
                    return false;
            }

            // Global statements are not a static context.
            if (node.FirstAncestorOrSelf<GlobalStatementSyntax>() != null) {
                return false;
            }

            // any other location is considered static
            return true;
        }

        public static NamespaceDeclarationSyntax GetInnermostNamespaceDeclarationWithUsings(this SyntaxNode contextNode)
        {
            var usingDirectiveAncsestor = contextNode.GetAncestor<UsingDirectiveSyntax>();
            if (usingDirectiveAncsestor == null) {
                return contextNode.GetAncestorsOrThis<NamespaceDeclarationSyntax>().FirstOrDefault(n => n.Usings.Count > 0);
            } else {
                // We are inside a using directive. In this case, we should find and return the first 'parent' namespace with usings.
                var containingNamespace = usingDirectiveAncsestor.GetAncestor<NamespaceDeclarationSyntax>();
                if (containingNamespace == null) {
                    // We are inside a top level using directive (i.e. one that's directly in the compilation unit).
                    return null;
                } else {
                    return containingNamespace.GetAncestors<NamespaceDeclarationSyntax>().FirstOrDefault(n => n.Usings.Count > 0);
                }
            }
        }

        // Matches the following:
        //
        // (whitespace* newline)+
        private static readonly Matcher<SyntaxTrivia> s_oneOrMoreBlankLines;

        // Matches the following:
        //
        // (whitespace* (single-comment|multi-comment) whitespace* newline)+ OneOrMoreBlankLines
        private static readonly Matcher<SyntaxTrivia> s_bannerMatcher;

        static SyntaxNodeExtensions()
        {
            var whitespace = Matcher.Repeat(Match(CSSyntaxKind.WhitespaceTrivia, "\\b"));
            var endOfLine = Match(CSSyntaxKind.EndOfLineTrivia, "\\n");
            var singleBlankLine = Matcher.Sequence(whitespace, endOfLine);

            var singleLineComment = Match(CSSyntaxKind.SingleLineCommentTrivia, "//");
            var multiLineComment = Match(CSSyntaxKind.MultiLineCommentTrivia, "/**/");
            var anyCommentMatcher = Matcher.Choice(singleLineComment, multiLineComment);

            var commentLine = Matcher.Sequence(whitespace, anyCommentMatcher, whitespace, endOfLine);

            s_oneOrMoreBlankLines = Matcher.OneOrMore(singleBlankLine);
            s_bannerMatcher =
                Matcher.Sequence(
                    Matcher.OneOrMore(commentLine),
                    s_oneOrMoreBlankLines);
        }

        private static Matcher<SyntaxTrivia> Match(CSSyntaxKind kind, string description)
        {
            return Matcher.Single<SyntaxTrivia>(t => CSharpExtensions.Kind(t) == kind, description);
        }

        /// <summary>
        /// Returns all of the trivia to the left of this token up to the previous token (concatenates
        /// the previous token's trailing trivia and this token's leading trivia).
        /// </summary>
        public static IEnumerable<SyntaxTrivia> GetAllPrecedingTriviaToPreviousToken(this SyntaxToken token)
        {
            var prevToken = token.GetPreviousToken(includeSkipped: true);
            if (CSharpExtensions.Kind(prevToken) == CSSyntaxKind.None) {
                return token.LeadingTrivia;
            }

            return prevToken.TrailingTrivia.Concat(token.LeadingTrivia);
        }

        public static bool IsBreakableConstruct(this SyntaxNode node)
        {
            switch (CSharpExtensions.Kind(node)) {
                case CSSyntaxKind.DoStatement:
                case CSSyntaxKind.WhileStatement:
                case CSSyntaxKind.SwitchStatement:
                case CSSyntaxKind.ForStatement:
                case CSSyntaxKind.ForEachStatement:
                    return true;
            }

            return false;
        }

        public static bool IsContinuableConstruct(this SyntaxNode node)
        {
            switch (CSharpExtensions.Kind(node)) {
                case CSSyntaxKind.DoStatement:
                case CSSyntaxKind.WhileStatement:
                case CSSyntaxKind.ForStatement:
                case CSSyntaxKind.ForEachStatement:
                    return true;
            }

            return false;
        }

        public static bool IsReturnableConstruct(this SyntaxNode node)
        {
            switch (CSharpExtensions.Kind(node)) {
                case CSSyntaxKind.AnonymousMethodExpression:
                case CSSyntaxKind.SimpleLambdaExpression:
                case CSSyntaxKind.ParenthesizedLambdaExpression:
                case CSSyntaxKind.MethodDeclaration:
                case CSSyntaxKind.ConstructorDeclaration:
                case CSSyntaxKind.DestructorDeclaration:
                case CSSyntaxKind.GetAccessorDeclaration:
                case CSSyntaxKind.SetAccessorDeclaration:
                case CSSyntaxKind.OperatorDeclaration:
                case CSSyntaxKind.AddAccessorDeclaration:
                case CSSyntaxKind.RemoveAccessorDeclaration:
                    return true;
            }

            return false;
        }

        public static bool SpansPreprocessorDirective<TSyntaxNode>(
            this IEnumerable<TSyntaxNode> list)
            where TSyntaxNode : SyntaxNode
        {
            if (list == null || !list.Any()) {
                return false;
            }

            var tokens = list.SelectMany(n => n.DescendantTokens());

            // todo: we need to dive into trivia here.
            return tokens.SpansPreprocessorDirective();
        }

        public static T WithPrependedLeadingTrivia<T>(
            this T node,
            params SyntaxTrivia[] trivia) where T : SyntaxNode
        {
            if (trivia.Length == 0) {
                return node;
            }

            return node.WithPrependedLeadingTrivia((IEnumerable<SyntaxTrivia>)trivia);
        }

        public static T WithPrependedLeadingTrivia<T>(
            this T node,
            SyntaxTriviaList trivia) where T : SyntaxNode
        {
            if (trivia.Count == 0) {
                return node;
            }

            return node.WithLeadingTrivia(trivia.Concat(node.GetLeadingTrivia()));
        }

        public static T WithPrependedLeadingTrivia<T>(
            this T node,
            IEnumerable<SyntaxTrivia> trivia) where T : SyntaxNode
        {
            return node.WithPrependedLeadingTrivia(Microsoft.CodeAnalysis.CSharp.SyntaxExtensions.ToSyntaxTriviaList(trivia));
        }

        public static T WithAppendedTrailingTrivia<T>(
            this T node,
            params SyntaxTrivia[] trivia) where T : SyntaxNode
        {
            if (trivia.Length == 0) {
                return node;
            }

            return node.WithAppendedTrailingTrivia((IEnumerable<SyntaxTrivia>)trivia);
        }

        public static T WithAppendedTrailingTrivia<T>(
            this T node,
            SyntaxTriviaList trivia) where T : SyntaxNode
        {
            if (trivia.Count == 0) {
                return node;
            }

            return node.WithTrailingTrivia(node.GetTrailingTrivia().Concat(trivia));
        }

        public static T WithAppendedTrailingTrivia<T>(
            this T node,
            IEnumerable<SyntaxTrivia> trivia) where T : SyntaxNode
        {
            return node.WithAppendedTrailingTrivia(Microsoft.CodeAnalysis.CSharp.SyntaxExtensions.ToSyntaxTriviaList(trivia));
        }

        public static T With<T>(
            this T node,
            IEnumerable<SyntaxTrivia> leadingTrivia,
            IEnumerable<SyntaxTrivia> trailingTrivia) where T : SyntaxNode
        {
            return node.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(trailingTrivia);
        }
        public static SyntaxToken WithConvertedTriviaFrom(this SyntaxToken node, SyntaxNode otherNode)
        {
            return node.WithConvertedLeadingTriviaFrom(otherNode).WithConvertedTrailingTriviaFrom(otherNode);
        }

        public static T WithConvertedLeadingTriviaFrom<T>(this T node, SyntaxToken fromToken) where T : SyntaxNode
        {
            var firstConvertedToken = node.GetFirstToken();
            return node.ReplaceToken(firstConvertedToken, firstConvertedToken.WithConvertedLeadingTriviaFrom(fromToken));
        }

        public static SyntaxToken WithConvertedLeadingTriviaFrom(this SyntaxToken node, SyntaxNode otherNode)
        {
            var firstToken = otherNode?.GetFirstToken();
            return WithConvertedLeadingTriviaFrom(node, firstToken);
        }

        public static SyntaxToken WithConvertedLeadingTriviaFrom(this SyntaxToken node, SyntaxToken? sourceToken)
        {
            if (sourceToken == null) return node;
            var convertedTrivia = ConvertTrivia(sourceToken.Value.LeadingTrivia);
            return node.WithLeadingTrivia(convertedTrivia);
        }

        public static T WithConvertedTrailingTriviaFrom<T>(this T node, SyntaxToken fromToken) where T : SyntaxNode
        {
            var lastConvertedToken = node.GetLastToken();
            return node.ReplaceToken(lastConvertedToken, lastConvertedToken.WithConvertedTrailingTriviaFrom(fromToken));
        }

        public static SyntaxToken WithConvertedTrailingTriviaFrom(this SyntaxToken node, SyntaxNode otherNode)
        {
            return node.WithConvertedTrailingTriviaFrom(otherNode?.GetLastToken());
        }

        public static SyntaxToken WithConvertedTrailingTriviaFrom(this SyntaxToken node, SyntaxToken? otherToken)
        {
            if (!otherToken.HasValue || !otherToken.Value.HasTrailingTrivia) return node;
            var convertedTrivia = ConvertTrivia(otherToken.Value.TrailingTrivia);
            return node.WithTrailingTrivia(node.ImportantTrailingTrivia().Concat(convertedTrivia));
        }

        public static IEnumerable<SyntaxTrivia> ImportantTrailingTrivia(this SyntaxToken node)
        {
            return node.TrailingTrivia.Where(x => !x.IsWhitespaceOrEndOfLine());
        }

        public static bool ParentHasSameTrailingTrivia(this SyntaxNode otherNode)
        {
            return otherNode.Parent.GetLastToken() == otherNode.GetLastToken();
        }

        public static IEnumerable<SyntaxTrivia> ConvertTrivia(this IReadOnlyCollection<SyntaxTrivia> triviaToConvert)
        {
            if (triviaToConvert.Any() && triviaToConvert.First().Language == LanguageNames.CSharp) {
                return CSharpToVBCodeConverter.Util.RecursiveTriviaConverter.ConvertTopLevel(triviaToConvert).Where(x => x != default(SyntaxTrivia));
            }
            return triviaToConvert.SelectMany(ConvertVBTrivia).Where(x => x != default(SyntaxTrivia));
        }

        private static IEnumerable<SyntaxTrivia> ConvertVBTrivia(SyntaxTrivia t)
        {
            if (t.IsKind(VBSyntaxKind.CommentTrivia)) {
                yield return SyntaxFactory.SyntaxTrivia(CSSyntaxKind.SingleLineCommentTrivia, $"// {t.GetCommentText()}");
                yield return SyntaxFactory.ElasticCarriageReturnLineFeed;
                yield break;
            }
            if (t.IsKind(VBSyntaxKind.DocumentationCommentTrivia)) {
                var previousWhitespace = t.GetPreviousTrivia(t.SyntaxTree, CancellationToken.None).ToString();
                var commentTextLines = t.GetCommentText().Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
                var outputCommentText = "/// " + String.Join($"\r\n{previousWhitespace}/// ", commentTextLines) + Environment.NewLine;
                yield return SyntaxFactory.SyntaxTrivia(CSSyntaxKind.SingleLineCommentTrivia, outputCommentText); //It's always single line...even when it has multiple lines
                yield return SyntaxFactory.ElasticCarriageReturnLineFeed;
                yield break;
            }

            if (t.IsKind(VBSyntaxKind.WhitespaceTrivia)) {
                yield return SyntaxFactory.SyntaxTrivia(CSSyntaxKind.WhitespaceTrivia, t.ToString());
                yield break;
            }

            if (t.IsKind(VBSyntaxKind.EndOfLineTrivia)) {
                // Mapping one to one here leads to newlines appearing where the natural line-end was in VB.
                // e.g. ToString\r\n()
                // Because C Sharp needs those brackets. Handling each possible case of this is far more effort than it's worth.
                yield return SyntaxFactory.SyntaxTrivia(CSSyntaxKind.EndOfLineTrivia, t.ToString());
                yield break;
            }

            //Each of these would need its own method to recreate for C# with the right structure probably so let's just warn about them for now.
            var convertedKind = t.GetCSKind();
            yield return convertedKind.HasValue
                ? SyntaxFactory.Comment($"/* TODO ERROR: Skipped {convertedKind.Value} */")
                : default(SyntaxTrivia);
        }

        public static T WithoutTrailingEndOfLineTrivia<T>(this T cSharpNode) where T : CSharpSyntaxNode
        {
            var lastDescendant = cSharpNode.DescendantNodesAndTokens().Last();
            var triviaWithoutNewline = lastDescendant.GetTrailingTrivia().Where(t => !t.IsKind(CSSyntaxKind.EndOfLineTrivia));
            if (lastDescendant.IsNode) {
                return cSharpNode.ReplaceNode(lastDescendant.AsNode(),
                    lastDescendant.AsNode().WithTrailingTrivia(triviaWithoutNewline));
            }
            return cSharpNode.ReplaceToken(lastDescendant.AsToken(),
                lastDescendant.AsToken().WithTrailingTrivia(triviaWithoutNewline));
        }

        public static T WithOrderedTriviaFromSubTree<T>(
            this T node,
            SyntaxNode subTree) where T : SyntaxNode
        {
            if (!subTree.Contains(node))
                throw new InvalidOperationException(nameof(node) + " must be a descendant of " + nameof(subTree));

            var location = node.FullSpan;
            var leadingTrivia = new List<SyntaxTrivia>();
            var trailingTrivia = new List<SyntaxTrivia>();

            bool wasWSOrEOL = false;

            foreach (var trivia in subTree.DescendantTrivia()) {
                // ignore superfluous eol
                if (wasWSOrEOL && trivia.IsWhitespaceOrEndOfLine())
                    continue;
                if (trivia.Span.End <= location.Start)
                    leadingTrivia.Add(trivia);
                else if (trivia.Span.Start >= location.End)
                    trailingTrivia.Add(trivia);
                wasWSOrEOL = trivia.IsWhitespaceOrEndOfLine();
            }

            return node.With(leadingTrivia.Concat(node.GetLeadingTrivia()), node.GetTrailingTrivia().Concat(trailingTrivia));
        }

        public static TNode ConvertToSingleLine<TNode>(this TNode node)
            where TNode : SyntaxNode
        {
            if (node == null) {
                return node;
            }

            var rewriter = new SingleLineRewriter();
            return (TNode)rewriter.Visit(node);
        }

        internal class SingleLineRewriter : CSharpSyntaxRewriter
        {
            private bool _lastTokenEndedInWhitespace;

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                if (_lastTokenEndedInWhitespace) {
                    token = token.WithLeadingTrivia(Enumerable.Empty<SyntaxTrivia>());
                } else if (token.LeadingTrivia.Count > 0) {
                    token = token.WithLeadingTrivia(SyntaxFactory.Space);
                }

                if (token.TrailingTrivia.Count > 0) {
                    token = token.WithTrailingTrivia(SyntaxFactory.Space);
                    _lastTokenEndedInWhitespace = true;
                } else {
                    _lastTokenEndedInWhitespace = false;
                }

                return token;
            }
        }

        public static bool IsAnyArgumentList(this SyntaxNode node)
        {
            return node.IsKind(CSSyntaxKind.ArgumentList) ||
                node.IsKind(CSSyntaxKind.AttributeArgumentList) ||
                node.IsKind(CSSyntaxKind.BracketedArgumentList) ||
                node.IsKind(CSSyntaxKind.TypeArgumentList);
        }

        public static bool IsAnyLambda(this SyntaxNode node)
        {
            return
                node.IsKind(CSSyntaxKind.ParenthesizedLambdaExpression) ||
                node.IsKind(CSSyntaxKind.SimpleLambdaExpression);
        }

        public static bool IsAnyLambdaOrAnonymousMethod(this SyntaxNode node)
        {
            return node.IsAnyLambda() || node.IsKind(CSSyntaxKind.AnonymousMethodExpression);
        }

        public static IEnumerable<SyntaxTrivia> GetLeadingBannerAndPreprocessorDirectives<TSyntaxNode>(
            this TSyntaxNode node)
            where TSyntaxNode : SyntaxNode
        {
            IEnumerable<SyntaxTrivia> leadingTrivia;
            node.GetNodeWithoutLeadingBannerAndPreprocessorDirectives(out leadingTrivia);
            return leadingTrivia;
        }

        public static TSyntaxNode GetNodeWithoutLeadingBannerAndPreprocessorDirectives<TSyntaxNode>(
            this TSyntaxNode node)
            where TSyntaxNode : SyntaxNode
        {
            IEnumerable<SyntaxTrivia> strippedTrivia;
            return node.GetNodeWithoutLeadingBannerAndPreprocessorDirectives(out strippedTrivia);
        }

        public static TSyntaxNode GetNodeWithoutLeadingBannerAndPreprocessorDirectives<TSyntaxNode>(
            this TSyntaxNode node, out IEnumerable<SyntaxTrivia> strippedTrivia)
            where TSyntaxNode : SyntaxNode
        {
            var leadingTrivia = node.GetLeadingTrivia();

            // Rules for stripping trivia:
            // 1) If there is a pp directive, then it (and all preceding trivia) *must* be stripped.
            //    This rule supersedes all other rules.
            // 2) If there is a doc comment, it cannot be stripped.  Even if there is a doc comment,
            //    followed by 5 new lines, then the doc comment still must stay with the node.  This
            //    rule does *not* supersede rule 1.
            // 3) Single line comments in a group (i.e. with no blank lines between them) belong to
            //    the node *iff* there is no blank line between it and the following trivia.

            List<SyntaxTrivia> leadingTriviaToStrip, leadingTriviaToKeep;

            int ppIndex = -1;
            for (int i = leadingTrivia.Count - 1; i >= 0; i--) {
                if (SyntaxFacts.IsPreprocessorDirective(CSharpExtensions.Kind(leadingTrivia[i]))) {
                    ppIndex = i;
                    break;
                }
            }

            if (ppIndex != -1) {
                // We have a pp directive.  it (and all all previous trivia) must be stripped.
                leadingTriviaToStrip = new List<SyntaxTrivia>(leadingTrivia.Take(ppIndex + 1));
                leadingTriviaToKeep = new List<SyntaxTrivia>(leadingTrivia.Skip(ppIndex + 1));
            } else {
                leadingTriviaToKeep = new List<SyntaxTrivia>(leadingTrivia);
                leadingTriviaToStrip = new List<SyntaxTrivia>();
            }

            // Now, consume as many banners as we can.
            var index = 0;
            while (
                s_oneOrMoreBlankLines.TryMatch(leadingTriviaToKeep, ref index) ||
                s_bannerMatcher.TryMatch(leadingTriviaToKeep, ref index)) {
            }

            leadingTriviaToStrip.AddRange(leadingTriviaToKeep.Take(index));

            strippedTrivia = leadingTriviaToStrip;
            return node.WithLeadingTrivia(leadingTriviaToKeep.Skip(index));
        }

        public static bool IsAnyAssignExpression(this SyntaxNode node)
        {
            return SyntaxFacts.IsAssignmentExpression(CSharpExtensions.Kind(node));
        }

        public static bool IsCompoundAssignExpression(this SyntaxNode node)
        {
            switch (CSharpExtensions.Kind(node)) {
                case CSSyntaxKind.AddAssignmentExpression:
                case CSSyntaxKind.SubtractAssignmentExpression:
                case CSSyntaxKind.MultiplyAssignmentExpression:
                case CSSyntaxKind.DivideAssignmentExpression:
                case CSSyntaxKind.ModuloAssignmentExpression:
                case CSSyntaxKind.AndAssignmentExpression:
                case CSSyntaxKind.ExclusiveOrAssignmentExpression:
                case CSSyntaxKind.OrAssignmentExpression:
                case CSSyntaxKind.LeftShiftAssignmentExpression:
                case CSSyntaxKind.RightShiftAssignmentExpression:
                    return true;
            }

            return false;
        }

        public static bool IsLeftSideOfAssignExpression(this SyntaxNode node)
        {
            return node.IsParentKind(CSSyntaxKind.SimpleAssignmentExpression) &&
                ((AssignmentExpressionSyntax)node.Parent).Left == node;
        }

        public static bool IsLeftSideOfAnyAssignExpression(this SyntaxNode node)
        {
            return node.Parent.IsAnyAssignExpression() &&
                ((AssignmentExpressionSyntax)node.Parent).Left == node;
        }

        public static bool IsRightSideOfAnyAssignExpression(this SyntaxNode node)
        {
            return node.Parent.IsAnyAssignExpression() &&
                ((AssignmentExpressionSyntax)node.Parent).Right == node;
        }

        public static bool IsVariableDeclaratorValue(this SyntaxNode node)
        {
            return
                node.IsParentKind(CSSyntaxKind.EqualsValueClause) &&
                node.Parent.IsParentKind(CSSyntaxKind.VariableDeclarator) &&
                ((EqualsValueClauseSyntax)node.Parent).Value == node;
        }

        public static BlockSyntax FindInnermostCommonBlock(this IEnumerable<SyntaxNode> nodes)
        {
            return nodes.FindInnermostCommonNode<BlockSyntax>();
        }

        public static IEnumerable<SyntaxNode> GetAncestorsOrThis(this SyntaxNode node, Func<SyntaxNode, bool> predicate)
        {
            var current = node;
            while (current != null) {
                if (predicate(current)) {
                    yield return current;
                }

                current = current.Parent;
            }
        }

        /// <summary>
        /// If the position is inside of token, return that token; otherwise, return the token to the right.
        /// </summary>
        public static SyntaxToken FindTokenOnRightOfPosition(
            this SyntaxNode root,
            int position,
            bool includeSkipped = true,
            bool includeDirectives = false,
            bool includeDocumentationComments = false)
        {
            var skippedTokenFinder = includeSkipped ? SyntaxTriviaExtensions.s_findSkippedTokenForward : (Func<SyntaxTriviaList, int, SyntaxToken>)null;

            return FindTokenHelper.FindTokenOnRightOfPosition<CompilationUnitSyntax>(
                root, position, skippedTokenFinder, includeSkipped, includeDirectives, includeDocumentationComments);
        }

        /// <summary>
        /// Returns child node or token that contains given position.
        /// </summary>
        /// <remarks>
        /// This is a copy of <see cref="SyntaxNode.ChildThatContainsPosition"/> that also returns the index of the child node.
        /// </remarks>
        internal static SyntaxNodeOrToken ChildThatContainsPosition(this SyntaxNode self, int position, out int childIndex)
        {
            var childList = self.ChildNodesAndTokens();

            int left = 0;
            int right = childList.Count - 1;

            while (left <= right) {
                int middle = left + ((right - left) / 2);
                SyntaxNodeOrToken node = childList.ElementAt(middle);

                var span = node.FullSpan;
                if (position < span.Start) {
                    right = middle - 1;
                } else if (position >= span.End) {
                    left = middle + 1;
                } else {
                    childIndex = middle;
                    return node;
                }
            }

            // we could check up front that index is within FullSpan,
            // but we wan to optimize for the common case where position is valid.
            Debug.Assert(!self.FullSpan.Contains(position), "Position is valid. How could we not find a child?");
            throw new ArgumentOutOfRangeException("position");
        }

        public static SyntaxNode GetParent(this SyntaxNode node)
        {
            return node != null ? node.Parent : null;
        }

        public static ValueTuple<SyntaxToken, SyntaxToken> GetBraces(this SyntaxNode node)
        {
            var namespaceNode = node as NamespaceDeclarationSyntax;
            if (namespaceNode != null) {
                return ValueTuple.Create(namespaceNode.OpenBraceToken, namespaceNode.CloseBraceToken);
            }

            var baseTypeNode = node as BaseTypeDeclarationSyntax;
            if (baseTypeNode != null) {
                return ValueTuple.Create(baseTypeNode.OpenBraceToken, baseTypeNode.CloseBraceToken);
            }

            var accessorListNode = node as AccessorListSyntax;
            if (accessorListNode != null) {
                return ValueTuple.Create(accessorListNode.OpenBraceToken, accessorListNode.CloseBraceToken);
            }

            var blockNode = node as BlockSyntax;
            if (blockNode != null) {
                return ValueTuple.Create(blockNode.OpenBraceToken, blockNode.CloseBraceToken);
            }

            var switchStatementNode = node as SwitchStatementSyntax;
            if (switchStatementNode != null) {
                return ValueTuple.Create(switchStatementNode.OpenBraceToken, switchStatementNode.CloseBraceToken);
            }

            var anonymousObjectCreationExpression = node as AnonymousObjectCreationExpressionSyntax;
            if (anonymousObjectCreationExpression != null) {
                return ValueTuple.Create(anonymousObjectCreationExpression.OpenBraceToken, anonymousObjectCreationExpression.CloseBraceToken);
            }

            var initializeExpressionNode = node as InitializerExpressionSyntax;
            if (initializeExpressionNode != null) {
                return ValueTuple.Create(initializeExpressionNode.OpenBraceToken, initializeExpressionNode.CloseBraceToken);
            }

            return new ValueTuple<SyntaxToken, SyntaxToken>();
        }

        public static ValueTuple<SyntaxToken, SyntaxToken> GetParentheses(this SyntaxNode node)
        {
            return node.TypeSwitch(
                (ParenthesizedExpressionSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (MakeRefExpressionSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (RefTypeExpressionSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (RefValueExpressionSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (CheckedExpressionSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (DefaultExpressionSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (TypeOfExpressionSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (SizeOfExpressionSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (ArgumentListSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (CastExpressionSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (WhileStatementSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (DoStatementSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (ForStatementSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (ForEachStatementSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (UsingStatementSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (FixedStatementSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (LockStatementSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (IfStatementSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (SwitchStatementSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (CatchDeclarationSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (AttributeArgumentListSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (ConstructorConstraintSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (ParameterListSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (SyntaxNode n) => default(ValueTuple<SyntaxToken, SyntaxToken>));
        }

        public static ValueTuple<SyntaxToken, SyntaxToken> GetBrackets(this SyntaxNode node)
        {
            return node.TypeSwitch(
                (ArrayRankSpecifierSyntax n) => ValueTuple.Create(n.OpenBracketToken, n.CloseBracketToken),
                (BracketedArgumentListSyntax n) => ValueTuple.Create(n.OpenBracketToken, n.CloseBracketToken),
                (ImplicitArrayCreationExpressionSyntax n) => ValueTuple.Create(n.OpenBracketToken, n.CloseBracketToken),
                (AttributeListSyntax n) => ValueTuple.Create(n.OpenBracketToken, n.CloseBracketToken),
                (BracketedParameterListSyntax n) => ValueTuple.Create(n.OpenBracketToken, n.CloseBracketToken),
                (SyntaxNode n) => default(ValueTuple<SyntaxToken, SyntaxToken>));
        }

        public static bool IsEmbeddedStatementOwner(this SyntaxNode node)
        {
            return node is IfStatementSyntax ||
                node is ElseClauseSyntax ||
                node is WhileStatementSyntax ||
                node is ForStatementSyntax ||
                node is ForEachStatementSyntax ||
                node is UsingStatementSyntax ||
                node is DoStatementSyntax;
        }

        public static StatementSyntax GetEmbeddedStatement(this SyntaxNode node)
        {
            return node.TypeSwitch(
                (IfStatementSyntax n) => n.Statement,
                (ElseClauseSyntax n) => n.Statement,
                (WhileStatementSyntax n) => n.Statement,
                (ForStatementSyntax n) => n.Statement,
                (ForEachStatementSyntax n) => n.Statement,
                (UsingStatementSyntax n) => n.Statement,
                (DoStatementSyntax n) => n.Statement,
                (SyntaxNode n) => null);
        }

        public static SyntaxTokenList GetModifiers(this CSharpSyntaxNode member)
        {
            if (member != null) {
                switch (CSharpExtensions.Kind(member)) {
                    case CSSyntaxKind.EnumDeclaration:
                        return ((EnumDeclarationSyntax)member).Modifiers;
                    case CSSyntaxKind.ClassDeclaration:
                    case CSSyntaxKind.InterfaceDeclaration:
                    case CSSyntaxKind.StructDeclaration:
                        return ((TypeDeclarationSyntax)member).Modifiers;
                    case CSSyntaxKind.DelegateDeclaration:
                        return ((DelegateDeclarationSyntax)member).Modifiers;
                    case CSSyntaxKind.FieldDeclaration:
                        return ((FieldDeclarationSyntax)member).Modifiers;
                    case CSSyntaxKind.EventFieldDeclaration:
                        return ((EventFieldDeclarationSyntax)member).Modifiers;
                    case CSSyntaxKind.ConstructorDeclaration:
                        return ((ConstructorDeclarationSyntax)member).Modifiers;
                    case CSSyntaxKind.DestructorDeclaration:
                        return ((DestructorDeclarationSyntax)member).Modifiers;
                    case CSSyntaxKind.PropertyDeclaration:
                        return ((PropertyDeclarationSyntax)member).Modifiers;
                    case CSSyntaxKind.EventDeclaration:
                        return ((EventDeclarationSyntax)member).Modifiers;
                    case CSSyntaxKind.IndexerDeclaration:
                        return ((IndexerDeclarationSyntax)member).Modifiers;
                    case CSSyntaxKind.OperatorDeclaration:
                        return ((OperatorDeclarationSyntax)member).Modifiers;
                    case CSSyntaxKind.ConversionOperatorDeclaration:
                        return ((ConversionOperatorDeclarationSyntax)member).Modifiers;
                    case CSSyntaxKind.MethodDeclaration:
                        return ((MethodDeclarationSyntax)member).Modifiers;
                    case CSSyntaxKind.GetAccessorDeclaration:
                    case CSSyntaxKind.SetAccessorDeclaration:
                    case CSSyntaxKind.AddAccessorDeclaration:
                    case CSSyntaxKind.RemoveAccessorDeclaration:
                        return ((AccessorDeclarationSyntax)member).Modifiers;
                }
            }

            return default(SyntaxTokenList);
        }

        public static SyntaxNode WithModifiers(this SyntaxNode member, SyntaxTokenList modifiers)
        {
            if (member != null) {
                switch (CSharpExtensions.Kind(member)) {
                    case CSSyntaxKind.EnumDeclaration:
                        return ((EnumDeclarationSyntax)member).WithModifiers(modifiers);
                    case CSSyntaxKind.ClassDeclaration:
                    case CSSyntaxKind.InterfaceDeclaration:
                    case CSSyntaxKind.StructDeclaration:
                        return ((TypeDeclarationSyntax)member).WithModifiers(modifiers);
                    case CSSyntaxKind.DelegateDeclaration:
                        return ((DelegateDeclarationSyntax)member).WithModifiers(modifiers);
                    case CSSyntaxKind.FieldDeclaration:
                        return ((FieldDeclarationSyntax)member).WithModifiers(modifiers);
                    case CSSyntaxKind.EventFieldDeclaration:
                        return ((EventFieldDeclarationSyntax)member).WithModifiers(modifiers);
                    case CSSyntaxKind.ConstructorDeclaration:
                        return ((ConstructorDeclarationSyntax)member).WithModifiers(modifiers);
                    case CSSyntaxKind.DestructorDeclaration:
                        return ((DestructorDeclarationSyntax)member).WithModifiers(modifiers);
                    case CSSyntaxKind.PropertyDeclaration:
                        return ((PropertyDeclarationSyntax)member).WithModifiers(modifiers);
                    case CSSyntaxKind.EventDeclaration:
                        return ((EventDeclarationSyntax)member).WithModifiers(modifiers);
                    case CSSyntaxKind.IndexerDeclaration:
                        return ((IndexerDeclarationSyntax)member).WithModifiers(modifiers);
                    case CSSyntaxKind.OperatorDeclaration:
                        return ((OperatorDeclarationSyntax)member).WithModifiers(modifiers);
                    case CSSyntaxKind.ConversionOperatorDeclaration:
                        return ((ConversionOperatorDeclarationSyntax)member).WithModifiers(modifiers);
                    case CSSyntaxKind.MethodDeclaration:
                        return ((MethodDeclarationSyntax)member).WithModifiers(modifiers);
                    case CSSyntaxKind.GetAccessorDeclaration:
                    case CSSyntaxKind.SetAccessorDeclaration:
                    case CSSyntaxKind.AddAccessorDeclaration:
                    case CSSyntaxKind.RemoveAccessorDeclaration:
                        return ((AccessorDeclarationSyntax)member).WithModifiers(modifiers);
                }
            }

            return null;
        }

        public static TypeDeclarationSyntax WithModifiers(
            this TypeDeclarationSyntax node, SyntaxTokenList modifiers)
        {
            switch (node.Kind()) {
                case CSSyntaxKind.ClassDeclaration:
                    return ((ClassDeclarationSyntax)node).WithModifiers(modifiers);
                case CSSyntaxKind.InterfaceDeclaration:
                    return ((InterfaceDeclarationSyntax)node).WithModifiers(modifiers);
                case CSSyntaxKind.StructDeclaration:
                    return ((StructDeclarationSyntax)node).WithModifiers(modifiers);
            }

            throw new InvalidOperationException();
        }

        public static bool CheckTopLevel(this SyntaxNode node, TextSpan span)
        {
            var block = node as BlockSyntax;
            if (block != null) {
                return block.ContainsInBlockBody(span);
            }

            var field = node as FieldDeclarationSyntax;
            if (field != null) {
                foreach (var variable in field.Declaration.Variables) {
                    if (variable.Initializer != null && variable.Initializer.Span.Contains(span)) {
                        return true;
                    }
                }
            }

            var global = node as GlobalStatementSyntax;
            if (global != null) {
                return true;
            }

            var constructorInitializer = node as ConstructorInitializerSyntax;
            if (constructorInitializer != null) {
                return constructorInitializer.ContainsInArgument(span);
            }

            return false;
        }

        public static bool ContainsInArgument(this ConstructorInitializerSyntax initializer, TextSpan textSpan)
        {
            if (initializer == null) {
                return false;
            }

            return initializer.ArgumentList.Arguments.Any(a => a.Span.Contains(textSpan));
        }

        public static bool ContainsInBlockBody(this BlockSyntax block, TextSpan textSpan)
        {
            if (block == null) {
                return false;
            }

            var blockSpan = TextSpan.FromBounds(block.OpenBraceToken.Span.End, block.CloseBraceToken.SpanStart);
            return blockSpan.Contains(textSpan);
        }

        public static IEnumerable<MemberDeclarationSyntax> GetMembers(this SyntaxNode node)
        {
            var compilation = node as CompilationUnitSyntax;
            if (compilation != null) {
                return compilation.Members;
            }

            var @namespace = node as NamespaceDeclarationSyntax;
            if (@namespace != null) {
                return @namespace.Members;
            }

            var type = node as TypeDeclarationSyntax;
            if (type != null) {
                return type.Members;
            }

            var @enum = node as EnumDeclarationSyntax;
            if (@enum != null) {
                return @enum.Members;
            }

            return SpecializedCollections.EmptyEnumerable<MemberDeclarationSyntax>();
        }

        public static IEnumerable<SyntaxNode> GetBodies(this SyntaxNode node)
        {
            var constructor = node as ConstructorDeclarationSyntax;
            if (constructor != null) {
                var result = SpecializedCollections.SingletonEnumerable<SyntaxNode>(constructor.Body).WhereNotNull();
                var initializer = constructor.Initializer;
                if (initializer != null) {
                    result = result.Concat(initializer.ArgumentList.Arguments.Select(a => (SyntaxNode)a.Expression).WhereNotNull());
                }

                return result;
            }

            var baseMethod = node as BaseMethodDeclarationSyntax;
            if (baseMethod != null) {
                if (baseMethod.Body != null)
                    return SpecializedCollections.SingletonEnumerable<SyntaxNode>(baseMethod.Body);
                var method = baseMethod as MethodDeclarationSyntax;
                if ((method != null) && (method.ExpressionBody != null)) {
                    return SpecializedCollections.SingletonEnumerable<SyntaxNode>(method.ExpressionBody);
                }
            }

            var baseProperty = node as BasePropertyDeclarationSyntax;
            if (baseProperty != null) {
                if (baseProperty.AccessorList != null) {
                    return baseProperty.AccessorList.Accessors.Select(a => a.Body).WhereNotNull();
                }
                var indexer = baseProperty as IndexerDeclarationSyntax;
                if ((indexer != null) && (indexer.ExpressionBody != null)) {
                    return SpecializedCollections.SingletonEnumerable<SyntaxNode>(indexer.ExpressionBody);
                }
            }

            var @enum = node as EnumMemberDeclarationSyntax;
            if (@enum != null) {
                if (@enum.EqualsValue != null) {
                    return SpecializedCollections.SingletonEnumerable(@enum.EqualsValue.Value).WhereNotNull();
                }
            }

            var field = node as BaseFieldDeclarationSyntax;
            if (field != null) {
                return field.Declaration.Variables.Where(v => v.Initializer != null).Select(v => v.Initializer.Value).WhereNotNull();
            }

            return SpecializedCollections.EmptyEnumerable<SyntaxNode>();
        }

        public static ConditionalAccessExpressionSyntax GetParentConditionalAccessExpression(this SyntaxNode node)
        {
            var parent = node.Parent;
            while (parent != null) {
                // Because the syntax for conditional access is right associate, we cannot
                // simply take the first ancestor ConditionalAccessExpression. Instead, we
                // must walk upward until we find the ConditionalAccessExpression whose
                // OperatorToken appears left of the MemberBinding.
                if (parent.IsKind(CSSyntaxKind.ConditionalAccessExpression) &&
                    ((ConditionalAccessExpressionSyntax)parent).OperatorToken.Span.End <= node.SpanStart) {
                    return (ConditionalAccessExpressionSyntax)parent;
                }

                parent = parent.Parent;
            }

            return null;
        }

        public static bool IsDelegateOrConstructorOrMethodParameterList(this SyntaxNode node)
        {
            if (!node.IsKind(CSSyntaxKind.ParameterList)) {
                return false;
            }

            return
                node.IsParentKind(CSSyntaxKind.MethodDeclaration) ||
                node.IsParentKind(CSSyntaxKind.ConstructorDeclaration) ||
                node.IsParentKind(CSSyntaxKind.DelegateDeclaration);
        }

        public static SyntaxTree WithAnnotatedNode(this SyntaxNode root, SyntaxNode selectedNode, string annotationKind, string annotationData = "")
        {
            var annotatatedNode =
                selectedNode.WithAdditionalAnnotations(new SyntaxAnnotation(annotationKind, annotationData));
            return root.ReplaceNode(selectedNode, annotatatedNode).SyntaxTree.WithFilePath(root.SyntaxTree.FilePath);
        }

        public static string GetBriefNodeDescription(this SyntaxNode node)
        {
            var sb = new StringBuilder();
            sb.Append($"'{node.ToString().Truncate()}' at character {node.SpanStart}");
            return sb.ToString();
        }

        public static string DescribeConversionError(this SyntaxNode node, Exception e)
        {
            return $"Cannot convert {node.GetType().Name}, {e}{Environment.NewLine}{Environment.NewLine}" +
                $"Input:{Environment.NewLine}{node.ToFullString()}{Environment.NewLine}";
        }

        public static string DescribeConversionWarning(this SyntaxNode node, string addtlInfo)
        {
            return $"{addtlInfo}{Environment.NewLine}" +
                $"{node.NormalizeWhitespace().ToFullString()}{Environment.NewLine}";
        }

        private static string Truncate(this string input, int maxLength = 30, string truncationIndicator = "...")
        {
            input = input.Replace(Environment.NewLine, "\\r\\n").Replace("    ", " ").Replace("\t", " ");
            if (input.Length <= maxLength) return input;
            return input.Substring(0, maxLength - truncationIndicator.Length) + truncationIndicator;
        }

        public static T WithCsTrailingErrorComment<T>(this T dummyDestNode,
            VisualBasicSyntaxNode sourceNode,
            Exception exception) where T : CSharpSyntaxNode
        {
            var errorDirective = SyntaxFactory.ParseTrailingTrivia($"#error Cannot convert {sourceNode.GetType().Name} - see comment for details{Environment.NewLine}");
            var errorDescription = sourceNode.DescribeConversionError(exception);
            var commentedText = "/* " + errorDescription + " */";
            var trailingTrivia = SyntaxFactory.TriviaList(errorDirective.Concat(SyntaxFactory.Comment(commentedText)));

            return dummyDestNode
                .WithTrailingTrivia(trailingTrivia)
                .WithAdditionalAnnotations(new SyntaxAnnotation(AnnotationConstants.ConversionErrorAnnotationKind, exception.ToString()));
        }

        public static T WithCsTrailingWarningComment<T>(this T dummyDestNode, string warning, string addtlInfo,
            CSharpSyntaxNode convertedNode
            ) where T : CSharpSyntaxNode
        {
            var warningDirective = SyntaxFactory.ParseTrailingTrivia($"#warning {warning}{Environment.NewLine}");
            var warningDescription = convertedNode.DescribeConversionWarning(addtlInfo);
            var commentedText = "/* " + warningDescription + " */";
            var trailingTrivia = SyntaxFactory.TriviaList(warningDirective.Concat(SyntaxFactory.Comment(commentedText)));

            return dummyDestNode
                .WithTrailingTrivia(trailingTrivia);
        }

        public static T WithVbTrailingErrorComment<T>(
            this T dummyDestNode, CSharpSyntaxNode problematicSourceNode, Exception exception) where T : VisualBasicSyntaxNode
        {
            var errorDescription = problematicSourceNode.DescribeConversionError(exception);
            var commentedText = "''' " + errorDescription.Replace("\r\n", "\r\n''' ");
            return dummyDestNode
                .WithTrailingTrivia(VBSyntaxFactory.CommentTrivia(commentedText))
                .WithAdditionalAnnotations(new SyntaxAnnotation(AnnotationConstants.ConversionErrorAnnotationKind,
                    exception.ToString()));
        }

        public static bool ContainsDeclaredVisibility(this SyntaxTokenList modifiers, bool isVariableOrConst = false, bool isConstructor = false)
        {
            return modifiers.Any(m => m.IsCsVisibility(isVariableOrConst, isConstructor));
        }

        public static SyntaxToken FindNonZeroWidthToken(this SyntaxNode node, int position)
        {
            var syntaxToken = node.FindToken(position);
            if (syntaxToken.FullWidth() == 0) {
                return syntaxToken.GetPreviousToken();
            } else {
                return syntaxToken;
            }
        }
    }
}