using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.Shared
{
    public class TriviaConverter
    {
        private static readonly string TrailingTriviaConversionKind = $"{nameof(TriviaConverter)}.TrailingTriviaConversion.Id";

        /// <summary>
        /// The source of truth for a source node's conversion id. This dictates that the source token's trailing trivia will be converted and placed on the node with that conversion id.
        /// </summary>
        private readonly Dictionary<SyntaxToken, string> _trailingTriviaConversionsBySource = new Dictionary<SyntaxToken, string>();

        /// <summary>
        /// Because annotation data can only be a string, use a dictionary to store the information actually desired.
        /// Note, this is NOT just the inverse of <see cref="_trailingTriviaConversionsBySource"/>.
        /// Crucially, the source token contained here is the one originally intended, and may now be obsolete.
        /// </summary>
        private readonly Dictionary<string, SyntaxToken> _annotationData = new Dictionary<string, SyntaxToken>();

        public T PortConvertedTrivia<T>(SyntaxNode sourceNode, T destination) where T : SyntaxNode
        {
            if (destination == null || sourceNode == null) return destination;

            destination = WithAnnotations(sourceNode, destination);

            if (sourceNode is CSharpSyntaxNode) return destination;

            destination = sourceNode.HasLeadingTrivia && sourceNode.GetFirstToken() != sourceNode?.Parent?.GetFirstToken()
                ? destination.WithLeadingTrivia(sourceNode.GetLeadingTrivia().ConvertTrivia())
                : destination;

            if (sourceNode.HasTrailingTrivia) {
                var lastDestToken = destination.GetLastToken();
                destination = destination.ReplaceToken(lastDestToken, WithDelegateToParentAnnotation(sourceNode, lastDestToken));
            }

            var firstLineOfBlockConstruct = sourceNode.ChildNodes().OfType<VBSyntax.StatementSyntax>().FirstOrDefault(IsFirstLineOfBlockConstruct);
            if (firstLineOfBlockConstruct != null) {
                var endOfFirstLineConstructOrDefault = destination.ChildTokens().FirstOrDefault(t => t.IsKind(SyntaxKind.CloseParenToken, SyntaxKind.OpenBraceToken));
                if (endOfFirstLineConstructOrDefault.IsKind(SyntaxKind.OpenBraceToken)) {
                    endOfFirstLineConstructOrDefault = endOfFirstLineConstructOrDefault.GetPreviousToken();
                }
                if (endOfFirstLineConstructOrDefault != default(SyntaxToken)) {
                    var withNewAnnotations = MoveChildTrailingEndOfLinesToToken(destination, endOfFirstLineConstructOrDefault);
                    destination = destination.ReplaceToken(endOfFirstLineConstructOrDefault, withNewAnnotations);
                }
            }

            var hasVisitedContainingBlock = HasVisitedContainingBlock(sourceNode);
            return WithTrailingTriviaConversions(destination, sourceNode.Parent?.GetLastToken(), hasVisitedContainingBlock);
        }

        private static bool HasVisitedContainingBlock(SyntaxNode sourceNode)
        {
            var containingSubModuleBlock =
                (SyntaxNode)sourceNode.FirstAncestorOrSelf<VBSyntax.StatementSyntax>(IsFirstLineOfBlockConstruct)
                ?? sourceNode.FirstAncestorOrSelf<CSSyntax.StatementSyntax>();

            return containingSubModuleBlock == null;
        }

        private static T WithAnnotations<T>(SyntaxNode sourceNode, T destination) where T : SyntaxNode
        {
            destination = sourceNode.CopyAnnotationsTo(destination);

            var sourceChildAnnotations = new HashSet<SyntaxAnnotation>(sourceNode.ChildNodes()
                .SelectMany(n => n.GetAnnotations(AnnotationConstants.SelectedNodeAnnotationKind)));
            foreach (var annotation in destination.ChildNodes().SelectMany(n => n.GetAnnotations(AnnotationConstants.SelectedNodeAnnotationKind))) {
                sourceChildAnnotations.Remove(annotation);
            }
            return destination.WithAdditionalAnnotations(sourceChildAnnotations);
        }

        private SyntaxToken MoveChildTrailingEndOfLinesToToken<T>(T destination, SyntaxToken beforeOpenBraceToken)
            where T : SyntaxNode
        {
            var conversionAnnotations = destination.GetAnnotatedTokens(TrailingTriviaConversionKind)
                .TakeWhile(t => t.FullSpan.Start < beforeOpenBraceToken.FullSpan.Start)
                .SelectMany(t => t.GetAnnotations(TrailingTriviaConversionKind).ToList())
                .ToList();
            foreach (var conversionAnnotation in conversionAnnotations) {
                var conversionId = conversionAnnotation.Data;
                var sourceSyntaxToken = _annotationData[conversionId];

                if (_trailingTriviaConversionsBySource.TryGetValue(sourceSyntaxToken, out var latestReplacementId) &&
                    latestReplacementId == conversionId
                    && sourceSyntaxToken.TrailingTrivia.Any(t => t.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.EndOfLineTrivia))) {
                    beforeOpenBraceToken = WithDelegateToParentAnnotation(sourceSyntaxToken, beforeOpenBraceToken);
                }
            }
            return beforeOpenBraceToken;
        }

        /// <summary>
        /// Trivia is attached to tokens, only port it when we're at the highest level for which it's the trailing trivia in the source
        /// </summary>
        /// <remarks>
        /// Because of differences in structure between C# and VB:
        ///  1) Trivia will be ported to the wrong place in a line, e.g. before the semicolon
        ///  2)  Not every node will be visited, and hence Trivia would sometimes be missed
        /// For (1), trailing trivia (often containing newlines) this is particularly problematic, so  we only do a replacement when the
        /// trailing trivia isn't also its parent's trailing trivia.
        /// For (2) the ability to schedule replacements here allows the trivia porting to remain separate from the main transformation
        /// </remarks>
        private T WithTrailingTriviaConversions<T>(T destination, SyntaxToken? parentLastToken, bool hasVisitedContainingBlock) where T : SyntaxNode
        {
            var destinationsWithConversions = destination.GetAnnotatedTokens(TrailingTriviaConversionKind);
            destination = destination.ReplaceTokens(destinationsWithConversions, (originalToken, updatedToken) =>
            {
                foreach (var conversionAnnotation in updatedToken.GetAnnotations(TrailingTriviaConversionKind).ToList()) {
                    var conversionId = conversionAnnotation.Data;
                    var foundAnnotation = _annotationData.TryGetValue(conversionId, out var sourceSyntaxToken);
                    if (foundAnnotation && parentLastToken == sourceSyntaxToken
                    || !hasVisitedContainingBlock) {
                        continue;
                    };

                    // Only port trivia if this replacement hasn't been superseded by another 
                    if (foundAnnotation && // BUG: Fix sometimes not finding annotation
                        _trailingTriviaConversionsBySource.TryGetValue(sourceSyntaxToken, out var latestReplacementId) &&
                        latestReplacementId == conversionId) {
                        updatedToken = updatedToken.WithConvertedTrailingTriviaFrom(sourceSyntaxToken);
                        _trailingTriviaConversionsBySource.Remove(sourceSyntaxToken);
                    }

                    // Remove annotations since it's either done, or obsolete. So we don't have to keep iterating over it for no reason.
                    updatedToken = updatedToken.WithoutAnnotations(conversionAnnotation);
                    _annotationData.Remove(conversionId);
                }
                return updatedToken;
            });
            return destination;
        }

        private static bool IsFirstLineOfBlockConstruct(SyntaxNode s)
        {
            return !(s is VBSyntax.DeclarationStatementSyntax);
        }

        /// <summary>
        /// Because <paramref name="destination"/> is immutable, any changes (such as gaining a parent) a new version to be created.
        /// Adding an annotation allows tracking this node, since it will stay with it in any reincarnations.
        /// </summary>
        public SyntaxToken WithDelegateToParentAnnotation(SyntaxToken lastSourceToken, SyntaxToken destination)
        {
            var identifier = lastSourceToken.GetHashCode() + "|" + destination.GetHashCode();
            _trailingTriviaConversionsBySource[lastSourceToken] = identifier;

            destination = destination.WithAdditionalAnnotations(new SyntaxAnnotation(TrailingTriviaConversionKind, identifier));
            _annotationData.Add(identifier, lastSourceToken);
            return destination;
        }

        public SyntaxToken WithDelegateToParentAnnotation(SyntaxNode unvisitedSourceStatement, SyntaxToken destinationToken)
        {
            return unvisitedSourceStatement == null ? destinationToken
                : WithDelegateToParentAnnotation(unvisitedSourceStatement.GetLastToken(), destinationToken);
        }

        public SyntaxToken WithDelegateToParentAnnotation<T>(SyntaxList<T> unvisitedSourceStatementList, SyntaxToken destinationToken) where T: SyntaxNode
        {
            return WithDelegateToParentAnnotation(unvisitedSourceStatementList.LastOrDefault(), destinationToken);
        }

        public bool IsAllTriviaConverted()
        {
            return _trailingTriviaConversionsBySource.Any(t => t.Key.TrailingTrivia.Any(x => !x.IsWhitespaceOrEndOfLine()));
        }
    }
}