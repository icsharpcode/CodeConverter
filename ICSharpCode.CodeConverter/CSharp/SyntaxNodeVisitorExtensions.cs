using System;
using System.Collections.Generic;
using System.Linq;
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

        public static async Task<T[]> Accept<T>(this IEnumerable<SyntaxNode> nodes, CommentConvertingVisitorWrapper<T> visitorWrapper) where T : SyntaxNode
        {
            if (nodes == null) return default;
            return await SelectAsync(nodes,  n => n.Accept(visitorWrapper));
        }

        public static async Task<TResult[]> SelectAsync<TArg, TResult>(this IEnumerable<TArg> nodes, Func<TArg, Task<TResult>> selector)
        {
            return await Task.WhenAll(nodes.Select(selector));
        }

        public static async Task<TResult[]> SelectManyAsync<TArg, TResult>(this IEnumerable<TArg> nodes, Func<TArg, Task<IEnumerable<TResult>>> selector)
        {
            var selectAsync = await nodes.SelectAsync(selector);
            return selectAsync.SelectMany(x => x).ToArray();
        }

        public static async Task<TSpecific[]> Accept<TGeneral, TSpecific>(this IEnumerable<SyntaxNode> nodes, CommentConvertingVisitorWrapper<TGeneral> visitorWrapper) where TGeneral : SyntaxNode where TSpecific : TGeneral
        {
            if (nodes == null) return default;
            return await Task.WhenAll(nodes.Select(async n => (TSpecific) await n.Accept(visitorWrapper)));
        }
    }
}