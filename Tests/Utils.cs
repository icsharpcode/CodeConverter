using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.CodeConverter.Util;

namespace ICSharpCode.CodeConverter.Tests;

internal static class Utils
{
    private static readonly string FileSystemRoot = Path.GetPathRoot(Path.GetTempPath());

    /// <summary>
    /// Builds an absolute path that is valid on the current OS (e.g. C:\a\b on Windows, /a/b on Linux) for fake files that needn't exist.
    /// Segments may use either separator.
    /// </summary>
    internal static string RootedPath(params string[] segments)
    {
        var normalized = segments.Select(s => s.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar));
        return Path.Combine(new[] { FileSystemRoot }.Concat(normalized).ToArray());
    }

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