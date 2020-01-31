using System;
using System.Linq;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using VbSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using CsSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory;
using System.Collections;

namespace ICSharpCode.CodeConverter.VB
{

    internal class CommentConvertingVisitorWrapper<T> where T : VisualBasicSyntaxNode
    {
        // Not thread safe
        private bool _addSourceMapping = true;
        private readonly CSharpSyntaxVisitor<T> _wrappedVisitor;
        public CommentConvertingVisitorWrapper(CSharpSyntaxVisitor<T> wrappedVisitor)
        {
            _wrappedVisitor = wrappedVisitor;
        }

        public TriviaConverter TriviaConverter { get; }

        public T Accept(SyntaxNode node, bool forceNoSourceMapping)
        {
            bool toggleSourceMapping = forceNoSourceMapping && _addSourceMapping;
            if (toggleSourceMapping) _addSourceMapping = false;
            try {
                var converted = _wrappedVisitor.Visit(node);
                return _addSourceMapping ? converted.WithSourceMappingFrom(node) : converted;
            } catch (Exception e) {
                var dummyStatement = SyntaxFactory.EmptyStatement();
                return ((T)(object)dummyStatement).WithVbTrailingErrorComment((CSharpSyntaxNode)node, e);
            } finally {
                if (toggleSourceMapping) _addSourceMapping = true;
            }

        }
    }
}
