using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class SyntaxNodeVisitorExtensions
    {
        public static Task<T> Accept<T>(this SyntaxNode node, CommentConvertingVisitorWrapper<T> visitorWrapper) where T: SyntaxNode
        {
            return await visitorWrapper.Visit(node);
        }
    }
}