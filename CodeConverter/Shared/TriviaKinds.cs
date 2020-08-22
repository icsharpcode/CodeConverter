using System;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Shared
{

    internal sealed class TriviaKinds
    {
        public static TriviaKinds All = new TriviaKinds(_ => true);
        public static TriviaKinds ImportantOnly = new TriviaKinds(t => !t.IsWhitespaceOrEndOfLine());
        public static TriviaKinds FormattingOnly = new TriviaKinds(t => t.IsWhitespaceOrEndOfLine());
        public Func<SyntaxTrivia, bool> ShouldAccept { get; }

        private TriviaKinds(Func<SyntaxTrivia, bool> shouldAccept)
        {
            ShouldAccept = shouldAccept ?? throw new ArgumentNullException(nameof(shouldAccept));
        }
    }
}