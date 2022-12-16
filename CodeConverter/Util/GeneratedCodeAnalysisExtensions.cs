using System.Text.RegularExpressions;

namespace ICSharpCode.CodeConverter.Util;

public static class GeneratedCodeAnalysisExtensions
{
    /// <summary>
    /// Copied from https://github.com/code-cracker/code-cracker/blob/330f7ad217d6aae17a07a0675f52bcaa9a03d956/src/Common/CodeCracker.Common/Extensions/GeneratedCodeAnalysisExtensions.cs
    /// Apache-2.0 license
    /// </summary>
    public static bool IsGeneratedFile(this string filePath) =>
        Regex.IsMatch(filePath, @"(\\service|\\TemporaryGeneratedFile_.*|\\assemblyinfo|\\assemblyattributes|\.(g\.i|g|designer|generated|assemblyattributes))\.(cs|vb)$",
            RegexOptions.IgnoreCase);

    public static bool IsTempFile(this string filePath) =>
        Regex.IsMatch(filePath, @"(\\service|\\obj|\\TemporaryGeneratedFile_.*|\\assemblyattributes|\.(g\.i|g|assemblyattributes))\.(cs|vb)$",
            RegexOptions.IgnoreCase);
}