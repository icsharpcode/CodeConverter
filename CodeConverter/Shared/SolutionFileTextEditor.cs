using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ICSharpCode.CodeConverter.Shared
{
    public class SolutionFileTextEditor
    {
        public static IEnumerable<(string Find, string Replace, bool FirstOnly)> GetProjectReferenceReplacements(IEnumerable<(string FilePath, string DirectoryPath)> projectsToConvert, string sourceSolutionContents)
        {
            foreach (var (projFilePath, projDirPath) in projectsToConvert) {
                var projFilename = Path.GetFileName(projFilePath);

                var newProjFilename = PathConverter.TogglePathExtension(projFilename);

                var projPath = PathConverter.GetFileDirPath(projFilename, projDirPath);
                var newProjPath = PathConverter.GetFileDirPath(newProjFilename, projDirPath);

                var projPathEscaped = Regex.Escape(projPath);

                yield return (projPathEscaped, newProjPath, false);
                if (!string.IsNullOrWhiteSpace(sourceSolutionContents) && GetProjectGuidReplacement(projPathEscaped, sourceSolutionContents) is { } replacement) yield return replacement;
            }
        }

        private static (string Find, string Replace, bool FirstOnly)? GetProjectGuidReplacement(string projPath, string contents)
        {
            var projGuidRegex = new Regex(projPath + @""", ""({[0-9A-Fa-f\-]{32,36}})("")");
            var projGuidMatch = projGuidRegex.Match(contents);
            if (!projGuidMatch.Success) return null;

            var oldGuid = projGuidMatch.Groups[1].Value;
            var newGuid = GetDeterministicGuidFrom(new Guid(oldGuid));
            return (oldGuid, newGuid.ToString("B").ToUpperInvariant(), false);
        }

        private static Guid GetDeterministicGuidFrom(Guid guidToConvert)
        {
            var codeConverterStaticGuid = new Guid("{B224816B-CC58-4FF1-8258-CA7E629734A0}");
            var deterministicNewBytes = codeConverterStaticGuid.ToByteArray().Zip(guidToConvert.ToByteArray(),
                (fromFirst, fromSecond) => (byte)(fromFirst ^ fromSecond));
            return new Guid(deterministicNewBytes.ToArray());
        }
    }
}