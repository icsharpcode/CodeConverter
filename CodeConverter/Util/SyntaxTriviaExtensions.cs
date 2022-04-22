using System.Text;
using System.Text.RegularExpressions;

namespace ICSharpCode.CodeConverter.Util;

internal static class SyntaxTriviaExtensions
{
    private static readonly Dictionary<VBasic.SyntaxKind, CS.SyntaxKind> VBToCSSyntaxKinds = new() {
        {VBasic.SyntaxKind.SkippedTokensTrivia, CS.SyntaxKind.SkippedTokensTrivia},
        {VBasic.SyntaxKind.DisabledTextTrivia, CS.SyntaxKind.DisabledTextTrivia},
        {VBasic.SyntaxKind.ConstDirectiveTrivia, CS.SyntaxKind.DefineDirectiveTrivia}, // Just a guess
        {VBasic.SyntaxKind.IfDirectiveTrivia, CS.SyntaxKind.IfDirectiveTrivia},
        {VBasic.SyntaxKind.ElseIfDirectiveTrivia, CS.SyntaxKind.ElifDirectiveTrivia},
        {VBasic.SyntaxKind.ElseDirectiveTrivia, CS.SyntaxKind.ElseDirectiveTrivia},
        {VBasic.SyntaxKind.EndIfDirectiveTrivia, CS.SyntaxKind.EndIfDirectiveTrivia},
        {VBasic.SyntaxKind.RegionDirectiveTrivia, CS.SyntaxKind.RegionDirectiveTrivia},
        {VBasic.SyntaxKind.EndRegionDirectiveTrivia, CS.SyntaxKind.EndRegionDirectiveTrivia},
        {VBasic.SyntaxKind.EnableWarningDirectiveTrivia, CS.SyntaxKind.WarningDirectiveTrivia},
        {VBasic.SyntaxKind.DisableWarningDirectiveTrivia, CS.SyntaxKind.WarningDirectiveTrivia},
        {VBasic.SyntaxKind.ReferenceDirectiveTrivia, CS.SyntaxKind.ReferenceDirectiveTrivia},
        {VBasic.SyntaxKind.BadDirectiveTrivia, CS.SyntaxKind.BadDirectiveTrivia},
        {VBasic.SyntaxKind.ConflictMarkerTrivia, CS.SyntaxKind.ConflictMarkerTrivia},
        {VBasic.SyntaxKind.ExternalSourceDirectiveTrivia, CS.SyntaxKind.LoadDirectiveTrivia}, //Just a guess
        {VBasic.SyntaxKind.ExternalChecksumDirectiveTrivia, CS.SyntaxKind.LineDirectiveTrivia}, // Even more random guess
    };

    public static CS.SyntaxKind? GetCSKind(this SyntaxTrivia t)
    {
        return VBToCSSyntaxKinds.TryGetValue(VBasic.VisualBasicExtensions.Kind(t), out var csKind) ? csKind : null;
    }

    /// <remarks>Good candidate for unit testing to catch newline issues hidden by the test harness</remarks>
    public static string GetCommentText(this SyntaxTrivia trivia)
    {
        var commentText = trivia.ToString();
        if (trivia.IsKind(CS.SyntaxKind.SingleLineCommentTrivia)) {
            if (commentText.StartsWith("//", StringComparison.InvariantCulture)) {
                commentText = commentText.Substring(2);
            }

            return commentText.TrimStart(null);
        } else if (trivia.IsKind(VBasic.SyntaxKind.CommentTrivia)) {
            if (commentText.StartsWith("'", StringComparison.InvariantCulture) || commentText.StartsWith("’", StringComparison.InvariantCulture)) {
                commentText = commentText.Substring(1);
            }

            return commentText.TrimStart(null);
        } else if (CS.CSharpExtensions.Kind(trivia) == CS.SyntaxKind.MultiLineCommentTrivia) {
            var textBuilder = new StringBuilder();

            if (commentText.EndsWith("*/", StringComparison.InvariantCulture)) {
                commentText = commentText.Substring(0, commentText.Length - 2);
            }

            if (commentText.StartsWith("/*", StringComparison.InvariantCulture)) {
                commentText = commentText.Substring(2);
            }


            var newLine = Environment.NewLine;
            var lines = commentText.Split(new[] { newLine }, StringSplitOptions.None);
            foreach (var line in lines) {

                textBuilder.AppendLine(line);
            }

            // remove trailing line breaks
            return textBuilder.ToString().TrimEnd();
        } else if (trivia.IsKind(VBasic.SyntaxKind.DocumentationCommentTrivia) || CS.CSharpExtensions.Kind(trivia) == CS.SyntaxKind.SingleLineDocumentationCommentTrivia) {
            var textBuilder = new StringBuilder();

            var lines = commentText.Replace("\r\n", "\n").Split('\n');
            foreach (var line in lines) {
                // Single line comment marker is part of the comment text
                string trimmed = Regex.Replace(line, @"^(\ *''')?\ ?", "");
                // Generated doc comments are indented by one space in VB, this is not idiomatic in C#
                // If someone has intentionally indented by a single space at the start for ascii art or something, this will unfortunately be lost.
                trimmed = Regex.Replace(trimmed, @"^\ ([^ ])", "$1");
                textBuilder.AppendLine(trimmed);
            }

            // remove trailing line breaks
            return textBuilder.ToString().TrimEnd();
        }

        throw new NotImplementedException($"Comment cannot be parsed:\r\n'{commentText}'");
    }

    public static bool IsWhitespaceOrEndOfLine(this SyntaxTrivia trivia)
    {
        return trivia.IsEndOfLine() || trivia.IsWhitespace();
    }

    public static bool IsEndOfLine(this SyntaxTrivia x)
    {
        return x.IsKind(VBasic.SyntaxKind.EndOfLineTrivia) || x.IsKind(CS.SyntaxKind.EndOfLineTrivia);
    }

    public static bool IsWhitespace(this SyntaxTrivia x)
    {
        return x.IsKind(VBasic.SyntaxKind.WhitespaceTrivia) || x.IsKind(CS.SyntaxKind.WhitespaceTrivia);
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
}