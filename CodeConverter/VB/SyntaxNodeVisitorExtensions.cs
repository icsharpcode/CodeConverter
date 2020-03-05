using Microsoft.CodeAnalysis;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;

namespace ICSharpCode.CodeConverter.VB
{

    [System.Diagnostics.DebuggerStepThrough]
    internal static class SyntaxNodeVisitorExtensions
    {
        public static T Accept<T>(this SyntaxNode node, CommentConvertingVisitorWrapper<T> visitorWrapper, bool addSourceMapping = true) where T : VBasic.VisualBasicSyntaxNode
        {
            if (node == null) return default(T);
            return visitorWrapper.Accept(node, addSourceMapping);
        }
    }
}
