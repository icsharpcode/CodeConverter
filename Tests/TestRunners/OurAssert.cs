using System;
using System.Text;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.TestRunners
{
    public static class OurAssert
    {
        public const string LineSplitter = "\r\n\r\n------------------------------------\r\n\r\n";

        public static StringBuilder DescribeStringDiff(string expectedConversion, string actualConversion)
        {
            int l = Math.Max(expectedConversion.Length, actualConversion.Length);
            StringBuilder sb = new StringBuilder(l);
            sb.AppendLine(LineSplitter + "diff:");
            for (int i = 0; i < l; i++)
            {
                if (i >= expectedConversion.Length || i >= actualConversion.Length ||
                    expectedConversion[i] != actualConversion[i])
                    sb.Append('x');
                else
                    sb.Append(expectedConversion[i]);
            }

            return sb.AppendLine(LineSplitter);
        }

        public static void EqualIgnoringNewlines(string expectedText, string actualText)
        {
            EqualIgnoringNewlines(expectedText + LineSplitter, actualText + LineSplitter, () => DescribeStringDiff(expectedText, actualText).ToString());
        }

        public static void EqualIgnoringNewlines(string expectedText, string actualText, Func<string> getMessage)
        {
            expectedText = Utils.HomogenizeEol(expectedText);
            actualText = Utils.HomogenizeEol(actualText);
            Equal(expectedText, actualText, getMessage);
        }

        public static void Equal(object expectedText, object actualText, Func<string> getMessage)
        {
            WithMessage(() => Assert.Equal(expectedText, actualText), getMessage);
        }

        public static void WithMessage(Action assertion, Func<string> getMessage)
        {
            try {
                assertion();
            } catch (Exception e) {
                throw new Exception(e.Message + "\r\n" + getMessage(), e);
            }
        }
    }
}
