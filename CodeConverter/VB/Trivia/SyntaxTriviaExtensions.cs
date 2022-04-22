namespace ICSharpCode.CodeConverter.VB.Trivia;

internal static class SyntaxTriviaExtensions
{

    public static bool IsCommentOrDirectiveTrivia(this SyntaxTrivia t)
    {
        if (t.IsSingleLineComment())
        {
            return true;
        }
        if (t.IsMultiLineComment())
        {
            return true;
        }
        if (t.IsDirective)
        {
            return true;
        }
        return false;
    }

    public static bool IsMultiLineComment(this SyntaxTrivia trivia)
    {
        return trivia.IsKind(CS.SyntaxKind.MultiLineCommentTrivia) || trivia.IsKind(CS.SyntaxKind.DocumentationCommentExteriorTrivia) || trivia.IsKind(CS.SyntaxKind.MultiLineDocumentationCommentTrivia);
    }

    public static bool IsSingleLineComment(this SyntaxTrivia trivia)
    {
        return trivia.IsKind(CS.SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(CS.SyntaxKind.SingleLineDocumentationCommentTrivia) || trivia.IsKind(VBasic.SyntaxKind.CommentTrivia);
    }
}