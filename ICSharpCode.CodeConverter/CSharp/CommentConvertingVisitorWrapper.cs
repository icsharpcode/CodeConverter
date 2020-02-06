using System;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class CommentConvertingVisitorWrapper<T> where T : CSharpSyntaxNode
    {
        private readonly VBasic.VisualBasicSyntaxVisitor<Task<T>> _wrappedVisitor;

        public CommentConvertingVisitorWrapper(VBasic.VisualBasicSyntaxVisitor<Task<T>> wrappedVisitor)
        {
            _wrappedVisitor = wrappedVisitor;
        }

        public async Task<T> Accept(SyntaxNode node, bool addSourceMapping)
        {
            try {
                var converted = await DefaultVisit(node);
                return addSourceMapping ? node.CopyAnnotationsTo(converted).WithSourceMappingFrom(node)
                    : converted.WithoutSourceMapping();
            } catch (Exception e) {
                var dummyStatement = (T)(object)SyntaxFactory.EmptyStatement();
                return dummyStatement.WithCsTrailingErrorComment((VBasic.VisualBasicSyntaxNode)node, e);
            }
        }

        /// <remarks>
        /// If lots of special cases, move to wrapping the wrappedVisitor in another visitor, but I'd rather use a simple switch here initially.
        /// </remarks>
        private async Task<T> DefaultVisit(SyntaxNode vbNode)
        {
            var converted = await _wrappedVisitor.Visit(vbNode);
            return converted;
        }
    }
}
