using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.DotNetTool.Util;
using ICSharpCode.CodeConverter.Util;

namespace ICSharpCode.CodeConverter.CommandLine.Util
{

    internal static class DirectoryInfoExtensions
    {
        /// <remarks>
        /// Should never be writing output within a .git dir, so it's safe to leave it there, and massively reduces the chances of accidentally destroying work
        /// </remarks>
        /// <returns>true iff directory was entirely removed (due to finding no git directory)</returns>
        public static bool DeleteExceptGitDir(this DirectoryInfo targetDirectory)
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

        public static async Task<bool> IsGitDiffEmptyAsync(this DirectoryInfo outputDirectory)
        {
            var gitDiff = await ProcessRunner.StartRedirectedToConsoleAsync(outputDirectory, "git", "diff", "--exit-code");
            return gitDiff.ExitCode != 0;
        }

        public static bool ContainsDataOtherThanGitDir(this DirectoryInfo outputDirectory)
        {
            var filesAndFolders = outputDirectory.GetFileSystemInfos();
            return filesAndFolders.Any(d => !string.Equals(d.Name, ".git", StringComparison.OrdinalIgnoreCase));
        }
    }
}
