using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.VisualStudio.Threading;

namespace ICSharpCode.CodeConverter.DotNetTool.Util;

public static class ProcessRunner
{

    public static Task<int> ConnectConsoleGetExitCodeAsync(string command, params string[] args) =>
        new ProcessStartInfo(command, ArgumentEscaper.EscapeAndConcatenate(args)).GetExitCodeAsync();

    public static async Task<(int ExitCode, string StdOut, string StdErr)> GetOutputAsync(this ProcessStartInfo psi)
    {
        var stdOutStringBuilder = new StringBuilder();
        using var stdOut = new StringWriter(stdOutStringBuilder);
        var stdErrStringBuilder = new StringBuilder();
        using var stdErr = new StringWriter(stdErrStringBuilder);
        var exitCode = await GetExitCodeAsync(psi, stdOut, stdErr);
        return (exitCode, stdOutStringBuilder.ToString(), stdErrStringBuilder.ToString());
    }

    /// <param name="psi">Process is started from this information</param>
    /// <param name="stdOut">Defaults to Console.Out</param>
    /// <param name="stdErr">Defaults to Console.Error</param>
    private static async Task<int> GetExitCodeAsync(this ProcessStartInfo psi, TextWriter? stdOut = null, TextWriter? stdErr = null)
    {
        stdOut ??= Console.Out;
        stdErr ??= Console.Error;
        psi.UseShellExecute = false;
        psi.RedirectStandardError = true;
        psi.RedirectStandardOutput = true;
        DisableMsBuildNodeReuse(psi);
        using var process = new Process { StartInfo = psi };
        var stdOutComplete = new TaskCompletionSource<object?>();
        var stdErrComplete = new TaskCompletionSource<object?>();
        process.OutputDataReceived += (sender, e) => {
            if (e.Data != null)
                stdOut.WriteLine(e.Data);
            else
                stdOutComplete.SetResult(null);
        };
        process.ErrorDataReceived += (sender, e) => {
            if (e.Data != null)
                stdErr.WriteLine(e.Data);
            else
                stdErrComplete.SetResult(null);
        };
        try {
            process.Start();
        } catch (Win32Exception win32Exception) {
            return win32Exception.ErrorCode;
        }
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await Task.WhenAll(process.WaitForExitAsync(), stdOutComplete.Task, stdErrComplete.Task);

        return process.ExitCode;
    }

    /// <summary>
    /// We wait for the redirected stdout/stderr to close, which only happens once every process holding them has exited.
    /// MSBuild keeps worker nodes alive after the build for reuse, and launches them without redirecting their streams when
    /// MSBUILDENSURESTDOUTFORTASKPROCESSES=1 (which `dotnet test` sets for its children), so they inherit our pipes and we'd wait
    /// until they time out. Without node reuse, the nodes exit with the build.
    /// Streams only redirected when that variable isn't set:
    /// https://github.com/dotnet/msbuild/blob/2bccd27c1733ff52893ff41e3b4046e51d0334ee/src/Build/BackEnd/Components/Communications/NodeLauncher.cs#L65-L67
    /// https://github.com/dotnet/msbuild/blob/2bccd27c1733ff52893ff41e3b4046e51d0334ee/src/Framework/Traits.cs#L487-L490
    /// MSBUILDDISABLENODEREUSE: https://github.com/dotnet/msbuild/blob/2bccd27c1733ff52893ff41e3b4046e51d0334ee/documentation/wiki/MSBuild-Environment-Variables.md
    /// </summary>
    private static void DisableMsBuildNodeReuse(ProcessStartInfo psi) => psi.Environment["MSBUILDDISABLENODEREUSE"] = "1";
}