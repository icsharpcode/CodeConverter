using System;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using CS = Microsoft.CodeAnalysis.CSharp;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.VB
{

    [System.Diagnostics.DebuggerStepThrough]
    public class CommentConvertingMethodBodyVisitor : CS.CSharpSyntaxVisitor<SyntaxList<VBSyntax.StatementSyntax>>
    {
        private readonly CS.CSharpSyntaxVisitor<SyntaxList<VBSyntax.StatementSyntax>> _wrappedVisitor;

        public CommentConvertingMethodBodyVisitor(CS.CSharpSyntaxVisitor<SyntaxList<VBSyntax.StatementSyntax>> wrappedVisitor)
        {
            this._wrappedVisitor = wrappedVisitor;
        }

        public override SyntaxList<VBSyntax.StatementSyntax> DefaultVisit(SyntaxNode node)
        {
            try {
                var converted = _wrappedVisitor.Visit(node);
                return converted.WithCsSourceMappingFrom(node);
            } catch (Exception e) {
                var dummyStatement = VBasic.SyntaxFactory.EmptyStatement();
                var withVbTrailingErrorComment = dummyStatement.WithVbTrailingErrorComment<VBSyntax.StatementSyntax>((CS.CSharpSyntaxNode) node, e);
                return VBasic.SyntaxFactory.SingletonList(withVbTrailingErrorComment);
            }
        }
    }
}
