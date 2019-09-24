using System;
using System.Text;
using Xunit;

namespace CodeConverter.Tests.TestRunners
{
    public class OurAssert
    {
        public static StringBuilder DescribeStringDiff(string expectedConversion, string actualConversion)
        {
            int l = Math.Max(expectedConversion.Length, actualConversion.Length);
            StringBuilder sb = new StringBuilder(l * 4);
            sb.AppendLine("expected:");
            sb.AppendLine(expectedConversion);
            sb.AppendLine("got:");
            sb.AppendLine(actualConversion);
            sb.AppendLine("diff:");
            for (int i = 0; i < l; i++)
            {
                if (i >= expectedConversion.Length || i >= actualConversion.Length ||
                    expectedConversion[i] != actualConversion[i])
                    sb.Append('x');
                else
                    sb.Append(expectedConversion[i]);
            }

            return sb;
        }

        public static void StringsEqualIgnoringNewlines(string expectedText, string actualText)
        {
            expectedText = Utils.HomogenizeEol(expectedText);
            actualText = Utils.HomogenizeEol(actualText);
            if (expectedText.Equals(actualText)) return;
            Assert.True(false, DescribeStringDiff(expectedText, actualText).ToString());
        }

        public static void StringsEqualIgnoringNewlines(string expectedText, string actualText, Func<string> getMessage)
        {
            expectedText = Utils.HomogenizeEol(expectedText);
            actualText = Utils.HomogenizeEol(actualText);
            if (expectedText.Equals(actualText)) return;
            Assert.True(false, getMessage());
        }
    }
}