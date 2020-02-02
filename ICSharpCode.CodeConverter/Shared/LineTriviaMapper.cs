using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ICSharpCode.CodeConverter.Shared
{
    internal class LineTriviaMapper
    {
        private SyntaxNode _target;
        private readonly SyntaxNode _source;
        private readonly TextLineCollection _sourceLines;
        private readonly IReadOnlyDictionary<int, TextLine> _targetLeadingTextLineFromSourceLine;
        private readonly IReadOnlyDictionary<int, TextLine> _targetTrailingTextLineFromSourceLine;
        private readonly List<SyntaxTriviaList> _leadingTriviaCarriedOver = new List<SyntaxTriviaList>();
        private readonly List<SyntaxTriviaList> _trailingTriviaCarriedOver = new List<SyntaxTriviaList>();
        private readonly Dictionary<SyntaxToken, (List<IReadOnlyCollection<SyntaxTrivia>> Leading, List<IReadOnlyCollection<SyntaxTrivia>> Trailing)> _targetTokenToTrivia = new Dictionary<SyntaxToken, (List<IReadOnlyCollection<SyntaxTrivia>>, List<IReadOnlyCollection<SyntaxTrivia>>)>();

        public LineTriviaMapper(SyntaxNode source, TextLineCollection sourceLines, Dictionary<int, TextLine> targetLeadingTextLineFromSourceLine, Dictionary<int, TextLine> targetTrailingTextLineFromSourceLine)
        {
            _source = source;
            _sourceLines = sourceLines;
            _targetLeadingTextLineFromSourceLine = targetLeadingTextLineFromSourceLine;
            _targetTrailingTextLineFromSourceLine = targetTrailingTextLineFromSourceLine;
        }

        /// <summary>
        /// For each source line:
        /// * Add leading trivia to the start of the first target line containing a node converted from that source line
        /// * Add trailing trivia to the end of the last target line containing a node converted from that source line
        /// Makes no attempt to convert whitespace/newline-only trivia
        /// Currently doesn't deal with any within-line trivia (i.e. /* block comments */)
        /// </summary>
        public static SyntaxNode MapSourceTriviaToTarget<TSource, TTarget>(TSource source, TTarget target)
            where TSource : SyntaxNode, ICompilationUnitSyntax where TTarget : SyntaxNode, ICompilationUnitSyntax
        {
            var originalTargetLines = target.GetText().Lines;

            var targetNodesBySourceStartLine = target.GetAnnotatedNodesAndTokens(AnnotationConstants.SourceStartLineAnnotationKind)
                .ToLookup(n => n.GetAnnotations(AnnotationConstants.SourceStartLineAnnotationKind).Select(a => int.Parse(a.Data)).Min())
                .ToDictionary(g => g.Key, g => originalTargetLines.GetLineFromPosition(g.Min(x => x.Span.Start)));

            var targetNodesBySourceEndLine = target.GetAnnotatedNodesAndTokens(AnnotationConstants.SourceEndLineAnnotationKind)
                .ToLookup(n => n.GetAnnotations(AnnotationConstants.SourceEndLineAnnotationKind).Select(a => int.Parse(a.Data)).Max())
                .ToDictionary(g => g.Key, g => originalTargetLines.GetLineFromPosition(g.Max(x => x.Span.End)));

            var sourceLines = source.GetText().Lines;
            var lineTriviaMapper = new LineTriviaMapper(source, sourceLines, targetNodesBySourceStartLine, targetNodesBySourceEndLine);

            return lineTriviaMapper.GetTargetWithSourceTrivia(target);
        }

        private SyntaxNode GetTargetWithSourceTrivia(SyntaxNode target)
        {
            //TODO Possible perf: Find token starting from position of last replaced token rather than from the root node each time?
            _target = target;

            for (int i = 0; i < _sourceLines.Count ; i++) {
                MapLeading(i);
            }

            for (int i = _sourceLines.Count - 1; i >= 0; i--) {
                MapTrailing(i);
            }

            return _target.ReplaceTokens(_targetTokenToTrivia.Keys, WithMappedTrivia);
        }

        private SyntaxToken WithMappedTrivia(SyntaxToken original, SyntaxToken rewritten)
        {
            var trivia = _targetTokenToTrivia[original];
            //TODO SelectMany
            foreach (var triviaList in trivia.Trailing) {
                rewritten = rewritten.WithTrailingTrivia(triviaList.PrependPreservingImportantTrivia(rewritten.TrailingTrivia));
            }
            foreach (var triviaList in trivia.Leading) {
                rewritten = rewritten.WithLeadingTrivia(triviaList.PrependPreservingImportantTrivia(rewritten.LeadingTrivia));
            }
            return rewritten;
        }

        private void MapTrailing(int sourceLineIndex)
        {
            var sourceLine = _sourceLines[sourceLineIndex];
            var endOfSourceLine = sourceLine.FindLastTokenWithinLine(_source);

            if (endOfSourceLine.TrailingTrivia.Any(t => !t.IsWhitespaceOrEndOfLine())) {
                _trailingTriviaCarriedOver.Add(endOfSourceLine.TrailingTrivia);
            }

            var targetLine = GetBestLine(_targetTrailingTextLineFromSourceLine, sourceLineIndex);
            if (targetLine != default) {
                var originalToReplace = targetLine.GetTrailingForLine(_target); //TODO Use withinline textline extensions
                if (originalToReplace != null) {
                    var targetTrivia = GetTargetTriviaCollection(originalToReplace);
                    targetTrivia.Trailing.AddRange(_trailingTriviaCarriedOver.Select(t => t.ConvertTrivia().ToList()));
                    _trailingTriviaCarriedOver.Clear();
                }
            }
        }

        private void MapLeading(int sourceLineIndex)
        {
            var sourceLine = _sourceLines[sourceLineIndex];
            var startOfSourceLine = sourceLine.FindFirstTokenWithinLine(_source);

            if (startOfSourceLine.LeadingTrivia.Any(t => !t.IsWhitespaceOrEndOfLine())) {
                _leadingTriviaCarriedOver.Add(startOfSourceLine.LeadingTrivia);
            }

            var targetLine = GetBestLine(_targetLeadingTextLineFromSourceLine, sourceLineIndex);
            if (targetLine != default) {
                var originalToReplace = targetLine.GetLeadingForLine(_target);
                if (originalToReplace != default) {
                    var targetTrivia = GetTargetTriviaCollection(originalToReplace);
                    targetTrivia.Leading.AddRange(_leadingTriviaCarriedOver.Select(t => t.ConvertTrivia().ToList()));
                    _leadingTriviaCarriedOver.Clear();
                    return;
                }
            }
        }

        private TextLine GetBestLine(IReadOnlyDictionary<int, TextLine> sourceToTargetLine, int sourceLineIndex)
        {
            if (sourceToTargetLine.TryGetValue(sourceLineIndex, out var targetLine)) return targetLine;
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
}