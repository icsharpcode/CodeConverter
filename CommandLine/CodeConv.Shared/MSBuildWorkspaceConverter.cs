using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Common;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Util;
using ICSharpCode.CodeConverter.VB;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.VisualStudio.Threading;

namespace ICSharpCode.CodeConverter.CommandLine;

/// <summary>
/// Provides high-fidelity analysis of .NET solutions by mimicking the behavior of 'dotnet build'.
/// </summary>
public sealed class MsBuildWorkspaceConverter
{
    private readonly JoinableTaskFactory _joinableTaskFactory;
    private readonly SolutionLoader _solutionLoader;
    private AsyncLazy<Solution>? _cachedSolution;

    // The other parameters are ignored for compatibility
    public MsBuildWorkspaceConverter(JoinableTaskFactory joinableTaskFactory, string solutionFilePath, bool bestEffortConversion = false, Dictionary<string, string>? buildProps = null)
    {
        _joinableTaskFactory = joinableTaskFactory;
        _solutionLoader = new SolutionLoader(solutionFilePath, bestEffortConversion, buildProps);
    }


    public async IAsyncEnumerable<ConversionResult> ConvertProjectsWhereAsync(Func<Project, bool> shouldConvertProject, Language? targetLanguage, IProgress<ConversionProgress> progress, [EnumeratorCancellation] CancellationToken token)
    {
        var strProgress = new Progress<string>(s => progress.Report(new ConversionProgress(s)));
        _cachedSolution ??= new AsyncLazy<Solution>(async () => await _solutionLoader.AnalyzeSolutionAsync(strProgress), _joinableTaskFactory);

        var solution = await _cachedSolution.GetValueAsync(token);

        targetLanguage ??= solution.Projects.Any(p => p.Language == LanguageNames.VisualBasic) ? Language.CS : Language.VB;

        var languageConversion = targetLanguage == Language.CS
            ? (ILanguageConversion)new VBToCSConversion()
            : new CSToVBConversion();
        languageConversion.ConversionOptions = new ConversionOptions { AbandonOptionalTasksAfter = TimeSpan.FromHours(4) };
        var languageNameToConvert = targetLanguage == Language.CS
            ? LanguageNames.VisualBasic
            : LanguageNames.CSharp;

        var projectsToConvert = solution.Projects.Where(p => p.Language == languageNameToConvert && shouldConvertProject(p)).ToArray();
        var results = SolutionConverter.CreateFor(languageConversion, projectsToConvert, progress, token).ConvertAsync();
        await foreach (var r in results.WithCancellation(token)) yield return r;
    }

    private class SolutionLoader
    {
        private readonly string _solutionFilePath;
        private readonly bool _bestEffort;
        private readonly IDictionary<string, string> _buildProps;

        public SolutionLoader(string solutionFilePath, bool bestEffort, IDictionary<string, string>? buildProps)
        {
            _solutionFilePath = solutionFilePath;
            _bestEffort = bestEffort;
            _buildProps = buildProps ?? new Dictionary<string, string>();
        }

        public async Task<Solution> AnalyzeSolutionAsync(IProgress<string> progress, string configuration = "Debug")
        {
            progress.Report($"Running dotnet restore on {_solutionFilePath}");
            // === PREREQUISITE: Run 'dotnet restore' ===
            await RunDotnetRestoreAsync(_solutionFilePath);

            // === STEP 1: Create and Configure Workspace ===
            var properties = new Dictionary<string, string>(_buildProps)
            {
                { "Configuration", configuration },
                { "RunAnalyzers", "true" },
                { "RunAnalyzersDuringBuild", "true" }
            };

            using var workspace = MSBuildWorkspace.Create(properties);

            Solution solution = await workspace.OpenSolutionAsync(_solutionFilePath);

            var errorString = await GetCompilationErrorsAsync(workspace, solution.Projects);
            if (string.IsNullOrEmpty(errorString)) return solution;
            errorString = "    " + errorString.Replace(Environment.NewLine, Environment.NewLine + "    ");
            progress.Report($"Compilation errors found before conversion.:{Environment.NewLine}{errorString}");

            if (_bestEffort) {
                progress.Report("Attempting best effort conversion on broken input due to override");
            } else {
                throw CreateException("Fix compilation errors before conversion for an accurate conversion, or as a last resort, use the best effort conversion option", errorString);
            }

            return solution;

            ValidationException CreateException(string mainMessage, string fullDetail)
            {
                return new ValidationException($"{mainMessage}:{Environment.NewLine}{fullDetail}{Environment.NewLine}{mainMessage}");
            }
        }

        private async Task<string> GetCompilationErrorsAsync(MSBuildWorkspace workspace, IEnumerable<Project> projectsToConvert)
        {
            var workspaceErrors = workspace.Diagnostics.GetErrorString();
            var errors = await projectsToConvert.ParallelSelectAwaitAsync(async x => {
                var c = await x.GetCompilationAsync() ?? throw new InvalidOperationException($"Compilation could not be created for {x.Language}");
                return new[] { CompilationWarnings.WarningsForCompilation(c, c.AssemblyName) };
            }, Env.MaxDop).ToArrayAsync();
            var errorString = string.Join("\r\n", workspaceErrors.Yield().Concat(errors.SelectMany(w => w)).Where(w => w != null));
            return errorString;
        }

        private async Task RunDotnetRestoreAsync(string path)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"restore \"{path}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };
            var output = new StringBuilder();
            var error = new StringBuilder();
            process.OutputDataReceived += (sender, args) => { if (args.Data != null) output.AppendLine(args.Data); };
            process.ErrorDataReceived += (sender, args) => { if (args.Data != null) error.AppendLine(args.Data); };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"dotnet restore failed with exit code {process.ExitCode}.\n" +
                    $"Error: {error}");
            }
        }
    }
}