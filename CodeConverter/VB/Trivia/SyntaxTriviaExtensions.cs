using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;
using ICSharpCode.CodeConverter.Util;

namespace CSharpToVBCodeConverter.Util
{
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
            return trivia.IsKind(CS.SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(CS.SyntaxKind.SingleLineDocumentationCommentTrivia) || trivia.IsKind(VB.SyntaxKind.CommentTrivia);
        }
    }
}

