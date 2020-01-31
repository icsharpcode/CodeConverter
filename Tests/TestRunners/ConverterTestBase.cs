using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.VB;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Xunit;
using Xunit.Sdk;

namespace CodeConverter.Tests.TestRunners
{
    public class ConverterTestBase
    {
        private const string AutoTestCommentPrefix = " SourceLine:";
        private static readonly bool RecharacterizeByWritingExpectedOverActual = TestConstants.RecharacterizeByWritingExpectedOverActual;

        private bool _testCstoVBCommentsByDefault = true;
        private readonly string _rootNamespace;

        protected TextConversionOptions EmptyNamespaceOptionStrictOff { get; set; }

        public ConverterTestBase(string rootNamespace = null)
        {
            _rootNamespace = rootNamespace;
            EmptyNamespaceOptionStrictOff = new TextConversionOptions(DefaultReferences.NetStandard2) {
                RootNamespaceOverride = string.Empty, TargetCompilationOptionsOverride = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOptionExplicit(true)
                .WithOptionCompareText(false)
                .WithOptionStrict(OptionStrict.Off)
                .WithOptionInfer(true)
            };
        }

        protected static async Task<string> GetConvertedCodeOrErrorString<TLanguageConversion>(string toConvert, TextConversionOptions conversionOptions = default) where TLanguageConversion : ILanguageConversion, new()
        {
            var conversionResult = await ProjectConversion.ConvertText<TLanguageConversion>(toConvert, conversionOptions ?? new TextConversionOptions(DefaultReferences.NetStandard2));
            var convertedCode = conversionResult.ConvertedCode ?? conversionResult.GetExceptionsAsString();
            return convertedCode;
        }

        public async Task TestConversionCSharpToVisualBasic(string csharpCode, string expectedVisualBasicCode, bool expectSurroundingMethodBlock = false, bool expectCompilationErrors = false, TextConversionOptions conversion = null, bool hasLineCommentConversionIssue = false)
        {
            expectedVisualBasicCode = AddSurroundingMethodBlock(expectedVisualBasicCode, expectSurroundingMethodBlock);

            await TestConversionCSharpToVisualBasicWithoutComments(csharpCode, expectedVisualBasicCode, conversion);
            if (_testCstoVBCommentsByDefault && !hasLineCommentConversionIssue) {
                await AssertLineCommentsConvertedInSameOrder(csharpCode, conversion, "//", LineCanHaveCSharpComment);
            }
        }

        private static bool LineCanHaveCSharpComment(string l)
        {
            return !l.TrimStart().StartsWith("#region");
        }

        /// <summary>
        /// Lines that already have cooments aren't automatically tested, so if a line changes order in a conversion, just add a comment to that line.
        /// If there's a comment conversion issue, set the optional hasLineCommentConversionIssue to true
        /// </summary>
        private async Task AssertLineCommentsConvertedInSameOrder(string source, TextConversionOptions conversion, string singleLineCommentStart, Func<string, bool> lineCanHaveComment)
        {
            var (sourceLinesWithComments, lineNumbersAdded) = AddLineNumberComments(source, singleLineCommentStart, AutoTestCommentPrefix, lineCanHaveComment);
            string sourceWithComments = string.Join(Environment.NewLine, sourceLinesWithComments);
            var convertedCode = await Convert<CSToVBConversion>(sourceWithComments, conversion);
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
            return "\r\n\r\nSource:\r\n" + sourceLinesWithComments + "\r\n-------------------\r\nConverted:\r\n" + convertedCode;
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

        private async Task TestConversionCSharpToVisualBasicWithoutComments(string csharpCode, string expectedVisualBasicCode, TextConversionOptions conversionOptions = null)
        {
            await AssertConvertedCodeResultEquals<CSToVBConversion>(csharpCode, expectedVisualBasicCode, conversionOptions);
        }

        /// <summary>
        /// <paramref name="missingSemanticInfo"/> is currently unused but acts as documentation, and in future will be used to decide whether to check if the input/output compiles
        /// </summary>
        public async Task TestConversionVisualBasicToCSharp(string visualBasicCode, string expectedCsharpCode, bool expectSurroundingBlock = false, bool missingSemanticInfo = false, bool hasLineCommentConversionIssue = false)
        {
            if (expectSurroundingBlock) expectedCsharpCode = SurroundWithBlock(expectedCsharpCode);
            await TestConversionVisualBasicToCSharpWithoutComments(visualBasicCode, expectedCsharpCode);
            if (!hasLineCommentConversionIssue) await TestConversionVisualBasicToCSharpWithoutComments(AddLineNumberComments(visualBasicCode, "' ", false), AddLineNumberComments(expectedCsharpCode, "// ", true));
        }

        private static string SurroundWithBlock(string expectedCsharpCode)
        {
            var indentedStatements = expectedCsharpCode.Replace("\n", "\n    ");
            return $"{{\r\n    {indentedStatements}\r\n}}";
        }

        public async Task TestConversionVisualBasicToCSharpWithoutComments(string visualBasicCode, string expectedCsharpCode)
        {
            await AssertConvertedCodeResultEquals<VBToCSConversion>(visualBasicCode, expectedCsharpCode);
        }

        private async Task AssertConvertedCodeResultEquals<TLanguageConversion>(string inputCode, string expectedConvertedCode, TextConversionOptions conversionOptions = default) where TLanguageConversion : ILanguageConversion, new()
        {
            string convertedTextFollowedByExceptions = await Convert<TLanguageConversion>(inputCode, conversionOptions);
            AssertConvertedCodeResultEquals(convertedTextFollowedByExceptions, expectedConvertedCode, inputCode);
        }

        private async Task<string> Convert<TLanguageConversion>(string inputCode, TextConversionOptions conversionOptions) where TLanguageConversion : ILanguageConversion, new()
        {
            var textConversionOptions = conversionOptions ?? new TextConversionOptions(DefaultReferences.NetStandard2) { RootNamespaceOverride = _rootNamespace };
            var conversionResult = await ProjectConversion.ConvertText<TLanguageConversion>(inputCode, textConversionOptions);
            return (conversionResult.ConvertedCode ?? "") + (conversionResult.GetExceptionsAsString() ?? "");
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
                sb.AppendLine();
                sb.AppendLine("source:");
                sb.AppendLine(originalSource);
                if (RecharacterizeByWritingExpectedOverActual) TestFileRewriter.UpdateFiles(expectedConversion, actualConversion);
                return sb.ToString();
            });
            Assert.False(RecharacterizeByWritingExpectedOverActual, $"Test setup issue: Set {nameof(RecharacterizeByWritingExpectedOverActual)} to false after using it");
        }



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

        private static string AddLineNumberComments(string code, string singleLineCommentStart, bool isTarget)
        {
            int skipped = 0;
            var lines = Utils.HomogenizeEol(code).Split(new[]{Environment.NewLine}, StringSplitOptions.None);
            bool started = false;

            var newLines = lines.Select((s, i) => {
                var prevLine = i > 0 ? lines[i - 1] : "";
                var nextLine = i < lines.Length - 1 ? lines[i + 1] : "";

                //Don't start until first line mentioning class
                started |= s.IndexOf("class ", StringComparison.InvariantCultureIgnoreCase) + s.IndexOf("module ", StringComparison.InvariantCultureIgnoreCase) > -2;

                //Lines which don't map directly should be tested independently
                if (!started ||
                isTarget && HasNoSourceLine(prevLine, s, nextLine)
                || !isTarget && HasNoTargetLine(prevLine, s, nextLine)) {
                    skipped++;
                    return s;
                }

                //Try to indent based on next line
                if (s.Trim() == "" && i > 0) {
                    s = s + new string(Enumerable.Repeat(' ', lines[i - 1].Length).ToArray());
                }

                return s + singleLineCommentStart + (i - skipped).ToString();
            });
            return string.Join(Environment.NewLine, newLines);
        }

        private static bool HasNoSourceLine(string prevLine, string line, string nextLine)
        {
            return line.Trim() == "{"
                   || nextLine.Contains("where T")
                   || IsTwoLineCsIfStatement(line, nextLine)
                   || line.TrimStart().StartsWith("//")
                   || line.Contains("DllImport")
                   || line.Contains("arg") && nextLine.Contains("ref ")
                   || line.TrimStart().StartsWith("if") && nextLine.Trim() == "{";
        }

        /// <summary>
        /// Comes from a one line if statement in VB
        /// </summary>
        private static bool IsTwoLineCsIfStatement(string line, string nextLine)
        {
            return line.Contains("if ") && !nextLine.Trim().Equals("{");
        }

        private static bool HasNoTargetLine(string prevLine, string line, string nextLine)
        {
            return IsVbInheritsOrImplements(nextLine)
                || IsFirstOfMultiLineVbIfStatement(line)
                || line.Contains("<Extension") || line.Contains("CompilerServices.Extension")
                || line.TrimStart().StartsWith("'")
                //Allow a blank line in VB after these statements that doesn't appear in the C# since C# has braces to act as a separator
                || string.IsNullOrWhiteSpace(line) && IsVbInheritsOrImplements(prevLine);
        }

        private static bool IsFirstOfMultiLineVbIfStatement(string line)
        {
            return line.Trim().StartsWith("If ") && line.Trim().EndsWith("Then");
        }

        private static bool IsVbInheritsOrImplements(string line)
        {
            return line.Contains("Inherits") || line.Contains("Implements");
        }

        public static void Fail(string message) => throw new XunitException(message);
    }

    public class ConversionOptions
    {
    }
}
