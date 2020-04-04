using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;
using Microsoft.VisualStudio.Threading;

namespace ICSharpCode.CodeConverter.DotNetTool
{
    public static class ConversionResultWriter
    {
        public static async Task WriteConvertedAsync(IAsyncEnumerable<ConversionResult> conversionResultsEnumerable, string solutionFilePath, string targetDirectoryPath, bool wipeTargetDirectory, bool copyOriginalDirectory)
        {
            var solutionFile = new FileInfo(solutionFilePath);
            var targetDirectory = targetDirectoryPath != null ? new DirectoryInfo(targetDirectoryPath) : solutionFile.Directory;

            if (wipeTargetDirectory) targetDirectory.Delete(true);
            if (copyOriginalDirectory) FileSystem.CopyDirectory(solutionFile.Directory.FullName, targetDirectory.FullName);

            await foreach (var conversionResult in conversionResultsEnumerable.Where(x => x.TargetPathOrNull != null)) {
                var expectedFilePath =
                    conversionResult.TargetPathOrNull.Replace(solutionFile.Directory.FullName, targetDirectory.FullName);
                Directory.CreateDirectory(Path.GetDirectoryName(expectedFilePath));
                File.WriteAllText(expectedFilePath, conversionResult.ConvertedCode);
            }
        }
    }
}
