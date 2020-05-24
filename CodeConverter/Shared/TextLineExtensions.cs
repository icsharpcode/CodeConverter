using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ICSharpCode.CodeConverter.Shared
{

    internal static class TextLineExtensions
    {
        public static bool ContainsPosition(this TextLine line, int position)
        {
            return line.Start <= position && position <= line.End;
        }

        public static SyntaxToken FindFirstTokenWithinLine(this TextLine line, SyntaxNode node)
        {
            var syntaxToken = node.FindToken(line.Start);
            var previousToken = syntaxToken.GetPreviousToken();
            var nextToken = syntaxToken.GetNextToken();
            return new[] { previousToken, syntaxToken, nextToken }
                .FirstOrDefault(t => line.ContainsPosition(t.Span.Start));
        }

        public static SyntaxToken FindLastTokenWithinLine(this TextLine line, SyntaxNode node)
        {
            var syntaxToken = node.FindToken(line.End);
            var previousToken = syntaxToken.GetPreviousToken();
            var nextToken = syntaxToken.GetNextToken();
            return new[] { nextToken, syntaxToken, previousToken }
                .FirstOrDefault(t => line.ContainsPosition(t.Span.End) && t.Width() > 0);
        }

        public static SyntaxToken GetLeadingForLine(this TextLine targetLine, SyntaxNode target)
        {
            var toReplace = target.FindNonZeroWidthToken(targetLine.Start);
            if (toReplace.Span.End < targetLine.Start) {
                toReplace = toReplace.GetNextToken();
            }

            return toReplace;
        }

        public static SyntaxToken GetTrailingForLine(this TextLine targetLine, SyntaxNode target)
        {
            var toReplace = target.FindNonZeroWidthToken(targetLine.End);
            if (toReplace.Width() == 0) {
                toReplace = toReplace.GetPreviousToken(); //Never append *trailing* trivia to the end of file token
            }

            return toReplace;
        }
    }
}