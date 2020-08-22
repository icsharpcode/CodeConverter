using System;
using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;
using System.Threading;
using ICSharpCode.CodeConverter.Shared;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.DotNetTool.Util;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using ICSharpCode.CodeConverter.CommandLine.Util;
using CodeConv.Shared.Util;
using System.Reflection;
using System.Diagnostics;
using Microsoft.VisualStudio.Threading;

namespace ICSharpCode.CodeConverter.CommandLine
{
    [Command(Name = "codeconv", Description = "Convert code from VB.NET to C# or C# to VB.NET",
        ExtendedHelpText = @"
Remarks:
  Converts all projects in a solution from VB.NET to C#.
  Please backup / commit your files to source control before use.
  We recommend running the conversion in-place (i.e. not specifying an output directory) for best performance.
  See https://github.com/icsharpcode/CodeConverter for the source code, issues, Visual Studio extension and other info.
")]
    [HelpOption("-h|--help")]
    public partial class CodeConvProgram
    {
        public const string CoreOptionDefinition = "--core-only";

        /// <remarks>Calls <see cref="OnExecuteAsync(CommandLineApplication)"/> by reflection</remarks>
        public static async Task<int> Main(string[] args) => await CommandLineApplication.ExecuteAsync<CodeConvProgram>(args);
        /// <remarks>Used by reflection in CommandLineApplication.ExecuteAsync</remarks>
        private async Task<int> OnExecuteAsync(CommandLineApplication _) => await ExecuteAsync();

        [FileExists]
        [Required]
        [Argument(0, "Source solution path", "The solution containing project(s) to be converted.")]
        public string SolutionPath { get; } = "";

        [Option("-i|--include", "Regex matching project file paths to convert. Can be used multiple times", CommandOptionType.MultipleValue)]
        public string[] Include { get; } = new string[0];

        [Option("-e|--exclude", "Regex matching project file paths to exclude from conversion. Can be used multiple times", CommandOptionType.MultipleValue)]
        public string[] Exclude { get; } = new string[0];

        [Option("-t|--target-language", "The language to convert to.", CommandOptionType.SingleValue, ValueName = nameof(Language.CS) + " | " + nameof(Language.VB))]
        public Language? TargetLanguage { get; }

        [Option("-f|--force", "Wipe the output directory before conversion", CommandOptionType.NoValue)]
        public bool Force { get; }

        [Option(CoreOptionDefinition, "Force dot net core build if converting only .NET Core projects and seeing pre-conversion compile errors", CommandOptionType.SingleValue)]
        public bool CoreOnlyProjects { get; }

        [Option("-b|--best-effort", "Overrides warnings about compilation issues with input, and attempts a best effort conversion anyway", CommandOptionType.NoValue)]
        public bool BestEffort { get; }

        [FileNotExists]
        [Option("-o|--output-directory", "Empty or non-existent directory to copy the solution directory to, then write the output.", CommandOptionType.SingleValue)]
        public string? OutputDirectory { get; }

        /// <remarks>
        /// Also allows semicolon and comma splitting of build properties to be compatible with https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild-command-line-reference?view=vs-2019#switches
        /// </remarks>
        [Option("-p|--build-property", "Set build properties in format: propertyName=propertyValue. Can be used multiple times", CommandOptionType.MultipleValue, ValueName = "Configuration=Release")]
        public string[] BuildProperty { get; } = new string[0];

        private async Task<int> ExecuteAsync()
        {
            // Ideally we'd be able to use MSBuildLocator.QueryVisualStudioInstances(DiscoveryType.VisualStudioSetup) from .NET core, but it will never be supported: https://github.com/microsoft/MSBuildLocator/issues/61
            // Instead, if MSBuild 16.0+ is available, start a .NET framework process and let it run with that
            if (_runningInNetCore && !CoreOnlyProjects) {
                if (await GetLatestMsBuildExePathAsync() is { } latestMsBuildExePath) {
                    return await RunNetFrameworkExeAsync(latestMsBuildExePath);
                } else {
                    Console.WriteLine($"Using dot net SDK MSBuild which only works for dot net core projects.");
                }
            }

            try {
                Progress<ConversionProgress>? progress = new Progress<ConversionProgress>(s => Console.Out.WriteLine(s.ToString()));
                await ConvertAsync(progress, CancellationToken.None);
            } catch (Exception ex) {
                await Task.Delay(100); // Give any async progress updates a moment to flush so they don't clash with this:

                if (!(ex is ValidationException)) {
                    await Console.Error.WriteLineAsync(Environment.NewLine + ex.GetType() + ex.StackTrace);
                    if (ex is ReflectionTypeLoadException rtle && rtle.LoaderExceptions is IEnumerable<Exception> loaderExceptions) {
                        foreach (var e in loaderExceptions) {
                            await Console.Error.WriteLineAsync(e.Message);
                        }
                    }
                }

                await Console.Error.WriteLineAsync(Environment.NewLine + ex.Message + Environment.NewLine +
                    "Please report issues at github.com/icsharpcode/CodeConverter"
                );
                return ProgramExitCodes.EX_SOFTWARE;
            }

            Console.WriteLine();
            Console.WriteLine("Exiting successfully. Report any issues at github.com/icsharpcode/CodeConverter to help us improve the accuracy of future conversions");
            return 0;
        }

        private static async Task<int> RunNetFrameworkExeAsync(string latestMsBuildExePath)
        {
            Console.WriteLine($"Using .NET Framework MSBuild from {latestMsBuildExePath}");
            var assemblyPath = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
            var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
            if (string.IsNullOrWhiteSpace(assemblyPath)) throw new InvalidOperationException("Could not retrieve executing assembly directory");
            var netFrameworkExe = Path.Combine(assemblyPath, "NetFramework", "ICSharpCode.CodeConverter.CodeConv.NetFramework.exe");
            var exitCode = await ProcessRunner.ConnectConsoleGetExitCodeAsync(netFrameworkExe, args);
            return exitCode;
        }

        private async Task ConvertAsync(IProgress<ConversionProgress> progress, CancellationToken cancellationToken)
        {
            IProgress<string> strProgress = new Progress<string>(p => progress.Report(new ConversionProgress(p)));

            if (!string.Equals(Path.GetExtension(SolutionPath), ".sln", StringComparison.OrdinalIgnoreCase)) {
                throw new ValidationException("Solution path must end in `.sln`");
            }

            var outputDirectory = new DirectoryInfo(string.IsNullOrWhiteSpace(OutputDirectory) ? Path.GetDirectoryName(SolutionPath) : OutputDirectory);
            if (await CouldOverwriteUncomittedFilesAsync(outputDirectory)) {
                var action = string.IsNullOrWhiteSpace(OutputDirectory) ? "may be overwritten" : "will be deleted";
                strProgress.Report($"WARNING: There are files in {outputDirectory.FullName} which {action}, and aren't comitted to git");
                if (Force) strProgress.Report("Continuing with possibility of data loss due to force option.");
                else throw new ValidationException("Aborting to avoid data loss (see above warning). Commit the files to git, remove them, or use the --force option to override this check.");
            }

            var properties = ParsedProperties();
            var joinableTaskFactory = new JoinableTaskFactory(new JoinableTaskContext());
            var msbuildWorkspaceConverter = new MSBuildWorkspaceConverter(SolutionPath, CoreOnlyProjects, joinableTaskFactory, BestEffort, properties);

            var converterResultsEnumerable = msbuildWorkspaceConverter.ConvertProjectsWhereAsync(ShouldIncludeProject, TargetLanguage, progress, cancellationToken);
            await ConversionResultWriter.WriteConvertedAsync(converterResultsEnumerable, SolutionPath, outputDirectory, Force, true, strProgress, cancellationToken);
        }

        private static async Task<bool> CouldOverwriteUncomittedFilesAsync(DirectoryInfo outputDirectory)
        {
            if (!outputDirectory.Exists || !outputDirectory.ContainsDataOtherThanGitDir()) return false;
            return !await outputDirectory.IsGitDiffEmptyAsync();
        }

        private Dictionary<string, string> ParsedProperties()
        {
            var props = BuildProperty.SelectMany(bp => bp.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Split('=')));
            return props.ToLookup(s => s[0], GetValidatedPropertyValue).ToDictionary();
        }

        private string GetValidatedPropertyValue(string[] s)
        {
            return s.Length == 2 ? s[1] : throw new ValidationException($"Build property {s[0]} must have exactly one value, e.g. `{s[0]}=1`");
        }

        private bool ShouldIncludeProject(Project project)
        {
            var isIncluded = !Include.Any() || Include.Any(regex => Regex.IsMatch(project.FilePath, regex));
            return isIncluded && Exclude.All(regex => !Regex.IsMatch(project.FilePath, regex));
        }

        private static async Task<string?> GetLatestMsBuildExePathAsync()
        {
            var vsWhereExe = Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe");
            var args = new[] { "-latest", "-prerelease", "-products", "*", "-requires", "Microsoft.Component.MSBuild", "-version", "[16.0,]", "-find", @"MSBuild\**\Bin\MSBuild.exe" };
            
            var (exitCode, stdOut, _) = await new ProcessStartInfo(vsWhereExe) {
                Arguments = ArgumentEscaper.EscapeAndConcatenate(args)
            }.GetOutputAsync();

            if (exitCode == 0 && !string.IsNullOrWhiteSpace(stdOut)) return stdOut;
            return null;
        }
    }
}
