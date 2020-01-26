using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using CS = Microsoft.CodeAnalysis.CSharp;
using CSS = Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.VB
{
    internal class CSharpConverter
    {
        public static async Task<SyntaxNode> ConvertCompilationTree(Document document,
            VisualBasicCompilation vbViewOfCsSymbols, Project vbReferenceProject)
        {
            document = await document.WithExpandedRootAsync();
            var compilation = await document.Project.GetCompilationAsync();
            var tree = await document.GetSyntaxTreeAsync();
            var semanticModel = compilation.GetSemanticModel(tree, true);
            var root = await document.GetSyntaxRootAsync() as CS.CSharpSyntaxNode ??
                       throw new InvalidOperationException(NullRootError(document));

            var vbSyntaxGenerator = SyntaxGenerator.GetGenerator(vbReferenceProject);
            var numberOfLines = tree.GetLineSpan(root.FullSpan).EndLinePosition.Line;

            var visualBasicSyntaxVisitor = new NodesVisitor(document, (CS.CSharpCompilation)compilation, semanticModel, vbViewOfCsSymbols, vbSyntaxGenerator, numberOfLines);
            var converted = root.Accept(visualBasicSyntaxVisitor.TriviaConvertingVisitor);
            var formattedConverted = Formatter.Format(converted, document.Project.Solution.Workspace);
            return WithLineTrivia(root, formattedConverted);
        }

        private static string NullRootError(Document document)
        {
            var initial = document.Project.Language != LanguageNames.CSharp
                ? "Document cannot be converted because it's not within a C# project."
                : "Could not find valid C# within document.";
            return initial + " For best results, convert a c# document from within a C# project which compiles successfully.";
        }

        /// <summary>
        /// For each source line:
        /// * Add leading trivia to the start of the first target line containing a node converted from that source line
        /// * Add trailing trivia to the end of the last target line containing a node converted from that source line
        /// Makes no attempt to convert whitespace/newline-only trivia
        /// Currently doesn't deal with any within-line trivia (i.e. /* block comments */)
        /// </summary>
        private static SyntaxNode WithLineTrivia(SyntaxNode source, SyntaxNode target)
        {
            var sourceLines = source.GetText().Lines;
            var originalTargetLines = target.GetText().Lines;
            var targetNodesBySourceLine = target.GetAnnotatedNodesAndTokens(AnnotationConstants.WithinOriginalLineAnnotationKind).ToLookup(n => n.GetAnnotations(AnnotationConstants.WithinOriginalLineAnnotationKind).Select(a => int.Parse(a.Data)).Min());
            //TODO Try harder to avoid losing track of various precalculated positions changing during the replacements, for example build up a dictionary of replacements and make them in a single ReplaceTokens call

            for (int i = sourceLines.Count - 1; i >= 0; i--) {
                var sourceLine = sourceLines[i];
                var endOfSourceLine = source.FindToken(sourceLine.End);
                var startOfSourceLine = source.FindTokenOnRightOfPosition(sourceLine.Start);

                if (endOfSourceLine.TrailingTrivia.Concat(startOfSourceLine.LeadingTrivia).All(t => t.IsWhitespaceOrEndOfLine())) continue;

                var convertedTrailingTrivia = endOfSourceLine.TrailingTrivia.ConvertTrivia();
                var convertedLeadingTrivia = startOfSourceLine.LeadingTrivia.ConvertTrivia();

                var (leadingLine, trailingLine) = GetBestLeadingAndTrailingLine(originalTargetLines, targetNodesBySourceLine, i);
                if (leadingLine == default || trailingLine == default) continue;

                var last = target.FindToken(trailingLine.End);
                target = target.ReplaceToken(last, last.WithTrailingTrivia(convertedTrailingTrivia));

                var first = target.FindTokenOnRightOfPosition(leadingLine.Start);
                target = target.ReplaceToken(first, first.WithLeadingTrivia(convertedLeadingTrivia));
            }
            return target;
        }

        private static (TextLine leadingLine, TextLine trailingLine) GetBestLeadingAndTrailingLine(Microsoft.CodeAnalysis.Text.TextLineCollection originalTargetLines, ILookup<int, SyntaxNodeOrToken> targetNodesBySourceLine, int sourceLineIndex)
        {
            var targetNodeGroup = targetNodesBySourceLine[sourceLineIndex];
            if (targetNodeGroup.Any()) return GetExactLeadingLineAndTrailingLine(originalTargetLines, targetNodeGroup);

            var (previousOffset, previousSourceLineTargetNodes) = GetOffsetSourceLineTargetNodes(targetNodesBySourceLine, sourceLineIndex, -1);
            var (nextOffset, nextSourceLineTargetNodes) = GetOffsetSourceLineTargetNodes(targetNodesBySourceLine, sourceLineIndex, 1);
            if (previousSourceLineTargetNodes.Any() && nextSourceLineTargetNodes.Any()) {
                var (previousleading, previousTrailing) = GetExactLeadingLineAndTrailingLine(originalTargetLines, previousSourceLineTargetNodes);
                var (nextLeading, nextTrailing) = GetExactLeadingLineAndTrailingLine(originalTargetLines, nextSourceLineTargetNodes);
                var guessedTargetLine = originalTargetLines[(previousTrailing.LineNumber + previousOffset + nextLeading.LineNumber - nextOffset) / 2];
                //TODO Annotate this case with a comment to say it's a guess
                //TODO Move this guessing phase to fill in the gaps after all other allocations are made to avoid clashes with other moved lines
                if (previousTrailing.LineNumber < guessedTargetLine.LineNumber && guessedTargetLine.LineNumber < nextLeading.LineNumber) return (guessedTargetLine, guessedTargetLine);
            }
            return (default, default);
        }

        private static (int offset, IEnumerable<SyntaxNodeOrToken> targetNodes) GetOffsetSourceLineTargetNodes(ILookup<int, SyntaxNodeOrToken> targetNodesBySourceLine, int sourceLineIndex, int multiplier)
        {
            for (int offset = 1; offset <= 5; offset++) {
                var thisLine = targetNodesBySourceLine[sourceLineIndex + (offset * multiplier)];
                if (thisLine.Any()) return (offset, thisLine);
            }
            return (0, Enumerable.Empty<SyntaxNodeOrToken>());
        }

        private static (TextLine leadingLine, TextLine trailingLine) GetExactLeadingLineAndTrailingLine(TextLineCollection originalTargetLines, IEnumerable<SyntaxNodeOrToken> targetNodeGroup)
        {
            var trailingTriviaAfterPosition = targetNodeGroup.Max(x => x.GetLocation().SourceSpan.End);
            var leadTriviaBeforePosition = targetNodeGroup.Min(x => x.GetLocation().SourceSpan.Start);

            var trailingLine = originalTargetLines.GetLineFromPosition(trailingTriviaAfterPosition);
            var leadingLine = originalTargetLines.GetLineFromPosition(leadTriviaBeforePosition);
            return (leadingLine, trailingLine);
        }
    }
}
