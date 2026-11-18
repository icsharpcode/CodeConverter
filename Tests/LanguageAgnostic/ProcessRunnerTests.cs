using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.DotNetTool.Util;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.LanguageAgnostic;

/// <remarks>
/// In the MSBuild collection so the restore doesn't race the multi-file tests' restore of the same solution.
/// </remarks>
[Collection(MultiFileTestFixture.Collection)]
public class ProcessRunnerTests
{
    private static readonly TimeSpan RestoreTimeout = TimeSpan.FromMinutes(3);

    [Fact]
    public async Task GetOutputAsyncReturnsWhenRestoreLeavesNewWorkerNodesRunningAsync()
    {
        var solutionFile = Path.Combine(TestConstants.GetTestDataDirectory(), "MultiFileCharacterization", "SourceFiles", "CharacterizationTestSolution.sln");
        var psi = new ProcessStartInfo("dotnet", $"restore \"{solutionFile}\"");
        psi.Environment["EnableWindowsTargeting"] = "true";
        // `dotnet test` sets this for its children: MSBuild then lets worker nodes inherit stdout/stderr instead of redirecting them
        psi.Environment["MSBUILDENSURESTDOUTFORTASKPROCESSES"] = "1";
        // A unique handshake stops MSBuild reusing an idle node, so it must launch new ones
        psi.Environment["MSBUILDNODEHANDSHAKESALT"] = Guid.NewGuid().ToString("N");

        var restore = psi.GetOutputAsync();
        var completed = await Task.WhenAny(restore, Task.Delay(RestoreTimeout));

        Assert.True(completed == restore, $"dotnet restore output didn't end within {RestoreTimeout}");
        var (exitCode, _, stdErr) = await restore;
        Assert.True(exitCode == 0, stdErr);
    }
}
