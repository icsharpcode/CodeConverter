using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.VB;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Xunit;
using Xunit.Sdk;

namespace ICSharpCode.CodeConverter.Tests.TestRunners
{
    public class ConverterTestBase
    {
        private const string AutoTestCommentPrefix = " SourceLine:";
        private static readonly bool RecharacterizeByWritingExpectedOverActual = TestConstants.RecharacterizeByWritingExpectedOverActual;

        private bool _testCstoVbCommentsByDefault = true;
        private bool _testVbtoCsCommentsByDefault = true;
        private readonly string _rootNamespace;

        protected TextConversionOptions EmptyNamespaceOptionStrictOff { get; set; }

        public ConverterTestBase(string rootNamespace = null)
        {
            _rootNamespace = rootNamespace;
            var options = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                                .WithOptionExplicit(true)
                                .WithOptionCompareText(false)
                                .WithOptionStrict(OptionStrict.Off)
                                .WithOptionInfer(true);
            EmptyNamespaceOptionStrictOff = new TextConversionOptions(DefaultReferences.NetStandard2) {
                RootNamespaceOverride = string.Empty, TargetCompilationOptionsOverride = options,
                ShowCompilationErrors = true
            };
        }

        public async Task TestConversionCSharpToVisualBasicAsync(string csharpCode, string expectedVisualBasicCode, bool expectSurroundingMethodBlock = false, bool expectCompilationErrors = false, TextConversionOptions conversionOptions = null, bool hasLineCommentConversionIssue = false)
        {
            expectedVisualBasicCode = AddSurroundingMethodBlock(expectedVisualBasicCode, expectSurroundingMethodBlock);

            conversionOptions ??= new TextConversionOptions(DefaultReferences.NetStandard2) { ShowCompilationErrors = !expectSurroundingMethodBlock };
            await AssertConvertedCodeResultEqualsAsync<CSToVBConversion>(csharpCode, expectedVisualBasicCode, conversionOptions);
            if (_testCstoVbCommentsByDefault && !hasLineCommentConversionIssue) {
                await AssertLineCommentsConvertedInSameOrderAsync<CSToVBConversion>(csharpCode, conversionOptions, "//", LineCanHaveCSharpComment);
            }
        }

        private static bool LineCanHaveCSharpComment(string l)
        {
            return !l.TrimStart().StartsWith("#region");
        }

        /// <summary>
        /// Lines that already have comments aren't automatically tested, so if a line changes order in a conversion, just add a comment to that line.
        /// If there's a comment conversion issue, set the optional hasLineCommentConversionIssue to true
        /// </summary>
        private async Task AssertLineCommentsConvertedInSameOrderAsync<TLanguageConversion>(string source, TextConversionOptions conversion, string singleLineCommentStart, Func<string, bool> lineCanHaveComment) where TLanguageConversion : ILanguageConversion, new()
        {
            var (sourceLinesWithComments, lineNumbersAdded) = AddLineNumberComments(source, singleLineCommentStart, AutoTestCommentPrefix, lineCanHaveComment);
            string sourceWithComments = string.Join(Environment.NewLine, sourceLinesWithComments);
            var convertedCode = await ConvertAsync<TLanguageConversion>(sourceWithComments, conversion);
            var convertedCommentLineNumbers = convertedCode.Split(new[] { AutoTestCommentPrefix }, StringSplitOptions.None)
                .Skip(1).Select(afterPrefix => afterPrefix.Split('\n')[0].TrimEnd()).ToList();
            var missingSourceLineNumbers = lineNumbersAdded.Except(convertedCommentLineNumbers);
            if (missingSourceLineNumbers.Any()) {
                Assert.False(true, "Comments not converted from source lines: " + string.Join(", ", missingSourceLineNumbers) + GetSourceAndConverted(sourceWithComments, convertedCode));
            }
            OurAssert.Equal(string.Join(", ", lineNumbersAdded), string.Join(", ", convertedCommentLineNumbers), () => GetSourceAndConverted(sourceWithComments, convertedCode));
        }

        private static string GetSourceAndConverted(string sourceLinesWithComments, string convertedCode)
        {
            return OurAssert.LineSplitter + "Converted:\r\n" + convertedCode + OurAssert.LineSplitter + "Source:\r\n" + sourceLinesWithComments;
        }

        private static string AddSurroundingMethodBlock(string expectedVisualBasicCode, bool expectSurroundingBlock)
        {
            if (expectSurroundingBlock) {
                var indentedStatements = expectedVisualBasicCode.Replace("\n", "\n    ");
                expectedVisualBasicCode =
$@"Private Sub SurroundingSub()
    {indentedStatements}
End Sub";
            }

            return expectedVisualBasicCode;
        }

        /// <summary>
        /// <paramref name="missingSemanticInfo"/> is currently unused but acts as documentation, and in future will be used to decide whether to check if the input/output compiles
        /// </summary>
        public async Task TestConversionVisualBasicToCSharpAsync(string visualBasicCode, string expectedCsharpCode, bool expectSurroundingBlock = false, bool missingSemanticInfo = false, bool hasLineCommentConversionIssue = false)
        {
            if (expectSurroundingBlock) expectedCsharpCode = SurroundWithBlock(expectedCsharpCode);
            var conversionOptions = new TextConversionOptions(DefaultReferences.NetStandard2) { RootNamespaceOverride = _rootNamespace, ShowCompilationErrors = !expectSurroundingBlock };
            await AssertConvertedCodeResultEqualsAsync<VBToCSConversion>(visualBasicCode, expectedCsharpCode, conversionOptions);

            if (_testVbtoCsCommentsByDefault && !hasLineCommentConversionIssue) {
                await AssertLineCommentsConvertedInSameOrderAsync<VBToCSConversion>(visualBasicCode, null, "'", _ => true);
            }
        }

        private static string SurroundWithBlock(string expectedCsharpCode)
        {
            var indentedStatements = expectedCsharpCode.Replace("\n", "\n    ");
            return $"{{\r\n    {indentedStatements}\r\n}}";
        }

        protected async Task<string> ConvertAsync<TLanguageConversion>(string inputCode, TextConversionOptions conversionOptions = default) where TLanguageConversion : ILanguageConversion, new()
        {
            var textConversionOptions = conversionOptions ?? new TextConversionOptions(DefaultReferences.NetStandard2) { RootNamespaceOverride = _rootNamespace, ShowCompilationErrors = true };
            var conversionResult = await ProjectConversion.ConvertTextAsync<TLanguageConversion>(inputCode, textConversionOptions);
            return (conversionResult.ConvertedCode ?? "") + (conversionResult.GetExceptionsAsString() ?? "");
        }

        protected async Task AssertConvertedCodeResultEqualsAsync<TLanguageConversion>(string inputCode, string expectedConvertedCode, TextConversionOptions conversionOptions = default) where TLanguageConversion : ILanguageConversion, new()
        {
            string convertedTextFollowedByExceptions = await ConvertAsync<TLanguageConversion>(inputCode, conversionOptions);
            AssertConvertedCodeResultEquals(convertedTextFollowedByExceptions, expectedConvertedCode, inputCode);
        }

        private static void AssertConvertedCodeResultEquals(string convertedCodeFollowedByExceptions,
            string expectedConversionResultText, string originalSource)
        {
            var txt = convertedCodeFollowedByExceptions.TrimEnd();
            expectedConversionResultText = expectedConversionResultText.TrimEnd();
            AssertCodeEqual(originalSource, expectedConversionResultText, txt);
        }

        private static void AssertCodeEqual(string originalSource, string expectedConversion, string actualConversion)
        {
            OurAssert.EqualIgnoringNewlines(expectedConversion, actualConversion, () =>
            {
                StringBuilder sb = OurAssert.DescribeStringDiff(expectedConversion, actualConversion);
                sb.AppendLine(OurAssert.LineSplitter);
                sb.AppendLine("source:");
                sb.AppendLine(originalSource);
                if (RecharacterizeByWritingExpectedOverActual) TestFileRewriter.UpdateFiles(expectedConversion, actualConversion);
                return sb.ToString();
            });
            Assert.False(RecharacterizeByWritingExpectedOverActual, $"Test setup issue: Set {nameof(RecharacterizeByWritingExpectedOverActual)} to false after using it");
        }


        /// <remarks>Currently puts comments in multi-line comments which then don't get converted</remarks>
        private static (IReadOnlyCollection<string> Lines, IReadOnlyCollection<string> LineNumbersAdded) AddLineNumberComments(string code, string singleLineCommentStart, string commentPrefix, Func<string, bool> lineCanHaveComment)
        {
            var lines = Utils.HomogenizeEol(code).Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            var lineNumbersAdded = new List<string>();
            var newLines = lines.Select((line, i) => {
                var lineNumber = i.ToString();
                var potentialExistingComments = line.Split(new[] { singleLineCommentStart }, StringSplitOptions.None).Skip(1);
                if (potentialExistingComments.Count() == 1 || !lineCanHaveComment(line)) return line;
                lineNumbersAdded.Add(lineNumber);
                return line + singleLineCommentStart + commentPrefix + lineNumber;
            }).ToArray();
            return (newLines, lineNumbersAdded);
        }

        public static void Fail(string message) => throw new XunitException(message);
    }
    public class CSToVBWithoutSimplifierConversion : ILanguageConversion {
        private CSToVBConversion baseConversion;
        public CSToVBWithoutSimplifierConversion() {
            this.baseConversion = new CSToVBConversion();
        }

        string ILanguageConversion.TargetLanguage => baseConversion.TargetLanguage;

        ConversionOptions ILanguageConversion.ConversionOptions { get => baseConversion.ConversionOptions; set => baseConversion.ConversionOptions = value; }

        bool ILanguageConversion.CanBeContainedByMethod(SyntaxNode node) {
            return baseConversion.CanBeContainedByMethod(node);
        }

        async Task<IProjectContentsConverter> ILanguageConversion.CreateProjectContentsConverterAsync(Project project, IProgress<ConversionProgress> progress, CancellationToken cancellationToken) {
            return await baseConversion.CreateProjectContentsConverterAsync(project, progress, cancellationToken);
        }

        async Task<Document> ILanguageConversion.CreateProjectDocumentFromTreeAsync(SyntaxTree tree, IEnumerable<MetadataReference> references)
        {
            return await baseConversion.CreateProjectDocumentFromTreeAsync(tree, references);
        }

        SyntaxTree ILanguageConversion.CreateTree(string text) {
            return baseConversion.CreateTree(text);
        }

        List<SyntaxNode> ILanguageConversion.FindSingleImportantChild(SyntaxNode annotatedNode) {
            return baseConversion.FindSingleImportantChild(annotatedNode);
        }

        IEnumerable<(string, string)> ILanguageConversion.GetProjectFileReplacementRegexes() {
            return baseConversion.GetProjectFileReplacementRegexes();
        }

        IReadOnlyCollection<(string, string)> ILanguageConversion.GetProjectTypeGuidMappings() {
            return baseConversion.GetProjectTypeGuidMappings();
        }

        SyntaxNode ILanguageConversion.GetSurroundedNode(IEnumerable<SyntaxNode> descendantNodes, bool surroundedWithMethod) {
            return baseConversion.GetSurroundedNode(descendantNodes, surroundedWithMethod);
        }

        bool ILanguageConversion.MustBeContainedByClass(SyntaxNode node) {
            return baseConversion.MustBeContainedByClass(node);
        }

        string ILanguageConversion.PostTransformProjectFile(string xml) {
            return baseConversion.PostTransformProjectFile(xml);
        }

        async Task<Document> ILanguageConversion.SingleSecondPassAsync(Document doc) {
            return doc;
        }

        string ILanguageConversion.WithSurroundingClass(string text) {
            return baseConversion.WithSurroundingClass(text);
        }
        string ILanguageConversion.WithSurroundingMethod(string text) {
            return baseConversion.WithSurroundingMethod(text);
        }
    }
}
