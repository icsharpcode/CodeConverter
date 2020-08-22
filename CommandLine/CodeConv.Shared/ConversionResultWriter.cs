using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.CommandLine.Util;
using Microsoft.VisualStudio.Threading;

namespace ICSharpCode.CodeConverter.CommandLine
{
    public static class ConversionResultWriter
    {
        private static readonly string[] FileSystemNamesToIgnore = new[] { ".git", ".gitattributes", ".gitignore", "bin", "obj", ".vs" };

        public static async Task WriteConvertedAsync(IAsyncEnumerable<ConversionResult> conversionResultsEnumerable, string solutionFilePath, DirectoryInfo targetDirectory, bool wipeTargetDirectory, bool copyOriginalDirectory, IProgress<string> progress, CancellationToken cancellationToken)
        {
            var solutionFile = new FileInfo(solutionFilePath);
            var sourceAndTargetSame = string.Equals(solutionFile.Directory.FullName, targetDirectory.FullName);

            if (!sourceAndTargetSame) {
                if (wipeTargetDirectory) {
                    progress.Report($"Removing {targetDirectory.FullName}");
                    //Should never be writing output within a .git dir, so it's safe to leave it there, and massively reduces the chances of accidentally destroying work
                    await targetDirectory.DeleteExceptAsync(FileSystemNamesToIgnore);
                }

                if (copyOriginalDirectory) {
                    progress.Report($"Started copying contents of {solutionFile.Directory.FullName} to {targetDirectory.FullName} so that the output is a usable solution.{Environment.NewLine}" +
                        "If you don't see the 'Finished copying contents' message, consider running the conversion in-place by not specifying an output directory."
                    );

                    // Speed up the copy by skipping irrelevant binaries and caches. An alternative would be to attempt a git clone
                    await solutionFile.Directory.CopyExceptAsync(targetDirectory, true, FileSystemNamesToIgnore);
                    progress.Report($"Finished copying contents of {solutionFile.Directory.FullName} to {targetDirectory.FullName}.");
                }
            }

            var sourcePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var targetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            await foreach (var conversionResult in conversionResultsEnumerable.Where(x => x.TargetPathOrNull != null)) {
                sourcePaths.Add(Path.GetFullPath(conversionResult.SourcePathOrNull));
                targetPaths.Add(Path.GetFullPath(conversionResult.TargetPathOrNull));
                cancellationToken.ThrowIfCancellationRequested();
                var targetFilePath =
                    conversionResult.TargetPathOrNull.Replace(solutionFile.Directory.FullName, targetDirectory.FullName);
                Directory.CreateDirectory(Path.GetDirectoryName(targetFilePath));
                File.WriteAllText(targetFilePath, conversionResult.ConvertedCode);
            }

            if (!sourceAndTargetSame) {
                var filePathsToRemove = sourcePaths.Except(targetPaths);
                foreach (var filePathToRemove in filePathsToRemove) {
                    string pathInTargetDir = filePathToRemove.Replace(solutionFile.Directory.FullName, targetDirectory.FullName);
                    if (File.Exists(pathInTargetDir)) File.Delete(pathInTargetDir);
                }
            }
        }
    }
}
