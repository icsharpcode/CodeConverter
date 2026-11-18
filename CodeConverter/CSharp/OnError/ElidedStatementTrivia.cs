using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// Keeps the comments of a statement that converts to nothing (such as an <c>On Error GoTo 0</c> when <c>Err</c> isn't maintained) with the code that follows it.
/// </summary>
/// <remarks>
/// The line-based trivia mapping only attaches trivia to lines that have converted nodes, and carries the rest back to the statement before.
/// For a handler that puts the comments in the <c>try</c>, so they are attached to the next statement of the same block instead.
/// </remarks>
internal static class ElidedStatementTrivia
{
    public static bool HasComments(VBSyntax.StatementSyntax elided) =>
        elided.GetFirstToken().LeadingTrivia.Concat(elided.GetLastToken().TrailingTrivia).Any(t => !t.IsWhitespaceOrEndOfLine());

    /// <summary>
    /// Puts the comments of <paramref name="elided"/> before <paramref name="next"/>, and stops the line-based mapping from also moving them.
    /// </summary>
    /// <param name="nextSource">The statement <paramref name="next"/> was converted from, whose own leading trivia has to be kept as the mapping skips it too.</param>
    public static StatementSyntax PrependTo(StatementSyntax next, VBSyntax.StatementSyntax nextSource, IReadOnlyCollection<VBSyntax.StatementSyntax> elided)
    {
        var withComments = elided.Where(HasComments).ToList();
        if (!withComments.Any()) return next;

        var carried = withComments.SelectMany(CommentLines).ToList();
        var firstToken = next.GetFirstToken();
        var nextSourceStart = nextSource.GetFirstToken().SpanStart;
        var alreadyMapped = firstToken.GetAnnotations(AnnotationConstants.LeadingTriviaAlreadyMappedAnnotation).Any(a => a.Data == nextSourceStart.ToString(CultureInfo.InvariantCulture));
        var ownLeadingTrivia = alreadyMapped ? firstToken.LeadingTrivia : SyntaxFactory.TriviaList(nextSource.GetFirstToken().LeadingTrivia.ToList().ConvertTrivia());

        var withTrivia = firstToken.WithLeadingTrivia(carried.Concat(ownLeadingTrivia));
        if (!alreadyMapped) withTrivia = withTrivia.WithAlreadyMappedStartLineAnnotation(nextSourceStart);
        foreach (var statement in withComments) {
            withTrivia = withTrivia
                .WithAlreadyMappedStartLineAnnotation(statement.GetFirstToken().SpanStart)
                .WithAlreadyMappedEndLineAnnotation(statement.GetLastToken().Span.End);
        }
        return next.ReplaceToken(firstToken, withTrivia);
    }

    private static IEnumerable<SyntaxTrivia> CommentLines(VBSyntax.StatementSyntax elided)
    {
        var leading = elided.GetFirstToken().LeadingTrivia.ToList().ConvertTrivia();
        var trailing = elided.GetLastToken().TrailingTrivia.ToList().ConvertTrivia().ToList();
        if (trailing.Any(t => !t.IsWhitespaceOrEndOfLine()) && !trailing.Last().IsEndOfLine()) trailing.Add(SyntaxFactory.CarriageReturnLineFeed);
        return leading.Concat(trailing);
    }
}
