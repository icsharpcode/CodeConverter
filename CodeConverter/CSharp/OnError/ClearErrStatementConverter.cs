using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// An <c>On Error GoTo 0</c> with no handler to deactivate only clears <c>Err</c>, which VB does for every <c>On Error</c> statement.
/// </summary>
internal sealed class ClearErrStatementConverter : IOnErrorStatementConverter
{
    private readonly ErrStatements _err;

    public ClearErrStatementConverter(ErrStatements err) => _err = err;

    public SyntaxList<StatementSyntax> ConvertOnError(VBSyntax.StatementSyntax onErrorStatement) => _err.Clear();

    public SyntaxList<StatementSyntax> ConvertResume(VBSyntax.ResumeStatementSyntax resumeStatement) =>
        throw new NotSupportedException("The clear-only shape is only chosen when the method has no Resume statement");
}
