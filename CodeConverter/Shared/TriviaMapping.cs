using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Shared
{

    internal struct TriviaMapping
    {
        public int SourceLine;
        public int TargetLine;
        public SyntaxTriviaList SourceTrivia;
        public SyntaxToken TargetToken;
        public bool IsLeading;

        public TriviaMapping(int sourceLine, int targetLine, SyntaxTriviaList sourceTrivia, SyntaxToken targetToken, bool isLeading)
        {
            SourceLine = sourceLine;
            TargetLine = targetLine;
            SourceTrivia = sourceTrivia;
            TargetToken = targetToken;
            IsLeading = isLeading;
        }

        public override bool Equals(object obj)
        {
            return obj is TriviaMapping other &&
                   SourceLine == other.SourceLine &&
                   TargetLine == other.TargetLine &&
                   SourceTrivia.Equals(other.SourceTrivia) &&
                   TargetToken.Equals(other.TargetToken) &&
                   IsLeading == other.IsLeading;
        }

        public override int GetHashCode()
        {
            var hashCode = -1113933263;
            hashCode = hashCode * -1521134295 + SourceLine.GetHashCode();
            hashCode = hashCode * -1521134295 + TargetLine.GetHashCode();
            hashCode = hashCode * -1521134295 + SourceTrivia.GetHashCode();
            hashCode = hashCode * -1521134295 + TargetToken.GetHashCode();
            hashCode = hashCode * -1521134295 + IsLeading.GetHashCode();
            return hashCode;
        }

        public void Deconstruct(out int sourceLine, out int targetLine, out SyntaxTriviaList sourceTrivia, out SyntaxToken targetToken, out bool isLeading)
        {
            sourceLine = SourceLine;
            targetLine = TargetLine;
            sourceTrivia = SourceTrivia;
            targetToken = TargetToken;
            isLeading = IsLeading;
        }

        public static implicit operator (int SourceLine, int TargetLine, SyntaxTriviaList SourceTrivia, SyntaxToken TargetToken, bool IsLeading)(TriviaMapping value)
        {
            return (value.SourceLine, value.TargetLine, value.SourceTrivia, value.TargetToken, value.IsLeading);
        }

        public static implicit operator TriviaMapping((int SourceLine, int TargetLine, SyntaxTriviaList SourceTrivia, SyntaxToken TargetToken, bool IsLeading) value)
        {
            return new TriviaMapping(value.SourceLine, value.TargetLine, value.SourceTrivia, value.TargetToken, value.IsLeading);
        }

        public override string ToString()
        {
            var type = IsLeading ? "Leading" : "Trailing";
            return $"{type} {TargetLine}: {TargetToken} - {SourceTrivia}";
        }
    }
}