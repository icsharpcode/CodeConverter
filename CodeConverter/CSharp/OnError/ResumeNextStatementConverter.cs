using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VBasicSyntaxKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// In the resume next shapes, an <c>On Error</c> statement clears <c>Err</c>. In the dynamic shape it also records whether errors are now ignored.
/// </summary>
internal sealed class ResumeNextStatementConverter : IOnErrorStatementConverter
{
    private readonly ErrStatements _err;
    private readonly string _ignoringErrorsFlag;

    /// <param name="ignoringErrorsFlag">The local that the guards' filters read, or null when the handler is known from where the statement is.</param>
    public ResumeNextStatementConverter(ErrStatements err, string ignoringErrorsFlag)
    {
        _err = err;
        _ignoringErrorsFlag = ignoringErrorsFlag;
    }

    public SyntaxList<StatementSyntax> ConvertOnError(VBSyntax.StatementSyntax onErrorStatement)
    {
        if (_ignoringErrorsFlag == null) return _err.Clear();
        var value = onErrorStatement is VBSyntax.OnErrorResumeNextStatementSyntax ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression;
        var assignment = SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
            SyntaxFactory.IdentifierName(_ignoringErrorsFlag), SyntaxFactory.LiteralExpression(value)));
        return SyntaxFactory.List(new StatementSyntax[] { assignment }.Concat(_err.Clear()));
    }

    public SyntaxList<StatementSyntax> ConvertResume(VBSyntax.ResumeStatementSyntax resumeStatement) =>
        throw new NotSupportedException("The resume next shapes are only chosen when no Resume statement can run");
}
