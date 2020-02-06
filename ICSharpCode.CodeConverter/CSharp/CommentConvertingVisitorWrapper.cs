using System;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using CS = Microsoft.CodeAnalysis.CSharp;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class CommentConvertingVisitorWrapper
    {
        private readonly VBasic.VisualBasicSyntaxVisitor<Task<CS.CSharpSyntaxNode>> _wrappedVisitor;

        public CommentConvertingVisitorWrapper(VBasic.VisualBasicSyntaxVisitor<Task<CS.CSharpSyntaxNode>> wrappedVisitor)
        {
            _wrappedVisitor = wrappedVisitor;
        }

        public async Task<T> Accept<T>(SyntaxNode node, bool addSourceMapping) where T : CS.CSharpSyntaxNode
        {
            return await ConvertHandled<T>(node, addSourceMapping);
        }

        public async Task<SeparatedSyntaxList<TOut>> Accept<TIn, TOut>(SeparatedSyntaxList<TIn> nodes, bool addSourceMapping) where TIn : VBasic.VisualBasicSyntaxNode where TOut : CS.CSharpSyntaxNode
        {
            var convertedNodes = await nodes.SelectAsync(n => ConvertHandled<TOut>(n, addSourceMapping));
            var convertedSeparators = nodes.GetSeparators().Select(s => 
                CS.SyntaxFactory.Token(CS.SyntaxKind.CommaToken).WithConvertedTrailingTriviaFrom(s)
            );
            return CS.SyntaxFactory.SeparatedList(convertedNodes, convertedSeparators);
        }

        private async Task<T> ConvertHandled<T>(SyntaxNode node, bool addSourceMapping) where T : CS.CSharpSyntaxNode
        {
            try {
                var converted = (T) await Convert(node);
                return addSourceMapping ? node.CopyAnnotationsTo(converted).WithSourceMappingFrom(node)
                    : converted.WithoutSourceMapping();
            } catch (Exception e) {
                var dummyStatement = (T)(object)CS.SyntaxFactory.EmptyStatement();
                return dummyStatement.WithCsTrailingErrorComment((VBasic.VisualBasicSyntaxNode)node, e);
            }
        }

        /// <remarks>
        /// If lots of special cases, move to wrapping the wrappedVisitor in another visitor, but I'd rather use a simple switch here initially.
        /// </remarks>
        private async Task<CS.CSharpSyntaxNode> Convert(SyntaxNode vbNode)
        {
            var converted = await _wrappedVisitor.Visit(vbNode);
            return converted;
        }
    }
}
