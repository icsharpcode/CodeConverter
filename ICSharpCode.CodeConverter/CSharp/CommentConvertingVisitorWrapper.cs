using System;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class CommentConvertingVisitorWrapper<T> where T : CSharpSyntaxNode
    {
        private readonly VisualBasicSyntaxVisitor<Task<T>> _wrappedVisitor;

        public CommentConvertingVisitorWrapper(VisualBasicSyntaxVisitor<Task<T>> wrappedVisitor)
        {
            _wrappedVisitor = wrappedVisitor;
        }

        public async Task<T> Accept(SyntaxNode node, bool addSourceMapping)
        {
            try {
                var converted = await _wrappedVisitor.Visit(node);
                return addSourceMapping ? node.CopyAnnotationsTo(converted).WithSourceMappingFrom(node)
                    : converted.WithoutSourceMapping();
            } catch (Exception e) {
                var dummyStatement = (T)(object)Microsoft.CodeAnalysis.CSharp.SyntaxFactory.EmptyStatement();
                return dummyStatement.WithCsTrailingErrorComment((VisualBasicSyntaxNode)node, e);
            }
        }
    }
}
