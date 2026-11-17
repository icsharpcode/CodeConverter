using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// Entry point for converting a method body that contains <c>On Error</c> or <c>Resume</c>. See README.md in this directory.
/// </summary>
internal static class OnErrorMethodBodyConverter
{
    /// <returns>The converted body, or null when the statements should be converted one by one as usual.</returns>
    public static Task<IReadOnlyCollection<StatementSyntax>> ConvertOrNullAsync(IReadOnlyCollection<VBSyntax.StatementSyntax> statements,
        Func<IEnumerable<VBSyntax.StatementSyntax>, Task<List<StatementSyntax>>> convertStatementsAsync) =>
        PendingShapeFallback.ConvertOrNullAsync(statements, convertStatementsAsync);
}
