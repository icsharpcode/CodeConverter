using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using SyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory;

namespace ICSharpCode.CodeConverter.VB;

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
            case CSSyntax.AttributeListSyntax _:
                converted = converted.WithPrependedLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed);
                break;
            case CSSyntax.CompilationUnitSyntax csCus when converted is VBSyntax.CompilationUnitSyntax vbCus:
                converted = (T) (object) vbCus.WithEndOfFileToken(
                    vbCus.EndOfFileToken.WithConvertedLeadingTriviaFrom(csCus.EndOfFileToken).WithSourceMappingFrom(csCus.EndOfFileToken)
                );
                break;

        }
        return csNode.CopyAnnotationsTo(converted).WithCsSourceMappingFrom(csNode);
    }
}