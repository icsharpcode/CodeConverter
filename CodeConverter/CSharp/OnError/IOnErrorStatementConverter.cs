using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// Converts the <c>On Error</c> and <c>Resume</c> statements the method body visitor meets, for the shape being emitted.
/// </summary>
internal interface IOnErrorStatementConverter
{
    SyntaxList<StatementSyntax> ConvertOnError(VBSyntax.StatementSyntax onErrorStatement);
    SyntaxList<StatementSyntax> ConvertResume(VBSyntax.ResumeStatementSyntax resumeStatement);
}
