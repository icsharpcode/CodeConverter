using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.DotNetTool.Util;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using ICSharpCode.CodeConverter.VB;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.VisualStudio.Threading;
using McMaster.Extensions.CommandLineUtils;
using System.Collections.Concurrent;
using System.IO;

namespace ICSharpCode.CodeConverter.DotNetTool
{
    internal class MSBuildWorkspaceConverter
    {
        private readonly IProgress<string> _progress;
        private readonly bool _bestEffortConversion;
        private readonly string _solutionFilePath;
        private readonly Dictionary<string, string> _buildProps;
        private Solution _cachedSolution;

        public MSBuildWorkspaceConverter(string solutionFilePath, IProgress<string> progress, bool bestEffortConversion = false, Dictionary<string, string> buildProps = null)
        {
            _progress = progress;
            _bestEffortConversion = bestEffortConversion;
            _buildProps ??= new Dictionary<string, string>();
            _buildProps.TryAdd("Configuration", "Debug");
            _buildProps.TryAdd("Platform", "AnyCPU");
            _solutionFilePath = solutionFilePath;
        }

        public async IAsyncEnumerable<ConversionResult> ConvertProjectsWhereAsync(Func<Project, bool> shouldConvertProject, CodeConvProgram.Language? targetLanguage, IProgress<ConversionProgress> progress, [EnumeratorCancellation] CancellationToken token)
        {
            var solution = _cachedSolution ?? (_cachedSolution = await GetSolutionAsync(_solutionFilePath));

            if (!targetLanguage.HasValue) {
                targetLanguage = solution.Projects.Any(p => p.Language == LanguageNames.VisualBasic) ? CodeConvProgram.Language.CS : CodeConvProgram.Language.VB;
            }

            var languageConversion = targetLanguage == CodeConvProgram.Language.CS
                ? (ILanguageConversion)new VBToCSConversion()
                : new CSToVBConversion();
            var languageNameToConvert = targetLanguage == CodeConvProgram.Language.CS
                ? LanguageNames.VisualBasic
                : LanguageNames.CSharp;

            var projectsToConvert = solution.Projects.Where(p => p.Language == languageNameToConvert && shouldConvertProject(p)).ToArray();
            var results = SolutionConverter.CreateFor(languageConversion, projectsToConvert, progress, token).Convert();
            await foreach (var r in results) yield return r;
        }

        private async Task<Solution> GetSolutionAsync(string projectOrSolutionFile)
        {
            _progress.Report($"Running dotnet restore on {projectOrSolutionFile}");
            await RestorePackagesForSolutionAsync(projectOrSolutionFile);

            var workspace = CreateWorkspace(_buildProps);
            var solution = string.Equals(Path.GetExtension(projectOrSolutionFile), ".sln", StringComparison.OrdinalIgnoreCase) ? await workspace.OpenSolutionAsync(projectOrSolutionFile)
                : (await workspace.OpenProjectAsync(projectOrSolutionFile)).Solution;

            var errorString = await GetCompilationErrorsAsync(solution.Projects, workspace.Diagnostics);
            if (errorString != "") {
                _progress.Report($"Please fix compilation erorrs before conversion, or use the best effort conversion option:{Environment.NewLine}{errorString}");
                if (_bestEffortConversion) _progress.Report("Attempting best effort conversion on broken input due to override");
                else throw new InvalidOperationException($"Fix compilation erorrs before conversion for an accurate conversion, or use the best effort conversion option:{Environment.NewLine}{errorString}");
            }
            return solution;
        }

        private static async Task<string> GetCompilationErrorsAsync(
            IEnumerable<Project> projectsToConvert, IReadOnlyCollection<WorkspaceDiagnostic> valueDiagnostics)
        {
            var errors = await projectsToConvert.ParallelSelectAwait(async x => {
                var c = await x.GetCompilationAsync();
                return new[] { CompilationWarnings.WarningsForCompilation(c, c.AssemblyName) };
            }, Env.MaxDop, default).ToArrayAsync();
            var solutionErrors = valueDiagnostics.Where(d => d.Kind > WorkspaceDiagnosticKind.Warning).Select(d => d.Message);
            var errorString = string.Join("\r\n", solutionErrors.Concat(errors.SelectMany(w => w).Where(w => w != null)));
            return errorString;
        }

        private static async Task RestorePackagesForSolutionAsync(string solutionFile)
        {
            var processStartInfo = new ProcessStartInfo(DotNetExe.FullPathOrDefault(), ArgumentEscaper.EscapeAndConcatenate(new[] { "restore", solutionFile }));
            var dotnetRestore = await processStartInfo.StartRedirectedToConsoleAsync();
            if (dotnetRestore.ExitCode != 0) throw new InvalidOperationException($"dotnet restore had a non-zero exit code.");
        }

        private static MSBuildWorkspace CreateWorkspace(Dictionary<string, string> buildProps)
        {
            var instances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
            var instance = instances.OrderByDescending(x => x.Version).FirstOrDefault() 
                ?? throw new InvalidOperationException("No Visual Studio instance available");
            MSBuildLocator.RegisterInstance(instance);
            return MSBuildWorkspace.Create(buildProps);
        }
    }
}
