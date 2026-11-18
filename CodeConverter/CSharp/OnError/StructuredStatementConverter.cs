using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VBasicSyntaxKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// In the structured shape, an <c>On Error</c> statement only clears <c>Err</c>: the <c>try</c> placement does the rest.
/// The handler's <c>Resume L</c> clears <c>Err</c> and jumps out of the <c>catch</c>, which C# allows.
/// </summary>
internal sealed class StructuredStatementConverter : IOnErrorStatementConverter
{
    private readonly ErrStatements _err;
    private readonly OnErrorModel _model;

    public StructuredStatementConverter(ErrStatements err, OnErrorModel model)
    {
        _err = err;
        _model = model;
    }

    public SyntaxList<StatementSyntax> ConvertOnError(VBSyntax.StatementSyntax onErrorStatement) => _err.Clear();

    public SyntaxList<StatementSyntax> ConvertResume(VBSyntax.ResumeStatementSyntax resumeStatement)
    {
        if (!resumeStatement.IsKind(VBasicSyntaxKind.ResumeLabelStatement)) {
            throw new NotSupportedException("The structured shape is only chosen when the handler resumes to a label");
        }
        var label = _model.LabelText(LabelName.Of(resumeStatement.Label));
        return _err.Clear().Add(SyntaxFactory.GotoStatement(SyntaxKind.GotoStatement, ValidSyntaxFactory.IdentifierName(label)));
    }
}
