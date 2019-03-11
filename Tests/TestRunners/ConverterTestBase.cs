using System;
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

        public void TestConversionCSharpToVisualBasic(string csharpCode, string expectedVisualBasicCode, bool expectSurroundingMethodBlock = false, bool expectCompilationErrors = false)
        {
            expectedVisualBasicCode = AddSurroundingMethodBlock(expectedVisualBasicCode, expectSurroundingMethodBlock);

            TestConversionCSharpToVisualBasicWithoutComments(csharpCode, expectedVisualBasicCode);
            if (_testCstoVBCommentsByDefault) TestConversionCSharpToVisualBasicWithoutComments(AddLineNumberComments(csharpCode, "// ", false), AddLineNumberComments(expectedVisualBasicCode, "' ", true));
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

        private void TestConversionCSharpToVisualBasicWithoutComments(string csharpCode, string expectedVisualBasicCode)
        {
            AssertConvertedCodeResultEquals<CSToVBConversion>(csharpCode, expectedVisualBasicCode).GetAwaiter().GetResult();
        }

        public void TestConversionVisualBasicToCSharp(string visualBasicCode, string expectedCsharpCode, bool expectSurroundingBlock = false)
        {
            if (expectSurroundingBlock) expectedCsharpCode = SurroundWithBlock(expectedCsharpCode);
            TestConversionVisualBasicToCSharpWithoutComments(visualBasicCode, expectedCsharpCode);
            TestConversionVisualBasicToCSharpWithoutComments(AddLineNumberComments(visualBasicCode, "' ", false), AddLineNumberComments(expectedCsharpCode, "// ", true));
        }

        private static string SurroundWithBlock(string expectedCsharpCode)
        {
            var indentedStatements = expectedCsharpCode.Replace("\n", "\n    ");
            return $"{{\r\n    {indentedStatements}\r\n}}";
        }

        public void TestConversionVisualBasicToCSharpWithoutComments(string visualBasicCode, string expectedCsharpCode)
        {
            AssertConvertedCodeResultEquals<VBToCSConversion>(visualBasicCode, expectedCsharpCode).GetAwaiter().GetResult();
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
            var txt = Utils.HomogenizeEol(convertedTextFollowedByExceptions).TrimEnd();
            expectedConversionResultText = Utils.HomogenizeEol(expectedConversionResultText).TrimEnd();
            AssertCodeEqual(originalSource, expectedConversionResultText, txt);
        }

        private static void AssertCodeEqual(string originalSource, string expectedConversion, string actualConversion)
        {
            if (expectedConversion != actualConversion) {
                int l = Math.Max(expectedConversion.Length, actualConversion.Length);
                StringBuilder sb = new StringBuilder(l * 4);
                sb.AppendLine("expected:");
                sb.AppendLine(expectedConversion);
                sb.AppendLine("got:");
                sb.AppendLine(actualConversion);
                sb.AppendLine("diff:");
                for (int i = 0; i < l; i++) {
                    if (i >= expectedConversion.Length || i >= actualConversion.Length || expectedConversion[i] != actualConversion[i])
                        sb.Append('x');
                    else
                        sb.Append(expectedConversion[i]);
                }
                sb.AppendLine();
                sb.AppendLine("source:");
                sb.AppendLine(originalSource);
                Assert.True(false, sb.ToString());
            }
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
                   || line.Contains("DllImport");
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
                || line.Contains("End If")
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
