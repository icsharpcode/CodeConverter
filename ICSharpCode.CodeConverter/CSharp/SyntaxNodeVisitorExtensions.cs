using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class SyntaxNodeVisitorExtensions
    {
        public static async Task<T> AcceptAsync<T>(this SyntaxNode node, CommentConvertingVisitorWrapper<T> visitorWrapper) where T: SyntaxNode
        {
            if (node == null) return default(T);
            return await visitorWrapper.Visit(node);
        }
        public static async Task<CSharpSyntaxNode> AcceptAsync(this SyntaxNode node, CommentConvertingNodesVisitor visitorWrapper)
        {
            if (node == null) return null;
            return await visitorWrapper.Visit(node);
        }

        public static async Task<T[]> AcceptAsync<T>(this IEnumerable<SyntaxNode> nodes, CommentConvertingVisitorWrapper<T> visitorWrapper) where T : SyntaxNode
        {
            if (nodes == null) return default;
            return await nodes.SelectAsync(n => n.AcceptAsync(visitorWrapper));
        }

        public static async Task<CSharpSyntaxNode[]> AcceptAsync(this IEnumerable<SyntaxNode> nodes, CommentConvertingNodesVisitor visitorWrapper)
        {
            return await nodes.SelectAsync(n => n.AcceptAsync(visitorWrapper));
        }

        public static async Task<TSpecific[]> AcceptAsync<TGeneral, TSpecific>(this IEnumerable<SyntaxNode> nodes, CommentConvertingVisitorWrapper<TGeneral> visitorWrapper) where TGeneral : SyntaxNode where TSpecific : TGeneral
        {
            if (nodes == null) return default;
            return await nodes.SelectAsync(async n => (TSpecific) await n.AcceptAsync(visitorWrapper));
        }
    }
}