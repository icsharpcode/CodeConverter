using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter;
using ICSharpCode.CodeConverter.Common;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Util;
using ICSharpCode.CodeConverter.VB;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.VisualStudio.Threading;

namespace ICSharpCode.CodeConverter.CommandLine;

/// <summary>
/// Provides high-fidelity analysis of .NET solutions by mimicking the behavior of 'dotnet build'.
/// </summary>
public sealed class MsBuildWorkspaceConverter
{
    private readonly SolutionLoader _solutionLoader;
    private AsyncLazy<Solution>? _cachedSolution;

    // The other parameters are ignored for compatibility
    public MsBuildWorkspaceConverter(string solutionFilePath, bool bestEffortConversion = false, Dictionary<string, string>? buildProps = null)
    {
        _solutionLoader = new SolutionLoader(solutionFilePath, bestEffortConversion, buildProps);
    }


    public async IAsyncEnumerable<ConversionResult> ConvertProjectsWhereAsync(Func<Project, bool> shouldConvertProject, Language? targetLanguage, IProgress<ConversionProgress> progress, [EnumeratorCancellation] CancellationToken token)
    {
        var strProgress = new Progress<string>(s => progress.Report(new ConversionProgress(s)));
#pragma warning disable VSTHRD012 // Provide JoinableTaskFactory where allowed - Shouldn't need main thread, and I can't access ThreadHelper without referencing VS shell.
        _cachedSolution ??= new AsyncLazy<Solution>(async () => await _solutionLoader.AnalyzeSolutionAsync(strProgress));
#pragma warning restore VSTHRD012 // Provide JoinableTaskFactory where allowed
        var solution = await _cachedSolution.GetValueAsync(token);

        if (!targetLanguage.HasValue) {
            targetLanguage = solution.Projects.Any(p => p.Language == LanguageNames.VisualBasic) ? Language.CS : Language.VB;
        }

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
            workspace.WorkspaceFailed += HandleWorkspaceFailure;

            Solution solution;
            try
            {
                solution = await workspace.OpenSolutionAsync(_solutionFilePath);
            }
            finally
            {
                workspace.WorkspaceFailed -= HandleWorkspaceFailure;
            }

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

        private async Task<List<Diagnostic>> GetDiagnosticsAsync(Project project)
        {
            Compilation? compilation = await project.GetCompilationAsync();
            if (compilation is null) {
                var collection = Diagnostic.Create("FAIL", "Compilation", "Compilation is null", DiagnosticSeverity.Error, DiagnosticSeverity.Error, true, 3);
                return new List<Diagnostic> {collection};
            }

            ImmutableArray<Diagnostic> compileDiagnostics = compilation.GetDiagnostics();

            var analyzers = project.AnalyzerReferences
                .SelectMany(r => r.GetAnalyzersForAllLanguages())
                .ToImmutableArray();

            ImmutableArray<Diagnostic> analyzerDiagnostics = ImmutableArray<Diagnostic>.Empty;
            if (!analyzers.IsEmpty)
            {
                var compWithAnalyzers = compilation.WithAnalyzers(analyzers);
                analyzerDiagnostics = await compWithAnalyzers.GetAllDiagnosticsAsync();
            }

            var allDiagnostics = compileDiagnostics
                .Concat(analyzerDiagnostics)
                .ToList();

            return allDiagnostics;
        }

        private void HandleWorkspaceFailure(object? sender, WorkspaceDiagnosticEventArgs e)
        {
            if (e.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure && 
                !e.Diagnostic.Message.Contains("SDK Resolver Failure") &&
                !e.Diagnostic.Message.Contains(".NETFramework,Version=v4.8"))
            {
                var diagnostic = Diagnostic.Create(
                    id: e.Diagnostic.Kind.ToString(),
                    category: "Workspace",
                    message: e.Diagnostic.Message,
                    severity: DiagnosticSeverity.Error,
                    defaultSeverity: DiagnosticSeverity.Error,
                    isEnabledByDefault: true,
                    warningLevel: 0);
                _loadDiagnostics.Add(diagnostic);
            }
            else if (e.Diagnostic.Kind == WorkspaceDiagnosticKind.Warning)
            {
                var diagnostic = Diagnostic.Create(
                    id: e.Diagnostic.Kind.ToString(),
                    category: "Workspace",
                    message: e.Diagnostic.Message,
                    severity: DiagnosticSeverity.Warning,
                    defaultSeverity: DiagnosticSeverity.Warning,
                    isEnabledByDefault: true,
                    warningLevel: 1);
                _loadDiagnostics.Add(diagnostic);
            }
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