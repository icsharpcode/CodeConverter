using Microsoft.CodeAnalysis.VisualBasic;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ICSharpCode.CodeConverter.CSharp;

[System.Diagnostics.DebuggerStepThrough]
public class CommentConvertingMethodBodyVisitor : VisualBasicSyntaxVisitor<Task<SyntaxList<CSSyntax.StatementSyntax>>>
{
    private readonly VisualBasicSyntaxVisitor<Task<SyntaxList<CSSyntax.StatementSyntax>>> _wrappedVisitor;

    public CommentConvertingMethodBodyVisitor(VisualBasicSyntaxVisitor<Task<SyntaxList<CSSyntax.StatementSyntax>>> wrappedVisitor)
    {
        this._wrappedVisitor = wrappedVisitor;
    }

    public override async Task<SyntaxList<CSSyntax.StatementSyntax>> DefaultVisit(SyntaxNode node)
    {
        return await DefaultVisitInnerAsync(node);
    }

    public async Task<SyntaxList<CSSyntax.StatementSyntax>> DefaultVisitInnerAsync(SyntaxNode node)
    {
        try {
            var converted = await _wrappedVisitor.Visit(node);
            return converted.WithVbSourceMappingFrom(node);
        } catch (Exception e) {
            var withTrailingErrorComment = SyntaxFactory.EmptyStatement()
                .WithCsTrailingErrorComment<CSSyntax.StatementSyntax>((VisualBasicSyntaxNode)node, e);
            return SyntaxFactory.SingletonList(withTrailingErrorComment);
        }
    }
}