using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.VisualStudio.Threading;

namespace ICSharpCode.CodeConverter.DotNetTool.Util
{
    internal static class ProcessRunner
    {
        public static Task<int> RedirectConsoleAndGetExitCodeAsync(DirectoryInfo workingDirectory, int maxStdOutLines, string command, params string[] args)
        {
            return new ProcessStartInfo(Environment.ExpandEnvironmentVariables(command), ArgumentEscaper.EscapeAndConcatenate(args)) {
                WorkingDirectory = workingDirectory.FullName
            }.RedirectConsoleGetExitCodeAsync(maxStdOutLines);
        }

        public static Task<int> RedirectConsoleAndGetExitCodeAsync(string command, params string[] args) =>
            new ProcessStartInfo(Environment.ExpandEnvironmentVariables(command), ArgumentEscaper.EscapeAndConcatenate(args))
            .RedirectConsoleGetExitCodeAsync();

        public static async Task<string?> RedirectConsoleGetStdOutAsync(string command, params string[] args)
        {
            var sb = new StringBuilder();
            string fullFilePath = Environment.ExpandEnvironmentVariables(command);
            var psi = new ProcessStartInfo(fullFilePath, ArgumentEscaper.EscapeAndConcatenate(args));
            if (Path.IsPathRooted(command)) {
                if (!File.Exists(fullFilePath)) return null;
                psi.WorkingDirectory = Path.GetDirectoryName(psi.FileName);
            }

            var exitCode = await psi.RedirectConsoleGetExitCodeAsync(stdOut: sb);
            if (exitCode == 0 && !string.IsNullOrWhiteSpace(sb.ToString())) return sb.ToString().Trim('\r', '\n');

            return null;
        }

        private static async Task<int> RedirectConsoleGetExitCodeAsync(this ProcessStartInfo psi, int maxStdOutLines = int.MaxValue, StringBuilder? stdOut = null)
        {
            psi.UseShellExecute = false;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            using var process = new Process() { StartInfo = psi };
            process.OutputDataReceived += (sender, e) => { if (--maxStdOutLines > 0) { Console.WriteLine(e.Data); stdOut?.AppendLine(e.Data); } };
            process.ErrorDataReceived += (sender, e) => Console.Error.WriteLine(e.Data);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            for(int i = 0; process.ExitCode == 0 && stdOut != null && stdOut.Length == 0 && i < 500; i++) {
                await Task.Delay(20);
            }
            return process.ExitCode;
        }
    }
}
