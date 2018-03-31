using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using ICSharpCode.CodeConverter.VB;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Xunit;

namespace CodeConverter.Tests
{
    public class ConverterTestBase
    {
        private bool _testCstoVBCommentsByDefault = false;

        public void TestConversionCSharpToVisualBasic(string csharpCode, string expectedVisualBasicCode, bool expectSurroundingMethodBlock = false, CSharpParseOptions csharpOptions = null, VisualBasicParseOptions vbOptions = null)
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

        private static void TestConversionCSharpToVisualBasicWithoutComments(string csharpCode, string expectedVisualBasicCode)
        {
            var outputNode = ProjectConversion<CSToVBConversion>.ConvertText(csharpCode, DiagnosticTestBase.DefaultMetadataReferences);

            var txt = outputNode.ConvertedCode ?? outputNode.GetExceptionsAsString();
            txt = Utils.HomogenizeEol(txt).TrimEnd();
            expectedVisualBasicCode = Utils.HomogenizeEol(expectedVisualBasicCode).TrimEnd();
            AssertCodeEqual(csharpCode, expectedVisualBasicCode, txt);
        }

        public void TestConversionVisualBasicToCSharp(string visualBasicCode, string expectedCsharpCode, bool expectUsings = true, bool expectSurroundingBlock = false)
        {
            if (expectUsings) expectedCsharpCode = AddCSUsings(expectedCsharpCode, expectSurroundingBlock);
            TestConversionVisualBasicToCSharpWithoutComments(visualBasicCode, expectedCsharpCode, false);
            TestConversionVisualBasicToCSharpWithoutComments(AddLineNumberComments(visualBasicCode, "' ", false), AddLineNumberComments(expectedCsharpCode, "// ", true), false);
        }

        private static string AddCSUsings(string expectedCsharpCode, bool standaloneStatements)
        {
            if (standaloneStatements)
            {
                var indentedStatements = expectedCsharpCode.Replace("\n", "\n    ");
                expectedCsharpCode =
                    $@"{{
    {indentedStatements}
}}";
            }
            else if (!expectedCsharpCode.StartsWith("using System;")) {
                var possibleNewline = expectedCsharpCode.StartsWith("using ") ? "" : Environment.NewLine;

                expectedCsharpCode =
                    @"using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;
" + possibleNewline + expectedCsharpCode;
            }

            return expectedCsharpCode;
        }

        public void TestConversionVisualBasicToCSharpWithoutComments(string visualBasicCode, string expectedCsharpCode, bool addUsings = true, CSharpParseOptions csharpOptions = null, VisualBasicParseOptions vbOptions = null)
        {
            if (addUsings) expectedCsharpCode = AddCSUsings(expectedCsharpCode, false);
            var outputNode = ProjectConversion<VBToCSConversion>.ConvertText(visualBasicCode, DiagnosticTestBase.DefaultMetadataReferences);
            var txt = Utils.HomogenizeEol(outputNode.ConvertedCode ?? outputNode.GetExceptionsAsString()).TrimEnd();
            expectedCsharpCode = Utils.HomogenizeEol(expectedCsharpCode).TrimEnd();
            AssertCodeEqual(visualBasicCode, expectedCsharpCode, txt);
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
    }
}
