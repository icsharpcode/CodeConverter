using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class SyntaxNodeVisitorExtensions
    {
        public static T Accept<T>(this SyntaxNode node, CommentConvertingVisitorWrapper<T> visitorWrapper) where T: SyntaxNode
        {
            return visitorWrapper.Visit(node);
        }
    }
}