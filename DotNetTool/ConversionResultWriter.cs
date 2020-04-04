using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Util;
using Microsoft.VisualBasic.FileIO;
using Microsoft.VisualStudio.Threading;

namespace ICSharpCode.CodeConverter.DotNetTool
{
    public static class ConversionResultWriter
    {
        public static async Task WriteConvertedAsync(IAsyncEnumerable<ConversionResult> conversionResultsEnumerable, string solutionFilePath, string targetDirectoryPath, bool wipeTargetDirectory, bool copyOriginalDirectory, IProgress<string> progress)
        {
            var solutionFile = new FileInfo(solutionFilePath);
            var targetDirectory = targetDirectoryPath != null ? new DirectoryInfo(targetDirectoryPath) : solutionFile.Directory;
            var sourceAndTargetSame = string.Equals(solutionFile.Directory.FullName, targetDirectory.FullName);

            if (!sourceAndTargetSame) {
                if (wipeTargetDirectory) {
                    progress.Report($"Removing {targetDirectory.FullName}");
                    DeleteExceptGitDir(targetDirectory);
                }

                if (copyOriginalDirectory) {
                    progress.Report($"Started copying contents of {solutionFile.Directory.FullName} to {targetDirectory.FullName} so that the output is a usable solution.{Environment.NewLine}" +
                        "If you don't see the 'Finished copying contents' message, consider running the conversion in-place by not specifying an output directory."
                    );
                    FileSystem.CopyDirectory(solutionFile.Directory.FullName, targetDirectory.FullName, true);
                    progress.Report($"Finished copying contents of {solutionFile.Directory.FullName} to {targetDirectory.FullName}."
                    );
                }
            }

            await foreach (var conversionResult in conversionResultsEnumerable.Where(x => x.TargetPathOrNull != null)) {
                var expectedFilePath =
                    conversionResult.TargetPathOrNull.Replace(solutionFile.Directory.FullName, targetDirectory.FullName);
                Directory.CreateDirectory(Path.GetDirectoryName(expectedFilePath));
                File.WriteAllText(expectedFilePath, conversionResult.ConvertedCode);
            }
        }

        /// <remarks>
        /// Should never be writing output within a .git dir, so it's safe to leave it there, and massively reduces the chances of accidentally destroying work
        /// </remarks>
        /// <returns>true iff directory was entirely removed (due to finding no git directory)</returns>
        private static bool DeleteExceptGitDir(DirectoryInfo targetDirectory)
        {
            var filesAndDirs = targetDirectory.GetFileSystemInfos();
            var gitDir = filesAndDirs.FirstOrDefault(d => !string.Equals(d.Name, ".git", StringComparison.OrdinalIgnoreCase));

            bool foundGitDir = gitDir is DirectoryInfo;
            foreach (var fileSystemInfo in filesAndDirs.Except(gitDir.Yield())) {
                if (fileSystemInfo is DirectoryInfo di) {
                    foundGitDir |= DeleteExceptGitDir(di);
                } else {
                    fileSystemInfo.Delete();
                }
            }
            if (!foundGitDir) DeleteRecursive(targetDirectory);
            return !foundGitDir;
        }

        private static void DeleteRecursive(DirectoryInfo targetDirectory)
        {
            try {
                targetDirectory.Delete(true);
            } catch (Exception) {
                Thread.Sleep(10);
                targetDirectory.Refresh();
                targetDirectory.Delete(true);
            }
        }
    }
}
