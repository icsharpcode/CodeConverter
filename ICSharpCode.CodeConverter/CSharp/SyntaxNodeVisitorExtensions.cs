using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class SyntaxNodeVisitorExtensions
    {
        public static async Task<T> Accept<T>(this SyntaxNode node, CommentConvertingVisitorWrapper<T> visitorWrapper) where T: SyntaxNode
        {
            if (node == null) return default(T);
            return await visitorWrapper.Visit(node);
        }
    }
}