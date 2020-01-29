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

            //// Special case: The end of file doesn't have trailing trivia, there's a special token which has leading trivia instead
            //target = target.ReplaceToken(target.EndOfFileToken, target.EndOfFileToken.WithLeadingTrivia(target.EndOfFileToken.LeadingTrivia.Concat(source.EndOfFileToken.LeadingTrivia.ConvertTrivia())));

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
            //TODO Keep track of lost comments and put them in a comment at the end of the file
            //TODO Possible perf: Find token starting from position of last replaced token rather than from the root node each time?
            var triviaMappings = new List<TriviaMapping>();
            for (int i = sourceLines.Count - 1; i >= 0; i--) {
                target = ConvertTrailingForSourceLine(target, i);
                target = ConvertLeadingForSourceLine(target, i);
            }
            triviaMappings = triviaMappings.OrderBy(x => x.TargetLine).ThenBy(x => !x.IsLeading).ToList();
            return target;
        }

        private SyntaxNode ConvertLeadingForSourceLine(SyntaxNode target, int sourceLineIndex)
        {
            var startOfSourceLine = source.FindToken(sourceLines[sourceLineIndex].Start);
            if (startOfSourceLine.LeadingTrivia.Any(t => !t.IsWhitespaceOrEndOfLine())) {
                var line = GetBestLine(targetLeadingTextLineFromSourceLine, sourceLineIndex);
                if (line != default) {
                    var convertedTrivia = startOfSourceLine.LeadingTrivia.ConvertTrivia();
                    var toReplace = target.FindToken(line.Start);
                    //TODO Check whether there's a general version of FindToken that covers this and similar comments
                    if (toReplace.Span.End < line.Start) {
                        toReplace = toReplace.GetNextToken(); //Zero width tokens with newline trivia can cause this, e.g. EOF
                    }
                    target = target.ReplaceToken(toReplace, toReplace.WithLeadingTrivia(convertedTrivia));
                }
            }

            return target;
        }

        private SyntaxNode ConvertTrailingForSourceLine(SyntaxNode target, int sourceLineIndex)
        {
            var endOfSourceLine = source.FindToken(sourceLines[sourceLineIndex].End);

            //TODO Check whether there's a general version of FindToken that covers this and similar comments
            if (endOfSourceLine.Width() == 0 && !endOfSourceLine.HasTrailingTrivia && !endOfSourceLine.HasLeadingTrivia) {
                endOfSourceLine = endOfSourceLine.GetPreviousToken();
            }

            if (endOfSourceLine.TrailingTrivia.Any(t => !t.IsWhitespaceOrEndOfLine())) {
                var line = GetBestLine(targetTrailingTextLineFromSourceLine, sourceLineIndex);
                if (line != default) {
                    var convertedTrivia = endOfSourceLine.TrailingTrivia.ConvertTrivia();
                    var toReplace = target.FindToken(line.End);
                    //TODO Check whether there's a general version of FindToken that covers this and similar comments
                    if (toReplace.Span.Start > line.End) {
                        toReplace = toReplace.GetPreviousToken(); //Zero width tokens with newline trivia can cause this, e.g. EOF
                    }
                    target = target.ReplaceToken(toReplace, toReplace.WithTrailingTrivia(convertedTrivia));
                }
            }

            return target;
        }

        private TextLine GetBestLine(IReadOnlyDictionary<int, TextLine> sourceToTargetLine, int sourceLineIndex)
        {
            if (sourceToTargetLine.TryGetValue(sourceLineIndex, out var targetLineIndex)) return targetLineIndex;
            return default;
        }
    }
}