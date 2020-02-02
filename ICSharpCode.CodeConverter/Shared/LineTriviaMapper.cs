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
                .ToDictionary(g => g.Key, g => originalTargetLines.GetLineFromPosition(g.Min(x => x.GetLocation().SourceSpan.Start)));

            var targetNodesBySourceEndLine = target.GetAnnotatedNodesAndTokens(AnnotationConstants.SourceEndLineAnnotationKind)
                .ToLookup(n => n.GetAnnotations(AnnotationConstants.SourceEndLineAnnotationKind).Select(a => int.Parse(a.Data)).Max())
                .ToDictionary(g => g.Key, g => originalTargetLines.GetLineFromPosition(g.Max(x => x.GetLocation().SourceSpan.End)));

            var sourceLines = source.GetText().Lines;
            var lineTriviaMapper = new LineTriviaMapper(source, sourceLines, targetNodesBySourceStartLine, targetNodesBySourceEndLine);

            return lineTriviaMapper.GetTargetWithSourceTrivia(target);
        }

        private SyntaxNode GetTargetWithSourceTrivia(SyntaxNode target)
        {
            //TODO Try harder to avoid losing track of various precalculated positions changing during the replacements, for example build up a dictionary of replacements and make them in a single ReplaceTokens call
            //TODO Possible perf: Find token starting from position of last replaced token rather than from the root node each time?
            _target = target;
            for (int i = _sourceLines.Count - 1; i >= 0; i--) {
                ConvertTrailingForSourceLine(target, i);
                ConvertLeadingForSourceLine(target, i);
            }
            //return target;
            return _target.ReplaceTokens(_targetTokenToTrivia.Keys, (original, rewritten) => {
                var trivia = _targetTokenToTrivia[original];
                foreach (var triviaList in trivia.Trailing) {
                    rewritten = rewritten.WithTrailingTrivia(PrependPreservingImportantTrivia(triviaList.ToList(), rewritten.TrailingTrivia));
                }
                foreach (var triviaList in trivia.Leading) {
                    rewritten = rewritten.WithLeadingTrivia(PrependPreservingImportantTrivia(triviaList.ToList(), rewritten.LeadingTrivia));
                }
                return rewritten;
            });
        }



        private SyntaxNode ConvertTrailingForSourceLine(SyntaxNode target, int sourceLineIndex)
        {
            var sourceLine = _sourceLines[sourceLineIndex];
            var endOfSourceLine = FindNonZeroWidthToken(_source, sourceLine.End);

            if (endOfSourceLine.TrailingTrivia.Any(t => !t.IsWhitespaceOrEndOfLine())) {
                var targetLine = GetBestLine(_targetTrailingTextLineFromSourceLine, sourceLineIndex);
                if (targetLine != default) {
                    _trailingTriviaCarriedOver.Add(endOfSourceLine.TrailingTrivia);
                    var originalToReplace = GetTrailingForLine(_target, targetLine);
                    SaveTrailingTrivia(originalToReplace, _trailingTriviaCarriedOver);
                    _trailingTriviaCarriedOver.Clear();
                } else if (endOfSourceLine.TrailingTrivia.Span.Start > sourceLine.Start) {
                    _trailingTriviaCarriedOver.Add(endOfSourceLine.TrailingTrivia);
                }
            }

            return target;
        }

        private static SyntaxToken GetTrailingForLine(SyntaxNode target, TextLine targetLine)
        {
            var toReplace = FindNonZeroWidthToken(target, targetLine.End);
            if (toReplace.Width() == 0) {
                toReplace = toReplace.GetPreviousToken(); //Never append *trailing* trivia to the end of file token
            }

            return toReplace;
        }

        private SyntaxNode ConvertLeadingForSourceLine(SyntaxNode target, int sourceLineIndex)
        {
            var sourceLine = _sourceLines[sourceLineIndex];
            var startOfSourceLine = FindNonZeroWidthToken(_source, sourceLine.Start);
            if (startOfSourceLine.LeadingTrivia.Any(t => !t.IsWhitespaceOrEndOfLine())) {
                var targetLine = GetBestLine(_targetLeadingTextLineFromSourceLine, sourceLineIndex);
                if (targetLine != default) {
                    _leadingTriviaCarriedOver.Add(startOfSourceLine.LeadingTrivia);
                    var originalToReplace = GetLeadingForLine(_target, targetLine);
                    SaveLeadingTrivia(originalToReplace, _leadingTriviaCarriedOver);
                    _leadingTriviaCarriedOver.Clear();
                } else if (startOfSourceLine.LeadingTrivia.Span.End < sourceLine.End) {
                    _leadingTriviaCarriedOver.Add(startOfSourceLine.LeadingTrivia);
                }
            }

            return target;
        }

        private static SyntaxToken GetLeadingForLine(SyntaxNode target, TextLine targetLine)
        {
            var toReplace = FindNonZeroWidthToken(target, targetLine.Start);
            if (toReplace.Span.End < targetLine.Start) {
                toReplace = toReplace.GetNextToken(); //TODO: Find out why FindToken is off by one from what I want sometimes, is there a better alternative?
            }

            return toReplace;
        }

        private TextLine GetBestLine(IReadOnlyDictionary<int, TextLine> sourceToTargetLine, int sourceLineIndex)
        {
            if (sourceToTargetLine.TryGetValue(sourceLineIndex, out var targetLineIndex)) return targetLineIndex;
            return default;
        }

        private void SaveLeadingTrivia(SyntaxToken toReplace, List<SyntaxTriviaList> leadingTriviaLists)
        {
            var targetTrivia = GetTargetTriviaCollection(toReplace);
            targetTrivia.Trailing.AddRange(leadingTriviaLists.Select(t => t.ConvertTrivia().ToList()));
        }

        private void SaveTrailingTrivia(SyntaxToken toReplace, List<SyntaxTriviaList> trailingTriviaLists)
        {
            var targetTrivia = GetTargetTriviaCollection(toReplace);
            targetTrivia.Trailing.AddRange(trailingTriviaLists.Select(t => t.ConvertTrivia().ToList()));
        }

        private static IEnumerable<SyntaxTrivia> PrependPreservingImportantTrivia(IReadOnlyCollection<SyntaxTrivia> convertedTrivia, IReadOnlyCollection<SyntaxTrivia> existingTrivia)
        {
            if (existingTrivia.Any(t => !t.IsWhitespaceOrEndOfLine())) {
                return convertedTrivia.Concat(existingTrivia);
            }
            return convertedTrivia;
        }

        private (List<IReadOnlyCollection<SyntaxTrivia>> Leading, List<IReadOnlyCollection<SyntaxTrivia>> Trailing) GetTargetTriviaCollection(SyntaxToken toReplace)
        {
            if (!_targetTokenToTrivia.TryGetValue(toReplace, out var targetTrivia)) {
                targetTrivia = (new List<IReadOnlyCollection<SyntaxTrivia>>(), new List<IReadOnlyCollection<SyntaxTrivia>>());
                _targetTokenToTrivia[toReplace] = targetTrivia;
            }

            return targetTrivia;
        }

        private static SyntaxToken FindNonZeroWidthToken(SyntaxNode node, int position)
        {
            var syntaxToken = node.FindToken(position);
            if (syntaxToken.FullWidth() == 0) {
                return syntaxToken.GetPreviousToken();
            } else {
                return syntaxToken;
            }
        }
    }
}