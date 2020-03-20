using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using VisualBasicExtensions = Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using ICSharpCode.CodeConverter.Shared;

namespace ICSharpCode.CodeConverter.Util
{
#if NR6
    public
#endif
    internal static class SyntaxTokenExtensions
    {
        public static SyntaxNode GetAncestor(this SyntaxToken token, Func<SyntaxNode, bool> predicate)
        {
            return token.GetAncestor<SyntaxNode>(predicate);
        }

        public static T GetAncestor<T>(this SyntaxToken token, Func<T, bool> predicate = null)
            where T : SyntaxNode
        {
            return token.Parent != null
                ? token.Parent.FirstAncestorOrSelf(predicate)
                    : default(T);
        }

        public static IEnumerable<T> GetAncestors<T>(this SyntaxToken token)
            where T : SyntaxNode
        {
            return token.Parent != null
                ? token.Parent.AncestorsAndSelf().OfType<T>()
                    : Enumerable.Empty<T>();
        }

        public static IEnumerable<SyntaxNode> GetAncestors(this SyntaxToken token, Func<SyntaxNode, bool> predicate)
        {
            return token.Parent != null
                ? token.Parent.AncestorsAndSelf().Where(predicate)
                    : Enumerable.Empty<SyntaxNode>();
        }

        public static int Width(this SyntaxToken token)
        {
            return token.Span.Length;
        }

        public static int FullWidth(this SyntaxToken token)
        {
            return token.FullSpan.Length;
        }

        public static bool IsKindOrHasMatchingText(this SyntaxToken token, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind kind)
        {
            return VisualBasicExtensions.Kind(token) == kind || token.HasMatchingText(kind);
        }

        public static bool HasMatchingText(this SyntaxToken token, SyntaxKind kind)
        {
            return token.ToString() == SyntaxFacts.GetText(kind);
        }

        public static bool HasMatchingText(this SyntaxToken token, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind kind)
        {
            return token.ToString() == VBasic.SyntaxFacts.GetText(kind);
        }

        public static bool IsKind(this SyntaxToken token, SyntaxKind kind1, SyntaxKind kind2)
        {
            return token.Kind() == kind1
                || token.Kind() == kind2;
        }

        public static bool IsKind(this SyntaxToken token, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind kind1, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind kind2)
        {
            return VisualBasicExtensions.Kind(token) == kind1
                || VisualBasicExtensions.Kind(token) == kind2;
        }

        public static bool IsKind(this SyntaxToken token, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3)
        {
            return token.Kind() == kind1
                || token.Kind() == kind2
                || token.Kind() == kind3;
        }

        public static bool IsKind(this SyntaxToken token, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind kind1, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind kind2, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind kind3)
        {
            return VisualBasicExtensions.Kind(token) == kind1
                || VisualBasicExtensions.Kind(token) == kind2
                || VisualBasicExtensions.Kind(token) == kind3;
        }

        public static bool IsKind(this SyntaxToken token, params SyntaxKind[] kinds)
        {
            return kinds.Contains(token.Kind());
        }

        public static bool IsKind(this SyntaxToken token, params Microsoft.CodeAnalysis.VisualBasic.SyntaxKind[] kinds)
        {
            return kinds.Contains(VisualBasicExtensions.Kind(token));
        }

        public static bool IntersectsWith(this SyntaxToken token, int position)
        {
            return token.Span.IntersectsWith(position);
        }

        public static SyntaxToken GetNextCsTokenOrEndOfFile(
            this SyntaxToken token,
            bool includeZeroWidth = false,
            bool includeSkipped = false,
            bool includeDirectives = false,
            bool includeDocumentationComments = false)
        {
            var nextToken = token.GetNextToken(includeZeroWidth, includeSkipped, includeDirectives, includeDocumentationComments);

            return nextToken.Kind() == SyntaxKind.None
                ? token.GetAncestor<CompilationUnitSyntax>().EndOfFileToken
                    : nextToken;
        }

        public static SyntaxToken With(this SyntaxToken token, SyntaxTriviaList leading, SyntaxTriviaList trailing)
        {
            return token.WithLeadingTrivia(leading).WithTrailingTrivia(trailing);
        }

        /// <summary>
        /// Determines whether the given SyntaxToken is the first token on a line in the specified SourceText.
        /// </summary>
        public static bool IsFirstTokenOnLine(this SyntaxToken token, SourceText text)
        {
            var previousToken = token.GetPreviousToken(includeSkipped: true, includeDirectives: true, includeDocumentationComments: true);
            if (previousToken.Kind() == SyntaxKind.None) {
                return true;
            }

            var tokenLine = text.Lines.IndexOf(token.SpanStart);
            var previousTokenLine = text.Lines.IndexOf(previousToken.SpanStart);
            return tokenLine > previousTokenLine;
        }

        public static bool SpansPreprocessorDirective(this IEnumerable<SyntaxToken> tokens)
        {
            // we want to check all leading trivia of all tokens (except the
            // first one), and all trailing trivia of all tokens (except the
            // last one).

            var first = true;
            var previousToken = default(SyntaxToken);

            foreach (var token in tokens) {
                if (first) {
                    first = false;
                } else {
                    // check the leading trivia of this token, and the trailing trivia
                    // of the previous token.
                    if (SpansPreprocessorDirective(token.LeadingTrivia) ||
                        SpansPreprocessorDirective(previousToken.TrailingTrivia)) {
                        return true;
                    }
                }

                previousToken = token;
            }

            return false;
        }

        private static bool SpansPreprocessorDirective(SyntaxTriviaList list)
        {
            return list.Any(t => t.GetStructure() is DirectiveTriviaSyntax);
        }

        public static SyntaxToken WithoutTrivia(
            this SyntaxToken token,
            params SyntaxTrivia[] trivia)
        {
            if (!token.LeadingTrivia.Any() && !token.TrailingTrivia.Any()) {
                return token;
            }

            return token.With(new SyntaxTriviaList(), new SyntaxTriviaList());
        }

        public static SyntaxToken WithPrependedLeadingTrivia(
            this SyntaxToken token,
            params SyntaxTrivia[] trivia)
        {
            if (trivia.Length == 0) {
                return token;
            }

            return token.WithPrependedLeadingTrivia((IEnumerable<SyntaxTrivia>)trivia);
        }

        public static SyntaxToken WithPrependedLeadingTrivia(
            this SyntaxToken token,
            SyntaxTriviaList trivia)
        {
            if (trivia.Count == 0) {
                return token;
            }

            return token.WithLeadingTrivia(trivia.Concat(token.LeadingTrivia));
        }

        public static SyntaxToken WithPrependedLeadingTrivia(
            this SyntaxToken token,
            IEnumerable<SyntaxTrivia> trivia)
        {
            return token.WithPrependedLeadingTrivia(trivia.ToSyntaxTriviaList());
        }

        public static SyntaxToken WithAppendedTrailingTrivia(
            this SyntaxToken token,
            IEnumerable<SyntaxTrivia> trivia)
        {
            return token.WithTrailingTrivia(token.TrailingTrivia.Concat(trivia));
        }

        public static bool IsVbVisibility(this SyntaxToken token, bool isVariableOrConst, bool isConstructor)
        {
            return token.IsKind(VBasic.SyntaxKind.PublicKeyword, VBasic.SyntaxKind.FriendKeyword, VBasic.SyntaxKind.ProtectedKeyword, VBasic.SyntaxKind.PrivateKeyword)
                   || isVariableOrConst && token.IsKind(VBasic.SyntaxKind.ConstKeyword)
                   || isConstructor && token.IsKind(VBasic.SyntaxKind.SharedKeyword);
        }

        public static bool IsCsVisibility(this SyntaxToken token, bool isVariableOrConst, bool isConstructor)
        {
            return token.IsKind(SyntaxKind.PublicKeyword, SyntaxKind.InternalKeyword, SyntaxKind.ProtectedKeyword, SyntaxKind.PrivateKeyword)
                   || isVariableOrConst && token.IsKind(SyntaxKind.ConstKeyword)
                   || isConstructor && token.IsKind(SyntaxKind.StaticKeyword);
        }

        public static SyntaxToken WithSourceMappingFrom(this SyntaxToken converted, SyntaxNodeOrToken fromToken)
        {
            var origLinespan = fromToken.SyntaxTree.GetLineSpan(fromToken.Span);
            if (fromToken.IsToken) converted = fromToken.AsToken().CopyAnnotationsTo(converted);
            return converted.WithSourceStartLineAnnotation(origLinespan).WithSourceEndLineAnnotation(origLinespan);
        }

        public static SyntaxToken WithSourceStartLineAnnotation(this SyntaxToken node, FileLinePositionSpan sourcePosition)
        {
            return node.WithAdditionalAnnotations(AnnotationConstants.SourceStartLine(sourcePosition));
        }

        public static SyntaxToken WithSourceEndLineAnnotation(this SyntaxToken node, FileLinePositionSpan sourcePosition)
        {
            return node.WithAdditionalAnnotations(AnnotationConstants.SourceEndLine(sourcePosition));
        }

        public static SyntaxToken WithoutSourceMapping(this SyntaxToken token)
        {
            return token.WithoutAnnotations(AnnotationConstants.SourceStartLineAnnotationKind).WithoutAnnotations(AnnotationConstants.SourceEndLineAnnotationKind);
        }
    }
}
