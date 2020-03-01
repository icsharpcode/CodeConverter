using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;
using VBFactory = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory;
using VBS = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using ICSharpCode.CodeConverter.Util;

namespace CSharpToVBCodeConverter.Util
{
    internal static class SyntaxTriviaExtensions
    {
        public static bool ContainsEOLTrivia(this SyntaxTriviaList TriviaList)
        {
            foreach (SyntaxTrivia t in TriviaList)
            {
                if (t.IsEndOfLine())
                {
                    return true;
                }
            }
            return false;
        }

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

        public static SyntaxTriviaList ToSyntaxTriviaList(this IEnumerable<SyntaxTrivia> l)
        {
            var NewSyntaxTriviaList = new SyntaxTriviaList();
            return NewSyntaxTriviaList.AddRange(l);
        }
    }
}

