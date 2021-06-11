namespace ICSharpCode.CodeConverter.Shared
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.IO.Abstractions;

    public static class TextReplacementConverter
    {
        public static IFileSystem FileSystem { get; set; } = new FileSystem();

        public static ConversionResult ConversionResultFromReplacements(this FileInfo filePath, IEnumerable<(string Find, string Replace, bool FirstOnly)> replacements, Func<string, string> postReplacementTransform = null)
        {
            postReplacementTransform ??= (s => s);
            var oldProjectText = FileSystem.File.ReadAllText(filePath.FullName);
            var newProjectText = oldProjectText.Replace(replacements);
            string withReplacements = postReplacementTransform(newProjectText);
            return new ConversionResult(withReplacements) { SourcePathOrNull = filePath.FullName, IsIdentity = oldProjectText == withReplacements};
        }

        public static string Replace(this string originalText, IEnumerable<(string Find, string Replace, bool FirstOnly)> replacements)
        {
            foreach (var (oldValue, newValue, firstOnly) in replacements) {
                Regex regex = new Regex(oldValue, RegexOptions.IgnoreCase);
                originalText = regex.Replace(originalText, newValue, firstOnly ? 1 : -1);
            }

            return originalText;
        }
    }
}