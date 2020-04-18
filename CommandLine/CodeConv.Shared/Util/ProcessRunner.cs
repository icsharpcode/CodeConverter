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
        public static Task<Process> StartRedirectedToConsoleAsync(string command, params string[] args)
        {
            return new ProcessStartInfo(Environment.ExpandEnvironmentVariables(command), ArgumentEscaper.EscapeAndConcatenate(args)).StartRedirectedToConsoleAsync();
        }

        public static async Task<string?> GetSuccessStdOutAsync(string command, params string[] args)
        {
            var sb = new StringBuilder();
            string fullFilePath = Environment.ExpandEnvironmentVariables(command);
            var psi = new ProcessStartInfo(fullFilePath, ArgumentEscaper.EscapeAndConcatenate(args));
            if (Path.IsPathRooted(command)) {
                if (!File.Exists(fullFilePath)) return null;
                psi.WorkingDirectory = Path.GetDirectoryName(psi.FileName);
            }

                var proc = await psi.StartRedirectedToConsoleAsync(sb);
            if (proc.ExitCode == 0 && !string.IsNullOrWhiteSpace(sb.ToString())) return sb.ToString().Trim('\r', '\n');

            return null;
        }

        private static async Task<Process> StartRedirectedToConsoleAsync(this ProcessStartInfo psi, StringBuilder? stdOut = null)
        {
            psi.UseShellExecute = false;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            var process = new Process() { StartInfo = psi };
            process.OutputDataReceived += (sender, e) => { Console.WriteLine(e.Data); stdOut?.AppendLine(e.Data); };
            process.ErrorDataReceived += (sender, e) => Console.Error.WriteLine(e.Data);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            for(int i = 0; process.ExitCode == 0 && stdOut != null && stdOut.Length == 0 && i < 500; i++) {
                await Task.Delay(20);
            }
            return process;
        }
    }
}
