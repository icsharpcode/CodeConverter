using System;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using ICSharpCode.CodeConverter.DotNetTool.Util;
using System.Reflection;

namespace ICSharpCode.CodeConverter.CommandLine
{

    public partial class CodeConvProgram
    {

        public static async Task<int> Main(string[] args)
        {
            // Ideally we'd be able to use MSBuildLocator.QueryVisualStudioInstances(DiscoveryType.VisualStudioSetup) from .NET core, but it will never be supported: https://github.com/microsoft/MSBuildLocator/issues/61
            // Instead, if MSBuild 16.0+ is available, start a .NET framework process and let it run with that
            var latestMsBuildExePath = await ProcessRunner.GetSuccessStdOutAsync(@"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe", "-latest", "-prerelease", "-products", "*", "-requires", "Microsoft.Component.MSBuild", "-version", "[16.0,]", "-find", @"MSBuild\**\Bin\MSBuild.exe");
            if (!args.Contains(CoreOptionDefinition, StringComparer.OrdinalIgnoreCase) && !string.IsNullOrEmpty(latestMsBuildExePath)) {
                Console.WriteLine($"Using MSBuild from {latestMsBuildExePath}");
                string currentAssemblyLocation = Assembly.GetExecutingAssembly().Location;
                var assemblyPath = Path.GetDirectoryName(currentAssemblyLocation);
                if (string.IsNullOrWhiteSpace(assemblyPath)) throw new InvalidOperationException("Could not retrieve executing assembly location");
                var netFrameworkExe = Path.Combine(assemblyPath, "NetFramework", "ICSharpCode.CodeConverter.CodeConv.NetFramework.exe");
                var process = await ProcessRunner.StartRedirectedToConsoleAsync(netFrameworkExe, args);
                return process.ExitCode;
            }

            return await ExecuteCurrentFrameworkAsync(args);
        }
    }
}
