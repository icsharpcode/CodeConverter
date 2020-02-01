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
        private readonly IReadOnlyDictionary<int, TextLine> targetLeadingTextLineFromSourceLine;
        private readonly IReadOnlyDictionary<int, TextLine> targetTrailingTextLineFromSourceLine;
        private readonly List<SyntaxTriviaList> leadingTriviaCarriedOver = new List<SyntaxTriviaList>();
        private readonly List<SyntaxTriviaList> trailingTriviaCarriedOver = new List<SyntaxTriviaList>();
        public LineTriviaMapper(SyntaxNode source, TextLineCollection sourceLines, Dictionary<int, TextLine> targetLeadingTextLineFromSourceLine, Dictionary<int, TextLine> targetTrailingTextLineFromSourceLine)
        {
            this.source = source;
            this.sourceLines = sourceLines;
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

            for (int i = sourceLines.Count - 1; i >= 0; i--) {
                target = ConvertTrailingForSourceLine(target, i);
                target = ConvertLeadingForSourceLine(target, i);
            }
            return target;
        }



        private SyntaxNode ConvertTrailingForSourceLine(SyntaxNode target, int sourceLineIndex)
        {
            var sourceLine = sourceLines[sourceLineIndex];
            var endOfSourceLine = FindNonZeroWidthToken(source, sourceLine.End);

            if (endOfSourceLine.TrailingTrivia.Any(t => !t.IsWhitespaceOrEndOfLine())) {
                var line = GetBestLine(targetTrailingTextLineFromSourceLine, sourceLineIndex);
                if (line != default) {
                    foreach (var triviaList in trailingTriviaCarriedOver) {
                        target = PrependTrailingTrivia(target, line, triviaList);
                    }
                    trailingTriviaCarriedOver.Clear();
                    target = PrependTrailingTrivia(target, line, endOfSourceLine.TrailingTrivia);
                } else if (endOfSourceLine.TrailingTrivia.Span.Start > sourceLine.Start) {
                    trailingTriviaCarriedOver.Add(endOfSourceLine.TrailingTrivia);
                }
            }

            return target;
        }

        private static SyntaxNode PrependTrailingTrivia(SyntaxNode target, TextLine targetLine, SyntaxTriviaList trailingTrivia)
        {
            var convertedTrivia = trailingTrivia.ConvertTrivia();
            var toReplace = FindNonZeroWidthToken(target, targetLine.End);
            if (toReplace.Width() == 0) {
                toReplace = toReplace.GetPreviousToken(); //Never append *trailing* trivia to the end of file token
            }
            target = target.ReplaceToken(toReplace, toReplace.WithTrailingTrivia(PrependPreservingImportantTrivia(convertedTrivia, toReplace.TrailingTrivia)));
            return target;
        }

        private SyntaxNode ConvertLeadingForSourceLine(SyntaxNode target, int sourceLineIndex)
        {
            var sourceLine = sourceLines[sourceLineIndex];
            var startOfSourceLine = FindNonZeroWidthToken(source, sourceLine.Start);
            if (startOfSourceLine.LeadingTrivia.Any(t => !t.IsWhitespaceOrEndOfLine())) {
                var line = GetBestLine(targetLeadingTextLineFromSourceLine, sourceLineIndex);
                if (line != default) {
                    foreach (var triviaList in leadingTriviaCarriedOver) {
                        target = PrependLeadingTrivia(target, line, triviaList);
                    }
                    leadingTriviaCarriedOver.Clear();
                    target = PrependLeadingTrivia(target, line, startOfSourceLine.LeadingTrivia);
                } else if (startOfSourceLine.LeadingTrivia.Span.End < sourceLine.End) {
                    leadingTriviaCarriedOver.Add(startOfSourceLine.LeadingTrivia);
                }
            }

            return target;
        }

        private static SyntaxNode PrependLeadingTrivia(SyntaxNode target, TextLine targetLine, SyntaxTriviaList leadingTrivia)
        {
            var convertedTrivia = leadingTrivia.ConvertTrivia();
            var toReplace = FindNonZeroWidthToken(target, targetLine.Start);
            if (toReplace.Span.End < targetLine.Start) {
                toReplace = toReplace.GetNextToken(); //TODO: Find out why FindToken is off by one from what I want sometimes, is there a better alternative?
            }
            target = target.ReplaceToken(toReplace, toReplace.WithLeadingTrivia(PrependPreservingImportantTrivia(convertedTrivia, toReplace.LeadingTrivia)));
            return target;
        }

        private TextLine GetBestLine(IReadOnlyDictionary<int, TextLine> sourceToTargetLine, int sourceLineIndex)
        {
            if (sourceToTargetLine.TryGetValue(sourceLineIndex, out var targetLineIndex)) return targetLineIndex;
            return default;
        }

        private static IEnumerable<SyntaxTrivia> PrependPreservingImportantTrivia(IEnumerable<SyntaxTrivia> convertedTrivia, IEnumerable<SyntaxTrivia> existingTrivia)
        {
            if (existingTrivia.Any(t => !t.IsWhitespaceOrEndOfLine())) {
                return convertedTrivia.Concat(existingTrivia);
            }
            return convertedTrivia;
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