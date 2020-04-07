using System;
using System.Diagnostics;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.VisualStudio.Threading;

namespace ICSharpCode.CodeConverter.DotNetTool.Util
{
    internal static class ProcessRunner
    {
        public static Task<Process> StartRedirectedToConsoleAsync(string command, params string[] args)
        {
            return new ProcessStartInfo(command, ArgumentEscaper.EscapeAndConcatenate(args)).StartRedirectedToConsoleAsync();
        }

        private static async Task<Process> StartRedirectedToConsoleAsync(this ProcessStartInfo psi)
        {
            psi.UseShellExecute = false;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            var process = new Process() { StartInfo = psi };
            process.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
            process.ErrorDataReceived += (sender, e) => Console.Error.WriteLine(e.Data);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            return process;
        }
    }
}
