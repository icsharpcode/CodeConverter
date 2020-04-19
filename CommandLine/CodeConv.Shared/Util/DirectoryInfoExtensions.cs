using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.DotNetTool.Util;
using ICSharpCode.CodeConverter.Util;
using McMaster.Extensions.CommandLineUtils;

namespace ICSharpCode.CodeConverter.CommandLine.Util
{

    internal static class DirectoryInfoExtensions
    {
        public static async Task CopyExceptAsync(this DirectoryInfo sourceDirectory, DirectoryInfo targetDirectory, bool overwrite, params string[] excludeNames)
        {
            targetDirectory.Create();
            foreach (var fileSystemEntry in sourceDirectory.GetFileSystemInfos().Where(d => !excludeNames.Contains(d.Name, StringComparer.OrdinalIgnoreCase))) {
                var targetPath = Path.Combine(targetDirectory.FullName, fileSystemEntry.Name);
                if (fileSystemEntry is DirectoryInfo currentSourceDir) {
                    await CopyExceptAsync(currentSourceDir, new DirectoryInfo(targetPath), overwrite, excludeNames);
                } else if (fileSystemEntry is FileInfo fi) {
                    await fi.RetryAsync(f => f.CopyTo(targetPath, overwrite));
                }
            }
        }

        /// <returns>true iff directory is completely deleted</returns>
        public static async Task<bool> DeleteExceptAsync(this DirectoryInfo targetDirectory, params string[] excludeNames)
        {
            if (!targetDirectory.Exists) return true;
            var filesAndDirs = targetDirectory.GetFileSystemInfos();
            var excluded = filesAndDirs.Where(d => excludeNames.Contains(d.Name, StringComparer.OrdinalIgnoreCase)).ToArray();

            bool foundExclusion = excluded.Any();
            foreach (var fileSystemInfo in filesAndDirs.Except(excluded)) {
                if (fileSystemInfo is DirectoryInfo di) {
                    foundExclusion |= await DeleteExceptAsync(di, excludeNames);
                } else {
                    fileSystemInfo.Delete();
                }
            }
            if (!foundExclusion) await targetDirectory.RetryAsync(d => d.Delete(true));
            return !foundExclusion;
        }

        private static async Task RetryAsync<T>(this T fileSystemInfo, Action<T> action, byte retries = 10, ushort delayMs = 10) where T: FileSystemInfo
        {
            for (int i = 0; i <= retries; i++) {
                try {
                    action(fileSystemInfo);
                    return;
                } catch (Exception) when (i < retries) {
                    await Task.Delay(delayMs);
                    fileSystemInfo.Refresh();
                }
            }
        }

        public static async Task<bool> IsGitDiffEmptyAsync(this DirectoryInfo outputDirectory)
        {
            var gitDiff = new ProcessStartInfo("git") {
                Arguments = ArgumentEscaper.EscapeAndConcatenate(new[] { "diff", "--exit-code", "--relative", "--summary", "--diff-filter=ACMRTUXB*" }),
                WorkingDirectory = outputDirectory.FullName
            };

            var (exitCode, stdErr, _) = await gitDiff.GetOutputAsync();
            if (exitCode == 1) Console.WriteLine(stdErr);
            return exitCode == 0;
        }

        public static bool ContainsDataOtherThanGitDir(this DirectoryInfo outputDirectory)
        {
            var filesAndFolders = outputDirectory.GetFileSystemInfos();
            return filesAndFolders.Any(d => !string.Equals(d.Name, ".git", StringComparison.OrdinalIgnoreCase));
        }
    }
}
