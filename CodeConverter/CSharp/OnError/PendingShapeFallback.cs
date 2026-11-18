using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// Output for methods whose shape has no emitter yet: one try/catch when the method has exactly one label and one <c>On Error GoTo</c>,
/// otherwise null so the statements are converted one by one (each unconvertible statement gets an #error).
/// </summary>
/// <remarks>
/// The try/catch ignores how the handler leaves, so it's only right for some methods. It's kept so methods that aren't handled by a shape emitter yet don't change output.
/// See https://learn.microsoft.com/en-us/dotnet/visual-basic/language-reference/statements/on-error-statement
/// </remarks>
internal static class PendingShapeFallback
{
    public static async Task<IReadOnlyCollection<StatementSyntax>> ConvertOrNullAsync(IReadOnlyCollection<VBSyntax.StatementSyntax> statements,
        Func<IEnumerable<VBSyntax.StatementSyntax>, Task<List<StatementSyntax>>> convertStatementsAsync)
    {
        var onlyIdentifierLabel = statements.OnlyOrDefault(s => s.IsKind(VBasic.SyntaxKind.LabelStatement));
        var onlyOnErrorGotoStatement = statements.OnlyOrDefault(s => s.IsKind(VBasic.SyntaxKind.OnErrorGoToLabelStatement));
        if (onlyIdentifierLabel == null || onlyOnErrorGotoStatement == null) return null;

        var statementsList = statements.ToList();
        var onlyIdentifierLabelIndex = statementsList.IndexOf(onlyIdentifierLabel);
        var onlyOnErrorGotoStatementIndex = statementsList.IndexOf(onlyOnErrorGotoStatement);
        if (onlyOnErrorGotoStatementIndex >= onlyIdentifierLabelIndex) return null;

        var beforeStatements = await convertStatementsAsync(statements.Take(onlyOnErrorGotoStatementIndex));
        var tryBlockStatements = await convertStatementsAsync(statements.Take(onlyIdentifierLabelIndex).Skip(onlyOnErrorGotoStatementIndex + 1));
        var tryBlock = SyntaxFactory.Block(tryBlockStatements);
        var afterStatements = await convertStatementsAsync(statements.Skip(onlyIdentifierLabelIndex + 1));

        var catchClauseSyntax = SyntaxFactory.CatchClause();

        // Default to putting the statements after the catch block in case logic falls through, but if the last statement is a return, put them inside the catch block for neatness.
        if (tryBlockStatements.LastOrDefault().IsKind(SyntaxKind.ReturnStatement)) {
            catchClauseSyntax = catchClauseSyntax.WithBlock(SyntaxFactory.Block(afterStatements));
            afterStatements = new List<StatementSyntax>();
        }

        var tryStatement = SyntaxFactory.TryStatement(SyntaxFactory.SingletonList(catchClauseSyntax)).WithBlock(tryBlock);
        return beforeStatements.Append(tryStatement).Concat(afterStatements).ToList();
    }
}
