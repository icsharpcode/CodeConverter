using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ICSharpCode.CodeConverter.Shared
{
    internal static class TextReplacementConverter
    {
        public static ConversionResult ConversionResultFromReplacements(this FileInfo filePath, IEnumerable<(string Find, string Replace, bool FirstOnly)> replacements, Func<string, string> postReplacementTransform = null)
        {
            postReplacementTransform ??= (s => s);
            var newProjectText = File.ReadAllText(filePath.FullName);
            newProjectText = newProjectText.Replace(replacements);
            string withReplacements = postReplacementTransform(newProjectText);
            return new ConversionResult(withReplacements) { SourcePathOrNull = filePath.FullName, IsIdentity = newProjectText == withReplacements};
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