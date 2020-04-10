using System;
using System.Diagnostics;
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

        public static async Task<string> GetSuccessStdOutAsync(string command, params string[] args)
        {
            var sb = new StringBuilder();
            var proc = await new ProcessStartInfo(Environment.ExpandEnvironmentVariables(command), ArgumentEscaper.EscapeAndConcatenate(args)).StartRedirectedToConsoleAsync(sb);
            return proc.ExitCode == 0 ? sb.ToString().Trim('\r', '\n') : null;
        }

        private static async Task<Process> StartRedirectedToConsoleAsync(this ProcessStartInfo psi, StringBuilder stdOut = null)
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
            return process;
        }
    }
}
