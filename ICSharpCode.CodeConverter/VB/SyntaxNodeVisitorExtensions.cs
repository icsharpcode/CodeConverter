using System;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using CS = Microsoft.CodeAnalysis.CSharp;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxNodeExtensions = ICSharpCode.CodeConverter.Util.SyntaxNodeExtensions;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System.Linq;
using System.Collections;

namespace ICSharpCode.CodeConverter.VB
{

    internal static class SyntaxNodeVisitorExtensions
    {
        public static T Accept<T>(this SyntaxNode node, CommentConvertingVisitorWrapper<T> visitorWrapper, bool addSourceMapping = true) where T : VBasic.VisualBasicSyntaxNode
        {
            if (node == null) return default(T);
            try {
                return visitorWrapper.Accept(node, !addSourceMapping);
            } finally {

            }

        }
    }
}
