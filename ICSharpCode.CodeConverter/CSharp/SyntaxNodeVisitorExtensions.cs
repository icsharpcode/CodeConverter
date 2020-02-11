using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using CS = Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class SyntaxNodeVisitorExtensions
    {
        public static Task<CSharpSyntaxNode> AcceptAsync(this SyntaxNode node, CommentConvertingVisitorWrapper visitorWrapper, SourceTriviaMapKind sourceTriviaMap = SourceTriviaMapKind.All)
        {
            return AcceptAsync<CSharpSyntaxNode>(node, visitorWrapper, sourceTriviaMap);
        }

        public static async Task<TOut> AcceptAsync<TOut>(this SyntaxNode node, CommentConvertingVisitorWrapper visitorWrapper, SourceTriviaMapKind sourceTriviaMap = SourceTriviaMapKind.All) where TOut : CSharpSyntaxNode
        {
            if (node == null) return null;
            return await visitorWrapper.Accept<TOut>(node, sourceTriviaMap);
        }

        public static async Task<SeparatedSyntaxList<TOut>> AcceptSeparatedListAsync<TIn, TOut>(this SeparatedSyntaxList<TIn> nodes, CommentConvertingVisitorWrapper visitorWrapper, SourceTriviaMapKind sourceTriviaMap = SourceTriviaMapKind.All) where TIn: VisualBasicSyntaxNode where TOut : CSharpSyntaxNode
        {
            return await visitorWrapper.Accept<TIn, TOut>(nodes, sourceTriviaMap);
        }
    }
}
