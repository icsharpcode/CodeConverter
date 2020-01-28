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
        private readonly SyntaxNode source;
        private readonly TextLineCollection sourceLines;
        private readonly TextLineCollection originalTargetLines;
        private readonly IReadOnlyDictionary<int, TextLine> targetLeadingTextLineFromSourceLine;
        private readonly IReadOnlyDictionary<int, TextLine> targetTrailingTextLineFromSourceLine;

        public LineTriviaMapper(SyntaxNode source, TextLineCollection sourceLines, TextLineCollection originalTargetLines, Dictionary<int, TextLine> targetLeadingTextLineFromSourceLine, Dictionary<int, TextLine> targetTrailingTextLineFromSourceLine)
        {
            this.source = source;
            this.sourceLines = sourceLines;
            this.originalTargetLines = originalTargetLines;
            this.targetLeadingTextLineFromSourceLine = targetLeadingTextLineFromSourceLine;
            this.targetTrailingTextLineFromSourceLine = targetTrailingTextLineFromSourceLine;
        }

        /// <summary>
        /// For each source line:
        /// * Add leading trivia to the start of the first target line containing a node converted from that source line
        /// * Add trailing trivia to the end of the last target line containing a node converted from that source line
        /// Makes no attempt to convert whitespace/newline-only trivia
        /// Currently doesn't deal with any within-line trivia (i.e. /* block comments */)
        /// </summary>
        public static SyntaxNode MapSourceTriviaToTarget(SyntaxNode source, SyntaxNode target)
        {
            var originalTargetLines = target.GetText().Lines;

            var targetNodesBySourceStartLine = target.GetAnnotatedNodesAndTokens(AnnotationConstants.SourceStartLineAnnotationKind)
                .ToLookup(n => n.GetAnnotations(AnnotationConstants.SourceStartLineAnnotationKind).Select(a => int.Parse(a.Data)).Min())
                .ToDictionary(g => g.Key, g => originalTargetLines.GetLineFromPosition(g.Min(x => x.GetLocation().SourceSpan.Start)));

            var targetNodesBySourceEndLine = target.GetAnnotatedNodesAndTokens(AnnotationConstants.SourceEndLineAnnotationKind)
                .ToLookup(n => n.GetAnnotations(AnnotationConstants.SourceEndLineAnnotationKind).Select(a => int.Parse(a.Data)).Max())
                .ToDictionary(g => g.Key, g => originalTargetLines.GetLineFromPosition(g.Max(x => x.GetLocation().SourceSpan.End)));

            var sourceLines = source.GetText().Lines;
            var lineTriviaMapper = new LineTriviaMapper(source, sourceLines, originalTargetLines, targetNodesBySourceStartLine, targetNodesBySourceEndLine);
            return lineTriviaMapper.GetTargetWithSourceTrivia(target);
        }

        private SyntaxNode GetTargetWithSourceTrivia(SyntaxNode target)
        {
            //TODO Try harder to avoid losing track of various precalculated positions changing during the replacements, for example build up a dictionary of replacements and make them in a single ReplaceTokens call
            //TODO Keep track of lost comments and put them in a comment at the end of the file
            var triviaMappings = new List<TriviaMapping>();
            var leadingSourceForTargetMappings = new List<TriviaMapping>();
            for (int i = sourceLines.Count - 1; i >= 0; i--) {
                var sourceLine = sourceLines[i];
                var endOfSourceLine = source.FindToken(sourceLine.End);
                var startOfSourceLine = source.FindTokenOnRightOfPosition(sourceLine.Start);

                if (endOfSourceLine.TrailingTrivia.Any(t => !t.IsWhitespaceOrEndOfLine())) {
                    var line = GetBestLine(targetTrailingTextLineFromSourceLine, i);
                    if (line != default) {
                        var convertedTrivia = endOfSourceLine.TrailingTrivia.ConvertTrivia();
                        var toReplace = target.FindToken(line.End);
                        target = target.ReplaceToken(toReplace, toReplace.WithTrailingTrivia(convertedTrivia));
                        triviaMappings.Add(new TriviaMapping(i, line.LineNumber, endOfSourceLine.TrailingTrivia, toReplace, false));
                    }
                }

                if (startOfSourceLine.LeadingTrivia.Any(t => !t.IsWhitespaceOrEndOfLine())) {
                    var line = GetBestLine(targetLeadingTextLineFromSourceLine, i);
                    if (line != default) {
                        var convertedTrivia = startOfSourceLine.LeadingTrivia.ConvertTrivia();
                        var toReplace = target.FindTokenOnRightOfPosition(line.Start);
                        target = target.ReplaceToken(toReplace, toReplace.WithLeadingTrivia(convertedTrivia));
                        triviaMappings.Add(new TriviaMapping(i, line.LineNumber, endOfSourceLine.TrailingTrivia, toReplace, true));
                    }
                }
            }
            triviaMappings = triviaMappings.OrderBy(x => x.TargetLine).ThenBy(x => !x.IsLeading).ToList();
            return target;
        }

        private TextLine GetBestLine(IReadOnlyDictionary<int, TextLine> sourceToTargetLine, int sourceLineIndex)
        {
            if (sourceToTargetLine.TryGetValue(sourceLineIndex, out var targetLineIndex)) return targetLineIndex;
            return default;
        }
    }
}