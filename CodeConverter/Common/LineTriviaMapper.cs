using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis.Text;

namespace ICSharpCode.CodeConverter.Common;

internal class LineTriviaMapper
{

    private static readonly string[] _kinds = { AnnotationConstants.SourceStartLineAnnotationKind, AnnotationConstants.SourceEndLineAnnotationKind };
    private readonly SyntaxNode _target;
    private readonly SyntaxNode _source;
    private readonly TextLineCollection _sourceLines;
    private readonly IReadOnlyDictionary<int, TextLine> _targetLeadingTextLineFromSourceLine;
    private readonly IReadOnlyDictionary<int, TextLine> _targetTrailingTextLineFromSourceLine;
    private readonly ImmutableHashSet<int> _startPositionsAlreadyMapped;
    private readonly ImmutableHashSet<int> _trailingPositionsAlreadyMapped;
    private readonly List<SyntaxTriviaList> _leadingTriviaCarriedOver = new();
    private readonly List<SyntaxTriviaList> _trailingTriviaCarriedOver = new();
    private readonly Dictionary<SyntaxToken, (List<IReadOnlyCollection<SyntaxTrivia>> Leading, List<IReadOnlyCollection<SyntaxTrivia>> Trailing)> _targetTokenToTrivia = new();

    private LineTriviaMapper(SyntaxNode source, TextLineCollection sourceLines, SyntaxNode target, Dictionary<int, TextLine> targetLeadingTextLineFromSourceLine,
        Dictionary<int, TextLine> targetTrailingTextLineFromSourceLine, ImmutableHashSet<int> startPositionsAlreadyMapped, ImmutableHashSet<int> trailingPositionsAlreadyMapped)
    {
        _source = source;
        _sourceLines = sourceLines;
        _target = target;
        _targetLeadingTextLineFromSourceLine = targetLeadingTextLineFromSourceLine;
        _targetTrailingTextLineFromSourceLine = targetTrailingTextLineFromSourceLine;
        _startPositionsAlreadyMapped = startPositionsAlreadyMapped;
        _trailingPositionsAlreadyMapped = trailingPositionsAlreadyMapped;
    }

    /// <summary>
    /// For each source line:
    /// * Add leading trivia to the start of the first target line containing a node converted from that source line
    /// * Add trailing trivia to the end of the last target line containing a node converted from that source line
    /// Currently doesn't deal with any within-line trivia (i.e. /* block comments */)
    /// </summary>
    public static SyntaxNode MapSourceTriviaToTarget<TSource, TTarget>(TSource source, TTarget target)
        where TSource : SyntaxNode, ICompilationUnitSyntax where TTarget : SyntaxNode, ICompilationUnitSyntax
    {
        var targetLines = target.GetText().Lines;

        var nodesBySourceLine = GetNodesBySourceLine(target);

        var targetNodesBySourceStartLine = nodesBySourceLine
            .ToDictionary(g => g.Key, g => targetLines.GetLineFromPosition(g.Min(GetPosition)));

        var targetNodesBySourceEndLine = nodesBySourceLine
            .ToDictionary(g => g.Key, g => targetLines.GetLineFromPosition(g.Max(GetPosition)));

        var sourcePositionsAlreadyMapped = target.GetAnnotatedTokens(AnnotationConstants.LeadingTriviaAlreadyMappedAnnotation)
            .SelectMany(x => x.GetAnnotations(AnnotationConstants.LeadingTriviaAlreadyMappedAnnotation), (t, a) => int.Parse(a.Data, CultureInfo.InvariantCulture)).ToImmutableHashSet();
        var trailingTriviaAlreadyMappedBySourceLine = target.GetAnnotatedTokens(AnnotationConstants.TrailingTriviaAlreadyMappedAnnotation)
            .SelectMany(x => x.GetAnnotations(AnnotationConstants.TrailingTriviaAlreadyMappedAnnotation), (t, a) => int.Parse(a.Data, CultureInfo.InvariantCulture)).ToImmutableHashSet();

        var sourceLines = source.GetText().Lines;
        var lineTriviaMapper = new LineTriviaMapper(source, sourceLines, target, targetNodesBySourceStartLine, targetNodesBySourceEndLine, sourcePositionsAlreadyMapped, trailingTriviaAlreadyMappedBySourceLine);

        return lineTriviaMapper.GetTargetWithSourceTrivia();
    }

    private static int GetPosition((SyntaxNodeOrToken Node, int SourceLineIndex, string Kind) n)
    {
        return n.Kind == AnnotationConstants.SourceStartLineAnnotationKind ? n.Node.SpanStart : n.Node.Span.End;
    }

    private static ILookup<int, (SyntaxNodeOrToken Node, int SourceLineIndex, string Kind)> GetNodesBySourceLine(SyntaxNode target)
    {
        return target.GetAnnotatedNodesAndTokens(_kinds).SelectMany(n =>
            n.GetAnnotations(_kinds).Select(a => (Node: n, SourceLineIndex: int.Parse(a.Data, CultureInfo.InvariantCulture), a.Kind))
        ).ToLookup(n => n.SourceLineIndex, n => n);
    }

    private SyntaxNode GetTargetWithSourceTrivia()
    {
        // Reverse iterate to ensure trivia never ends up after the place it came from (consider #if directive or opening brace of method)
        for (int i = _sourceLines.Count - 1; i >= 0; i--) {
            MapLeading(i);
            MapTrailing(i);
        }

        //Reverse trivia due to above reverse looping
        foreach (var trivia in _targetTokenToTrivia) {
            trivia.Value.Leading.Reverse();
            trivia.Value.Trailing.Reverse();
        }

        BalanceTrivia();

        return _target.ReplaceTokens(_targetTokenToTrivia.Keys, AttachMappedTrivia);
    }

    /// <summary>
    /// Trailing trivia can't contain multiple newlines (it gets lost during formatting), so prepend as leading trivia of the next token
    /// </summary>
    private void BalanceTrivia()
    {
        foreach (var trivia in _targetTokenToTrivia.Where(t => t.Value.Trailing.Count > 1).ToList()) {
            var lastIndexToKeep = trivia.Value.Trailing.FindIndex(tl => tl.Any(t => t.IsEndOfLine()));
            var moveToLeadingTrivia = trivia.Value.Trailing.Skip(lastIndexToKeep + 1).ToList();
            if (moveToLeadingTrivia.Any()) {
                var toReplace = trivia.Key.GetNextToken(true);
                var nextTrivia = GetTargetTriviaCollection(toReplace);
                if (!nextTrivia.Leading.Any()) nextTrivia.Leading.Add(toReplace.LeadingTrivia);
                nextTrivia.Leading.InsertRange(0, moveToLeadingTrivia);
                trivia.Value.Trailing.RemoveRange(lastIndexToKeep + 1, moveToLeadingTrivia.Count);
            }
        }
    }

    private SyntaxToken AttachMappedTrivia(SyntaxToken original, SyntaxToken rewritten)
    {
        var trivia = _targetTokenToTrivia[original];
        if (trivia.Leading.Any()) rewritten = rewritten.WithLeadingTrivia(trivia.Leading.SelectMany(tl => tl));
        if (trivia.Trailing.Any()) rewritten = rewritten.WithTrailingTrivia(trivia.Trailing.SelectMany(tl => tl));
        return rewritten;
    }

    private void MapTrailing(int sourceLineIndex)
    {
        var sourceLine = _sourceLines[sourceLineIndex];
        var endOfSourceLine = sourceLine.FindLastTokenWithinLine(_source);
        if (_trailingPositionsAlreadyMapped.Contains(endOfSourceLine.Span.End)) return;

        var triviaToUse = !endOfSourceLine.TrailingTrivia.Any() || endOfSourceLine.TrailingTrivia.OnlyOrDefault().IsEndOfLine() ? _trailingTriviaCarriedOver : _trailingTriviaCarriedOver.Concat(endOfSourceLine.TrailingTrivia).ToList();

        if (triviaToUse.Any()) {
            var targetLine = GetTargetLine(sourceLineIndex, false);
            if (targetLine != default) {
                var originalToReplace = targetLine.GetTrailingForLine(_target);
                if (originalToReplace != default) {
                    var targetTrivia = GetTargetTriviaCollection(originalToReplace);
                    targetTrivia.Trailing.AddRange(triviaToUse.Select(t => t.ConvertTrivia().ToList()));
                    _trailingTriviaCarriedOver.Clear();
                    return;
                }
            }
        }

        if (endOfSourceLine.TrailingTrivia.Any(x => !x.IsWhitespaceOrEndOfLine())) {
            _trailingTriviaCarriedOver.Add(new(endOfSourceLine.TrailingTrivia));
        }
    }

    private void MapLeading(int sourceLineIndex)
    {
        var sourceLine = _sourceLines[sourceLineIndex];
        var startOfSourceLine = sourceLine.FindFirstTokenWithinLine(_source);

        if (_startPositionsAlreadyMapped.Contains(startOfSourceLine.SpanStart)) return;

        var triviaToUse = !startOfSourceLine.LeadingTrivia.Any() ? _leadingTriviaCarriedOver : _leadingTriviaCarriedOver.Concat(startOfSourceLine.LeadingTrivia).ToList();

        if (triviaToUse.Any()) {
            var targetLine = GetTargetLine(sourceLineIndex, true);
            if (targetLine != default) {
                var originalToReplace = targetLine.GetLeadingForLine(_target);
                if (originalToReplace != default) {
                    var targetTrivia = GetTargetTriviaCollection(originalToReplace);
                    targetTrivia.Leading.AddRange(triviaToUse.Select(t => t.ConvertTrivia().ToList()));
                    _leadingTriviaCarriedOver.Clear();
                    return;
                }
            }
        }

        if (startOfSourceLine.LeadingTrivia.Any(x => !x.IsWhitespaceOrEndOfLine())) {
            _leadingTriviaCarriedOver.Add(new(startOfSourceLine.LeadingTrivia));
        }
    }

    private TextLine GetTargetLine(int sourceLineIndex, bool isLeading)
    {
        var exactCollection = isLeading ? _targetLeadingTextLineFromSourceLine : _targetTrailingTextLineFromSourceLine;
        if (exactCollection.TryGetValue(sourceLineIndex, out var targetLine)) return targetLine;
        return default;
    }

    private (List<IReadOnlyCollection<SyntaxTrivia>> Leading, List<IReadOnlyCollection<SyntaxTrivia>> Trailing) GetTargetTriviaCollection(SyntaxToken toReplace)
    {
        if (!_targetTokenToTrivia.TryGetValue(toReplace, out var targetTrivia)) {
            targetTrivia = (new List<IReadOnlyCollection<SyntaxTrivia>>(), new List<IReadOnlyCollection<SyntaxTrivia>>());
            _targetTokenToTrivia[toReplace] = targetTrivia;
        }

        return targetTrivia;
    }
}