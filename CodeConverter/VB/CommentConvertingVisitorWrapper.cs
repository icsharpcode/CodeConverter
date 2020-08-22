using System;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using VbSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using CsSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory;

namespace ICSharpCode.CodeConverter.VB
{

    [System.Diagnostics.DebuggerStepThrough]
    internal class CommentConvertingVisitorWrapper<T> where T : VisualBasicSyntaxNode
    {
        private readonly CSharpSyntaxVisitor<T> _wrappedVisitor;

        public CommentConvertingVisitorWrapper(CSharpSyntaxVisitor<T> wrappedVisitor)
        {
               _wrappedVisitor = wrappedVisitor;
        }

        public T Accept(SyntaxNode csNode, bool addSourceMapping)
        {
            try {
                var converted = _wrappedVisitor.Visit(csNode);
                return addSourceMapping ? WithSourceMapping(csNode, converted) : converted.WithoutSourceMapping();
            } catch (Exception e) {
                var dummyStatement = SyntaxFactory.EmptyStatement();
                return ((T)(object)dummyStatement).WithVbTrailingErrorComment((CSharpSyntaxNode)csNode, e);
            }
        }

        /// <remarks>
        /// If lots of special cases, move to wrapping the wrappedVisitor in another visitor, but I'd rather use a simple switch here initially.
        /// </remarks>
        private static T WithSourceMapping(SyntaxNode csNode, T converted)
        {
            switch (csNode) {
                case CsSyntax.AttributeListSyntax _:
                    converted = converted.WithPrependedLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed);
                    break;
                case CsSyntax.CompilationUnitSyntax csCus when converted is VbSyntax.CompilationUnitSyntax vbCus:
                    converted = (T) (object) vbCus.WithEndOfFileToken(
                        vbCus.EndOfFileToken.WithConvertedLeadingTriviaFrom(csCus.EndOfFileToken).WithSourceMappingFrom(csCus.EndOfFileToken)
                     );
                    break;

            }
            return csNode.CopyAnnotationsTo(converted).WithCsSourceMappingFrom(csNode);
        }
    }
}
