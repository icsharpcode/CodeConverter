using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.DotNetTool.Util;
using Microsoft.VisualBasic.FileIO;
using Microsoft.VisualStudio.Threading;

namespace ICSharpCode.CodeConverter.DotNetTool
{
    public static class ConversionResultWriter
    {
        public static async Task WriteConvertedAsync(IAsyncEnumerable<ConversionResult> conversionResultsEnumerable, string solutionFilePath, DirectoryInfo targetDirectory, bool wipeTargetDirectory, bool copyOriginalDirectory, IProgress<string> progress, CancellationToken cancellationToken)
        {
            var solutionFile = new FileInfo(solutionFilePath);
            var sourceAndTargetSame = string.Equals(solutionFile.Directory.FullName, targetDirectory.FullName);

            if (!sourceAndTargetSame) {
                if (wipeTargetDirectory) {
                    progress.Report($"Removing {targetDirectory.FullName}");
                    targetDirectory.DeleteExceptGitDir();
                }

                if (copyOriginalDirectory) {
                    progress.Report($"Started copying contents of {solutionFile.Directory.FullName} to {targetDirectory.FullName} so that the output is a usable solution.{Environment.NewLine}" +
                        "If you don't see the 'Finished copying contents' message, consider running the conversion in-place by not specifying an output directory."
                    );
                    FileSystem.CopyDirectory(solutionFile.Directory.FullName, targetDirectory.FullName, true);
                    progress.Report($"Finished copying contents of {solutionFile.Directory.FullName} to {targetDirectory.FullName}.");
                }
            }

            await foreach (var conversionResult in conversionResultsEnumerable.Where(x => x.TargetPathOrNull != null)) {
                cancellationToken.ThrowIfCancellationRequested();
                var expectedFilePath =
                    conversionResult.TargetPathOrNull.Replace(solutionFile.Directory.FullName, targetDirectory.FullName);
                Directory.CreateDirectory(Path.GetDirectoryName(expectedFilePath));
                File.WriteAllText(expectedFilePath, conversionResult.ConvertedCode);
            }
        }
    }
}
