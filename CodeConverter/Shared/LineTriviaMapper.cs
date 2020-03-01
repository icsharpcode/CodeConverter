using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ICSharpCode.CodeConverter.Shared
{
    internal class LineTriviaMapper
    {

        private static readonly string[] _kinds = new[] { AnnotationConstants.SourceStartLineAnnotationKind, AnnotationConstants.SourceEndLineAnnotationKind };
        private readonly SyntaxNode _target;
        private readonly SyntaxNode _source;
        private readonly TextLineCollection _sourceLines;
        private readonly TextLineCollection _targetLines;
        private readonly IReadOnlyDictionary<int, TextLine> _targetLeadingTextLineFromSourceLine;
        private readonly IReadOnlyDictionary<int, TextLine> _targetTrailingTextLineFromSourceLine;
        private readonly List<SyntaxTriviaList> _leadingTriviaCarriedOver = new List<SyntaxTriviaList>();
        private readonly List<SyntaxTriviaList> _trailingTriviaCarriedOver = new List<SyntaxTriviaList>();
        private readonly Dictionary<SyntaxToken, (List<IReadOnlyCollection<SyntaxTrivia>> Leading, List<IReadOnlyCollection<SyntaxTrivia>> Trailing)> _targetTokenToTrivia = new Dictionary<SyntaxToken, (List<IReadOnlyCollection<SyntaxTrivia>>, List<IReadOnlyCollection<SyntaxTrivia>>)>();

        private LineTriviaMapper(SyntaxNode source, TextLineCollection sourceLines, SyntaxNode target, TextLineCollection targetLines, Dictionary<int, TextLine> targetLeadingTextLineFromSourceLine, Dictionary<int, TextLine> targetTrailingTextLineFromSourceLine)
        {
            _source = source;
            _sourceLines = sourceLines;
            _targetLines = targetLines;
            _target = target;
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
            var targetLines = target.GetText().Lines;

            var nodesBySourceLine = GetNodesBySourceLine(target);

            var targetNodesBySourceStartLine = nodesBySourceLine
                .ToDictionary(g => g.Key, g => targetLines.GetLineFromPosition(g.Min(GetPosition)));

            var targetNodesBySourceEndLine = nodesBySourceLine
                .ToDictionary(g => g.Key, g => targetLines.GetLineFromPosition(g.Max(GetPosition)));

            var sourceLines = source.GetText().Lines;
            var lineTriviaMapper = new LineTriviaMapper(source, sourceLines, target, targetLines, targetNodesBySourceStartLine, targetNodesBySourceEndLine);

            return lineTriviaMapper.GetTargetWithSourceTrivia();
        }

        private static int GetPosition((SyntaxNodeOrToken Node, int SourceLineIndex, string Kind) n)
        {
            return n.Kind == AnnotationConstants.SourceStartLineAnnotationKind ? n.Node.SpanStart : n.Node.Span.End;
        }

        private static ILookup<int, (SyntaxNodeOrToken Node, int SourceLineIndex, string Kind)> GetNodesBySourceLine(SyntaxNode target)
        {
            return target.GetAnnotatedNodesAndTokens(_kinds).SelectMany(n =>
                n.GetAnnotations(_kinds).Select(a => (Node: n, SourceLineIndex: int.Parse(a.Data), Kind: a.Kind))
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

            if (endOfSourceLine.TrailingTrivia.Any(t => !t.IsWhitespaceOrEndOfLine())) {
                _trailingTriviaCarriedOver.Add(endOfSourceLine.TrailingTrivia);
            }

            if (_trailingTriviaCarriedOver.Any()) {
                var targetLine = GetTargetLine(sourceLineIndex, false);
                if (targetLine != default) {
                    var originalToReplace = targetLine.GetTrailingForLine(_target);
                    if (originalToReplace != null) {
                        var targetTrivia = GetTargetTriviaCollection(originalToReplace);
                        targetTrivia.Trailing.AddRange(_trailingTriviaCarriedOver.Select(t => t.ConvertTrivia().ToList()));
                        _trailingTriviaCarriedOver.Clear();
                    }
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

            if (_leadingTriviaCarriedOver.Any()) {
                var targetLine = GetTargetLine(sourceLineIndex, true);
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
}