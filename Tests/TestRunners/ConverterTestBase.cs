using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.VB;
using Xunit;
using Xunit.Sdk;

namespace CodeConverter.Tests.TestRunners
{
    public class ConverterTestBase
    {
        private static readonly bool RecharacterizeByWritingExpectedOverActual = TestConstants.RecharacterizeByWritingExpectedOverActual;

        private bool _testCstoVBCommentsByDefault = false;
        private readonly string _rootNamespace;

        public ConverterTestBase(string rootNamespace = null)
        {
            _rootNamespace = rootNamespace;
        }

        protected static async Task<string> GetConvertedCodeOrErrorString<TLanguageConversion>(string toConvert) where TLanguageConversion : ILanguageConversion, new()
        {
            var conversionResult = await ProjectConversion.ConvertText<TLanguageConversion>(toConvert, DefaultReferences.NetStandard2);
            var convertedCode = conversionResult.ConvertedCode ?? conversionResult.GetExceptionsAsString();
            return convertedCode;
        }

        public async Task TestConversionCSharpToVisualBasic(string csharpCode, string expectedVisualBasicCode, bool expectSurroundingMethodBlock = false, bool expectCompilationErrors = false)
        {
            expectedVisualBasicCode = AddSurroundingMethodBlock(expectedVisualBasicCode, expectSurroundingMethodBlock);

            await TestConversionCSharpToVisualBasicWithoutComments(csharpCode, expectedVisualBasicCode);
            if (_testCstoVBCommentsByDefault) await TestConversionCSharpToVisualBasicWithoutComments(AddLineNumberComments(csharpCode, "// ", false), AddLineNumberComments(expectedVisualBasicCode, "' ", true));
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

        private async Task TestConversionCSharpToVisualBasicWithoutComments(string csharpCode, string expectedVisualBasicCode)
        {
            await AssertConvertedCodeResultEquals<CSToVBConversion>(csharpCode, expectedVisualBasicCode);
        }

        /// <summary>
        /// <paramref name="missingSemanticInfo"/> is currently unused but acts as documentation, and in future will be used to decide whether to check if the input/output compiles
        /// </summary>
        public async Task TestConversionVisualBasicToCSharp(string visualBasicCode, string expectedCsharpCode, bool expectSurroundingBlock = false, bool missingSemanticInfo = false)
        {
            if (expectSurroundingBlock) expectedCsharpCode = SurroundWithBlock(expectedCsharpCode);
            await TestConversionVisualBasicToCSharpWithoutComments(visualBasicCode, expectedCsharpCode);
            await TestConversionVisualBasicToCSharpWithoutComments(AddLineNumberComments(visualBasicCode, "' ", false), AddLineNumberComments(expectedCsharpCode, "// ", true));
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

        private async Task AssertConvertedCodeResultEquals<TLanguageConversion>(string inputCode, string expectedConvertedCode) where TLanguageConversion : ILanguageConversion, new()
        {
            var outputNode =
                ProjectConversion.ConvertText<TLanguageConversion>(inputCode, DefaultReferences.NetStandard2, _rootNamespace);
            AssertConvertedCodeResultEquals(await outputNode, expectedConvertedCode, inputCode);
        }

        private static void AssertConvertedCodeResultEquals(ConversionResult conversionResult,
            string expectedConversionResultText, string originalSource)
        {
            var convertedTextFollowedByExceptions =
                (conversionResult.ConvertedCode ?? "") + (conversionResult.GetExceptionsAsString() ?? "");
            var txt = convertedTextFollowedByExceptions.TrimEnd();
            expectedConversionResultText = expectedConversionResultText.TrimEnd();
            AssertCodeEqual(originalSource, expectedConversionResultText, txt);
        }

        private static void AssertCodeEqual(string originalSource, string expectedConversion, string actualConversion)
        {
            OurAssert.StringsEqualIgnoringNewlines(expectedConversion, actualConversion, () =>
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
}
