using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CSharp = Microsoft.CodeAnalysis.CSharp;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;

namespace ICSharpCode.CodeConverter.Util
{
#if NR6
    public
#endif
    static class SyntaxTriviaExtensions
    {
        /// <summary>
        /// Look inside a trivia list for a skipped token that contains the given position.
        /// </summary>
        private static readonly Func<SyntaxTriviaList, int, SyntaxToken> s_findSkippedTokenBackward =
            (l, p) => FindTokenHelper.FindSkippedTokenBackward(GetSkippedTokens(l), p);

        /// <summary>
        /// Look inside a trivia list for a skipped token that contains the given position.
        /// </summary>
        public static readonly Func<SyntaxTriviaList, int, SyntaxToken> s_findSkippedTokenForward =
            (l, p) => FindTokenHelper.FindSkippedTokenForward(GetSkippedTokens(l), p);

        /// <summary>
        /// return only skipped tokens
        /// </summary>
        private static IEnumerable<SyntaxToken> GetSkippedTokens(SyntaxTriviaList list)
        {
            return list.Where(trivia => trivia.RawKind == (int)SyntaxKind.SkippedTokensTrivia)
                .SelectMany(t => ((SkippedTokensTriviaSyntax)t.GetStructure()).Tokens);
        }

        private static readonly Dictionary<VBasic.SyntaxKind, SyntaxKind> VBToCSSyntaxKinds = new Dictionary<VBasic.SyntaxKind, SyntaxKind> {
            {VBasic.SyntaxKind.SkippedTokensTrivia, SyntaxKind.SkippedTokensTrivia},
            {VBasic.SyntaxKind.DisabledTextTrivia, SyntaxKind.DisabledTextTrivia},
            {VBasic.SyntaxKind.ConstDirectiveTrivia, SyntaxKind.DefineDirectiveTrivia}, // Just a guess
            {VBasic.SyntaxKind.IfDirectiveTrivia, SyntaxKind.IfDirectiveTrivia},
            {VBasic.SyntaxKind.ElseIfDirectiveTrivia, SyntaxKind.ElifDirectiveTrivia},
            {VBasic.SyntaxKind.ElseDirectiveTrivia, SyntaxKind.ElseDirectiveTrivia},
            {VBasic.SyntaxKind.EndIfDirectiveTrivia, SyntaxKind.EndIfDirectiveTrivia},
            //{VBSyntaxKind.RegionDirectiveTrivia, SyntaxKind.RegionDirectiveTrivia}, Oh no I accidentally disabled regions :O ;)
            //{VBSyntaxKind.EndRegionDirectiveTrivia, SyntaxKind.EndRegionDirectiveTrivia},
            {VBasic.SyntaxKind.EnableWarningDirectiveTrivia, SyntaxKind.WarningDirectiveTrivia},
            {VBasic.SyntaxKind.DisableWarningDirectiveTrivia, SyntaxKind.WarningDirectiveTrivia},
            {VBasic.SyntaxKind.ReferenceDirectiveTrivia, SyntaxKind.ReferenceDirectiveTrivia},
            {VBasic.SyntaxKind.BadDirectiveTrivia, SyntaxKind.BadDirectiveTrivia},
            {VBasic.SyntaxKind.ConflictMarkerTrivia, SyntaxKind.ConflictMarkerTrivia},
            {VBasic.SyntaxKind.ExternalSourceDirectiveTrivia, SyntaxKind.LoadDirectiveTrivia}, //Just a guess
            {VBasic.SyntaxKind.ExternalChecksumDirectiveTrivia, SyntaxKind.LineDirectiveTrivia}, // Even more random guess
        };

        private static readonly Dictionary<SyntaxKind, VBasic.SyntaxKind> CSToVBSyntaxKinds =
            VBToCSSyntaxKinds
                .ToLookup(kvp => kvp.Value, kvp => kvp.Key)
                .ToDictionary(g => g.Key, g => g.First());

        public static SyntaxKind? GetCSKind(this SyntaxTrivia t)
        {
            return VBToCSSyntaxKinds.TryGetValue(VBasic.VisualBasicExtensions.Kind(t), out var csKind) ? csKind : (SyntaxKind?)null;
        }
        public static VBasic.SyntaxKind? GetVBKind(this SyntaxTrivia t)
        {
            return CSToVBSyntaxKinds.TryGetValue(t.Kind(), out var vbKind) ? vbKind : (VBasic.SyntaxKind?) null;
        }

        public static int Width(this SyntaxTrivia trivia)
        {
            return trivia.Span.Length;
        }

        public static int FullWidth(this SyntaxTrivia trivia)
        {
            return trivia.FullSpan.Length;
        }

        public static bool IsElastic(this SyntaxTrivia trivia)
        {
            return trivia.HasAnnotation(SyntaxAnnotation.ElasticAnnotation);
        }

        public static bool MatchesKind(this SyntaxTrivia trivia, SyntaxKind kind)
        {
            return trivia.Kind() == kind;
        }

        public static bool MatchesKind(this SyntaxTrivia trivia, SyntaxKind kind1, SyntaxKind kind2)
        {
            var triviaKind = trivia.Kind();
            return triviaKind == kind1 || triviaKind == kind2;
        }

        public static bool MatchesKind(this SyntaxTrivia trivia, params SyntaxKind[] kinds)
        {
            return kinds.Contains(trivia.Kind());
        }

        public static bool IsRegularComment(this SyntaxTrivia trivia)
        {
            return trivia.IsSingleLineComment() || trivia.IsMultiLineComment();
        }

        public static bool IsRegularOrDocComment(this SyntaxTrivia trivia)
        {
            return trivia.IsSingleLineComment() || trivia.IsMultiLineComment() || trivia.IsDocComment();
        }

        public static bool IsSingleLineComment(this SyntaxTrivia trivia)
        {
            return trivia.Kind() == SyntaxKind.SingleLineCommentTrivia;
        }

        public static bool IsMultiLineComment(this SyntaxTrivia trivia)
        {
            return trivia.Kind() == SyntaxKind.MultiLineCommentTrivia;
        }

        public static bool IsCompleteMultiLineComment(this SyntaxTrivia trivia)
        {
            if (trivia.Kind() != SyntaxKind.MultiLineCommentTrivia) {
                return false;
            }

            var text = trivia.ToFullString();
            return text.Length >= 4
                && text[text.Length - 1] == '/'
                && text[text.Length - 2] == '*';
        }

        public static bool IsDocComment(this SyntaxTrivia trivia)
        {
            return trivia.IsSingleLineDocComment() || trivia.IsMultiLineDocComment();
        }

        public static bool IsSingleLineDocComment(this SyntaxTrivia trivia)
        {
            return trivia.Kind() == SyntaxKind.SingleLineDocumentationCommentTrivia;
        }

        public static bool IsMultiLineDocComment(this SyntaxTrivia trivia)
        {
            return trivia.Kind() == SyntaxKind.MultiLineDocumentationCommentTrivia;
        }

        /// <remarks>Good candidate for unit testing to catch newline issues hidden by the test harness</remarks>
        public static string GetCommentText(this SyntaxTrivia trivia)
        {
            var commentText = trivia.ToString();
            if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)) {
                if (commentText.StartsWith("//")) {
                    commentText = commentText.Substring(2);
                }

                return commentText.TrimStart(null);
            } else if (trivia.IsKind(VBasic.SyntaxKind.CommentTrivia)) {
                if (commentText.StartsWith("'")) {
                    commentText = commentText.Substring(1);
                }

                return commentText.TrimStart(null);
            } else if (trivia.Kind() == SyntaxKind.MultiLineCommentTrivia) {
                var textBuilder = new StringBuilder();

                if (commentText.EndsWith("*/")) {
                    commentText = commentText.Substring(0, commentText.Length - 2);
                }

                if (commentText.StartsWith("/*")) {
                    commentText = commentText.Substring(2);
                }

                commentText = commentText.Trim();

                var newLine = Environment.NewLine;
                var lines = commentText.Split(new[] { newLine }, StringSplitOptions.None);
                foreach (var line in lines) {
                    var trimmedLine = line.Trim();

                    // Note: we trim leading '*' characters in multi-line comments.
                    // If the '*' was intentional, sorry, it's gone.
                    if (trimmedLine.StartsWith("*")) {
                        trimmedLine = trimmedLine.TrimStart('*');
                        trimmedLine = trimmedLine.TrimStart(null);
                    }

                    textBuilder.AppendLine(trimmedLine);
                }

                // remove last line break
                textBuilder.Remove(textBuilder.Length - newLine.Length, newLine.Length);

                return textBuilder.ToString();
            } else if (trivia.IsKind(VBasic.SyntaxKind.DocumentationCommentTrivia)) {
                var textBuilder = new StringBuilder();

                if (commentText.EndsWith("*/")) {
                    commentText = commentText.TrimEnd('\'');
                }

                if (commentText.StartsWith("'''")) {
                    commentText = commentText.TrimStart('\'');
                }

                commentText = commentText.Trim();

                var newLine = commentText.Contains('\n') ? '\n' : '\r';
                var lines = commentText.Split(new []{newLine}, StringSplitOptions.None);
                foreach (var line in lines) {
                    var trimmedLine = line.Trim();

                    // Note: we trim leading ' characters in multi-line comments.
                    // If the ' was intentional, sorry, it's gone.
                    if (trimmedLine.StartsWith("'")) {
                        trimmedLine = trimmedLine.TrimStart('\'');
                        trimmedLine = trimmedLine.TrimStart(null);
                    }

                    textBuilder.AppendLine(trimmedLine);
                }

                return textBuilder.ToString().TrimEnd();
            } 

            throw new NotImplementedException($"Comment cannot be parsed:\r\n'{commentText}'");
        }

        public static string AsString(this IEnumerable<SyntaxTrivia> trivia)
        {
            //Contract.ThrowIfNull(trivia);

            if (trivia.Any()) {
                var sb = new StringBuilder();
                trivia.Select(t => t.ToFullString()).Do((s) => sb.Append(s));
                return sb.ToString();
            } else {
                return String.Empty;
            }
        }

        public static int GetFullWidth(this IEnumerable<SyntaxTrivia> trivia)
        {
            //Contract.ThrowIfNull(trivia);
            return trivia.Sum(t => t.FullWidth());
        }

        public static SyntaxTriviaList AsTrivia(this string s)
        {
            return SyntaxFactory.ParseLeadingTrivia(s ?? String.Empty);
        }

        public static bool IsWhitespaceOrEndOfLine(this SyntaxTrivia trivia)
        {
            return trivia.IsEndOfLine() || trivia.IsWhitespace();
        }

        public static bool IsEndOfLine(this SyntaxTrivia x)
        {
            return x.IsKind(VBasic.SyntaxKind.EndOfLineTrivia) || x.IsKind(SyntaxKind.EndOfLineTrivia);
        }

        private static bool IsWhitespace(this SyntaxTrivia x)
        {
            return x.IsKind(VBasic.SyntaxKind.WhitespaceTrivia) || x.IsKind(SyntaxKind.WhitespaceTrivia);
        }

        public static SyntaxTrivia GetPreviousTrivia(
            this SyntaxTrivia trivia, SyntaxTree syntaxTree, CancellationToken cancellationToken, bool findInsideTrivia = false)
        {
            var span = trivia.FullSpan;
            if (span.Start == 0) {
                return default(SyntaxTrivia);
            }

            return syntaxTree.GetRoot(cancellationToken).FindTrivia(span.Start - 1, findInsideTrivia);
        }

#if false
        public static int Width(this SyntaxTrivia trivia)
        {
        return trivia.Span.Length;
        }

        public static int FullWidth(this SyntaxTrivia trivia)
        {
        return trivia.FullSpan.Length;
        }
#endif
    }
}
