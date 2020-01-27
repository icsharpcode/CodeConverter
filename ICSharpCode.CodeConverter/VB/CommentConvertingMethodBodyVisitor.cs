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
    public class CommentConvertingMethodBodyVisitor : CS.CSharpSyntaxVisitor<SyntaxList<VBSyntax.StatementSyntax>>
    {
        private readonly CS.CSharpSyntaxVisitor<SyntaxList<VBSyntax.StatementSyntax>> _wrappedVisitor;
        private readonly TriviaConverter _triviaConverter;
        private readonly BitArray _lineTriviaMapped;

        public CommentConvertingMethodBodyVisitor(CS.CSharpSyntaxVisitor<SyntaxList<VBSyntax.StatementSyntax>> wrappedVisitor, TriviaConverter triviaConverter, BitArray lineTriviaMapped)
        {
            this._wrappedVisitor = wrappedVisitor;
            this._triviaConverter = triviaConverter;
            _lineTriviaMapped = lineTriviaMapped;
        }

        public override SyntaxList<VBSyntax.StatementSyntax> DefaultVisit(SyntaxNode node)
        {
            try {
                var converted = _wrappedVisitor.Visit(node);
                return converted.WithSourceMappingFrom(node);
            } catch (Exception e) {
                var dummyStatement = VBasic.SyntaxFactory.EmptyStatement();
                var withVbTrailingErrorComment = dummyStatement.WithVbTrailingErrorComment<VBSyntax.StatementSyntax>((CS.CSharpSyntaxNode) node, e);
                return VBasic.SyntaxFactory.SingletonList(withVbTrailingErrorComment);
            }
        }
    }
}
