using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.DotNetTool.Util;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.LanguageAgnostic;

public class ProcessRunnerTests
{
    private static readonly TimeSpan BuildTimeout = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Two SDK-less projects built in parallel make MSBuild launch a worker node, without the cost of a real restore.
    /// </summary>
    [Fact]
    public async Task GetOutputAsyncReturnsWhenBuildLeavesNewWorkerNodesRunningAsync()
    {
        var dir = Directory.CreateTempSubdirectory("ProcessRunnerTests");
        try {
            File.WriteAllText(Path.Combine(dir.FullName, "child.proj"), @"<Project><Target Name=""Build"" /></Project>");
            File.WriteAllText(Path.Combine(dir.FullName, "root.proj"), @"<Project>
  <ItemGroup>
    <P Include=""child.proj"" AdditionalProperties=""X=1"" />
    <P Include=""child.proj"" AdditionalProperties=""X=2"" />
  </ItemGroup>
  <Target Name=""Build""><MSBuild Projects=""@(P)"" BuildInParallel=""true"" /></Target>
</Project>");
            var psi = new ProcessStartInfo("dotnet", "msbuild root.proj -m:2 -nologo -v:q") { WorkingDirectory = dir.FullName };
            // `dotnet test` sets this for its children: MSBuild then lets worker nodes inherit stdout/stderr instead of redirecting them
            psi.Environment["MSBUILDENSURESTDOUTFORTASKPROCESSES"] = "1";
            // A unique handshake stops MSBuild reusing an idle node, so it must launch a new one
            psi.Environment["MSBUILDNODEHANDSHAKESALT"] = Guid.NewGuid().ToString("N");

            var build = psi.GetOutputAsync();
            var completed = await Task.WhenAny(build, Task.Delay(BuildTimeout));

            Assert.True(completed == build, $"dotnet msbuild output didn't end within {BuildTimeout}");
            var (exitCode, stdOut, stdErr) = await build;
            Assert.True(exitCode == 0, stdOut + stdErr);
        } finally {
            dir.Delete(true);
        }
    }
}
