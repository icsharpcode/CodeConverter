using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace ICSharpCode.CodeConverter.DotNetTool.Util
{
    internal static class ProcessStartInfoExtensions
    {
        public static async Task<Process> StartRedirectedToConsoleAsync(this ProcessStartInfo psi)
        {
            psi.UseShellExecute = false;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            var dotnetRestore = new Process() { StartInfo = psi };
            dotnetRestore.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
            dotnetRestore.ErrorDataReceived += (sender, e) => Console.Error.WriteLine(e.Data);
            dotnetRestore.Start();
            dotnetRestore.BeginOutputReadLine();
            dotnetRestore.BeginErrorReadLine();
            await dotnetRestore.WaitForExitAsync();
            return dotnetRestore;
        }
    }
}
