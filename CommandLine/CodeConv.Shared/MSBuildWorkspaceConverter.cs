using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Diagnostics;
using ICSharpCode.CodeConverter;
using ICSharpCode.CodeConverter.Common;

namespace ICSharpCode.CodeConverter.CommandLine;

/// <summary>
/// Provides high-fidelity analysis of .NET solutions by mimicking the behavior of 'dotnet build'.
/// </summary>
public sealed class MsBuildWorkspaceConverter
{
    private readonly string _solutionFilePath;
    // The other parameters are ignored for compatibility
    public MsBuildWorkspaceConverter(string solutionFilePath, bool isNetCore, object joinableTaskFactory, bool bestEffortConversion = false, Dictionary<string, string>? buildProps = null)
    {
        _solutionFilePath = solutionFilePath;
    }

    /// <summary>
    /// Maintains compatibility: yields a ConversionResult for each diagnostic in the solution.
    /// </summary>
    public async IAsyncEnumerable<ConversionResult> ConvertProjectsWhereAsync(
        Func<Project, bool> shouldConvertProject,
        Language? targetLanguage,
        IProgress<ConversionProgress> progress,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
    {
        var analysis = await AnalyzeSolutionAsync(_solutionFilePath);
        foreach (var projectResult in analysis.ProjectResults)
        {
            if (!shouldConvertProject(projectResult.Project)) continue;
            foreach (var diag in projectResult.Diagnostics)
            {
                var result = new ConversionResult(new Exception(diag.ToString()))
                {
                    SourcePathOrNull = projectResult.Project.FilePath
                };
                yield return result;
            }
        }
    }

    /// <summary>
    /// Analyzes a complete .NET solution (.sln) file and returns diagnostics for all projects.
    /// </summary>
    /// <param name="solutionPath">The absolute path to the .sln file.</param>
    /// <param name="configuration">The build configuration (e.g., "Debug" or "Release").</param>
    /// <returns>A SolutionAnalysisResult containing all projects and diagnostics.</returns>
    public async Task<SolutionAnalysisResult> AnalyzeSolutionAsync(string solutionPath, string configuration = "Debug")
    {
        var analyzer = new SolutionAnalyzer();
        return await analyzer.AnalyzeSolutionAsync(solutionPath, configuration);
    }

    /// <summary>
    /// A container for the results of a full solution analysis.
    /// </summary>
    public class SolutionAnalysisResult
    {
        public Solution Solution { get; set; } = null!;
        public List<ProjectAnalysisResult> ProjectResults { get; set; } = new List<ProjectAnalysisResult>();
        public IEnumerable<Diagnostic> AllDiagnostics => ProjectResults.SelectMany(p => p.Diagnostics);
    }

    /// <summary>
    /// A container for the results of a single project analysis.
    /// </summary>
    public class ProjectAnalysisResult
    {
        public Project Project { get; set; } = null!;
        public IReadOnlyList<Diagnostic> Diagnostics { get; set; } = Array.Empty<Diagnostic>();
    }

    private class SolutionAnalyzer
    {
        private readonly List<Diagnostic> _loadDiagnostics = new();

        public async Task<SolutionAnalysisResult> AnalyzeSolutionAsync(string solutionPath, string configuration = "Debug")
        {
            // === PREREQUISITE: Run 'dotnet restore' ===
            await RunDotnetRestoreAsync(solutionPath);

            _loadDiagnostics.Clear();

            // === STEP 1: Create and Configure Workspace ===
            var properties = new Dictionary<string, string>
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
                solution = await workspace.OpenSolutionAsync(solutionPath);
            }
            finally
            {
                workspace.WorkspaceFailed -= HandleWorkspaceFailure;
            }

            // === STEP 2: Analyze Each Project ===
            var projectResults = new List<ProjectAnalysisResult>();
            foreach (var project in solution.Projects)
            {
                var projectResult = await AnalyzeProjectAsync(project);
                projectResults.Add(projectResult);
            }

            // Include load diagnostics in the first project or create a dummy entry
            if (projectResults.Count > 0 && _loadDiagnostics.Any())
            {
                var firstProject = projectResults[0];
                projectResults[0] = new ProjectAnalysisResult
                {
                    Project = firstProject.Project,
                    Diagnostics = _loadDiagnostics.Concat(firstProject.Diagnostics).ToList()
                };
            }

            return new SolutionAnalysisResult
            {
                Solution = solution,
                ProjectResults = projectResults
            };
        }

        private async Task<ProjectAnalysisResult> AnalyzeProjectAsync(Project project)
        {
            Compilation? compilation = await project.GetCompilationAsync();
            if (compilation is null)
            {
                return new ProjectAnalysisResult
                {
                    Project = project,
                    Diagnostics = new List<Diagnostic>()
                };
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

            return new ProjectAnalysisResult
            {
                Project = project,
                Diagnostics = allDiagnostics
            };
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