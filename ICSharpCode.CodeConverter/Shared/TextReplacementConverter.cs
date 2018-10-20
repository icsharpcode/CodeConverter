using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ICSharpCode.CodeConverter.Shared
{
    internal static class TextReplacementConverter
    {
        public static ConversionResult ConversionResultFromReplacements(this FileInfo filePath, IEnumerable<(string, string)> replacements, Func<string, string> postReplacementTransform = null)
        {
            postReplacementTransform = postReplacementTransform ?? (s => s);
            var newProjectText = File.ReadAllText(filePath.FullName);
            newProjectText = newProjectText.Replace(replacements);
            return new ConversionResult(postReplacementTransform(newProjectText)) {SourcePathOrNull = filePath.FullName};
        }

        public static string Replace(this string originalText, IEnumerable<(string, string)> replacements)
        {
            foreach (var (oldValue, newValue) in replacements)
            {
                originalText = Regex.Replace(originalText, oldValue, newValue, RegexOptions.IgnoreCase);
            }

            return originalText;
        }
    }
}