using System.IO.Abstractions;
using System.Text.RegularExpressions;

namespace ICSharpCode.CodeConverter.Common;

public class TextReplacementConverter
{
    private readonly IFileSystem _fileSystem;

    public TextReplacementConverter(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public ConversionResult ConversionResultFromReplacements(FileInfo filePath, IEnumerable<(string Find, string Replace, bool FirstOnly)> replacements,
        Func<string, string> postReplacementTransform = null)
    {
        postReplacementTransform ??= (s => s);
        var oldProjectText = _fileSystem.File.ReadAllText(filePath.FullName);
        var newProjectText = Replace(oldProjectText, replacements);
        string withReplacements = postReplacementTransform(newProjectText);
        return new ConversionResult(withReplacements) { SourcePathOrNull = filePath.FullName, IsIdentity = oldProjectText == withReplacements};
    }

    public static string Replace(string originalText, IEnumerable<(string Find, string Replace, bool FirstOnly)> replacements)
    {
        foreach (var (oldValue, newValue, firstOnly) in replacements) {
            Regex regex = new Regex(oldValue, RegexOptions.IgnoreCase);
            originalText = regex.Replace(originalText, newValue, firstOnly ? 1 : -1);
        }

        return originalText;
    }
}