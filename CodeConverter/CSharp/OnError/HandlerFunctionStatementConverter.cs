using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VBasicSyntaxKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// In the handler function shape, <c>On Error</c> clears <c>Err</c>, and <c>Resume</c> / <c>Resume Next</c> clear it and return to the guard's <c>catch</c>
/// with the value that says which of the two the handler chose.
/// </summary>
internal sealed class HandlerFunctionStatementConverter : IOnErrorStatementConverter
{
    /// <summary>Marks the returns made by <c>Resume</c> statements, so they aren't mistaken for the method's own <c>Return</c> statements.</summary>
    public static readonly SyntaxAnnotation ResumeReturn = new();

    private readonly ErrStatements _err;
    private readonly HandlerFunctionPlan _plan;

    public HandlerFunctionStatementConverter(ErrStatements err, HandlerFunctionPlan plan)
    {
        _err = err;
        _plan = plan;
    }

    public SyntaxList<StatementSyntax> ConvertOnError(VBSyntax.StatementSyntax onErrorStatement) => _err.Clear();

    public SyntaxList<StatementSyntax> ConvertResume(VBSyntax.ResumeStatementSyntax resumeStatement)
    {
        bool? value = resumeStatement.IsKind(VBasicSyntaxKind.ResumeStatement) ? _plan.ResumeValue : _plan.ResumeNextValue;
        var returnStatement = (value is { } v
            ? SyntaxFactory.ReturnStatement(SyntaxFactory.LiteralExpression(v ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression))
            : SyntaxFactory.ReturnStatement()).WithAdditionalAnnotations(ResumeReturn);
        return _err.Clear().Add(returnStatement);
    }
}
