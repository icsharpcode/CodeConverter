namespace ICSharpCode.CodeConverter.Common;

internal sealed class TriviaKinds
{
    public static TriviaKinds All = new(_ => true);
    public static TriviaKinds ImportantOnly = new(t => !t.IsWhitespaceOrEndOfLine());
    public static TriviaKinds FormattingOnly = new(t => t.IsWhitespaceOrEndOfLine());
    public Func<SyntaxTrivia, bool> ShouldAccept { get; }

    private TriviaKinds(Func<SyntaxTrivia, bool> shouldAccept)
    {
        ShouldAccept = shouldAccept ?? throw new ArgumentNullException(nameof(shouldAccept));
    }
}