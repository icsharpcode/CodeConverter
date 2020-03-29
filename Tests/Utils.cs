using System.Text;
using ICSharpCode.CodeConverter.Util;

namespace ICSharpCode.CodeConverter.Tests
{
    internal static class Utils
    {
        internal static string HomogenizeEol(string str)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < str.Length; i++) {
                var ch = str[i];
                var possibleNewline = NewLine.GetDelimiterLength(ch, i + 1 < str.Length ? str[i + 1] : '\0');
                if (possibleNewline > 0) {
                    sb.AppendLine();
                    if (possibleNewline == 2)
                        i++;
                } else {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }
    }
}
