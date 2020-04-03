using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using ICSharpCode.CodeConverter.VB;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.VisualBasic.FileIO;
using Microsoft.VisualStudio.Threading;

namespace ICSharpCode.CodeConverter.DotNetTool
{
    internal class MSBuildWorkspaceConverter
    {
        private readonly Lazy<MSBuildWorkspace> _msBuildWorkspace;
        private readonly AsyncLazy<Solution> _solution;
        private readonly IProgress<string> _progress;
        private readonly bool _bestEffortConversion;

        public MSBuildWorkspaceConverter(string solutionFilePath, IProgress<string> progress, bool bestEffortConversion = false)
        {
            _progress = progress;
            _bestEffortConversion = bestEffortConversion;
            _msBuildWorkspace = new Lazy<MSBuildWorkspace>(CreateWorkspace);
            _solution = new AsyncLazy<Solution>(() => GetSolutionAsync(solutionFilePath));
        }

        public async IAsyncEnumerable<ConversionResult> ConvertProjectsWhere(Func<Project, bool> shouldConvertProject, CodeConvProgram.Language targetLanguage, IProgress<ConversionProgress> progress, [EnumeratorCancellation] CancellationToken token)
        {
            var languageNameToConvert = targetLanguage == CodeConvProgram.Language.CS
                ? LanguageNames.VisualBasic
                : LanguageNames.CSharp;
            var languageConversion = targetLanguage == CodeConvProgram.Language.CS
                ? (ILanguageConversion) new VBToCSConversion()
                : new CSToVBConversion();

            var solution = await _solution.GetValueAsync();
            var projectsToConvert = solution.Projects.Where(p => p.Language == languageNameToConvert && shouldConvertProject(p)).ToArray();
            var results = SolutionConverter.CreateFor(languageConversion, projectsToConvert, progress, token).Convert();
            await foreach (var r in results) yield return r;
        }

        private async Task<Solution> GetSolutionAsync(string solutionFile)
        {
            await RestorePackagesForSolution(solutionFile);
            var solution = await _msBuildWorkspace.Value.OpenSolutionAsync(solutionFile);
            var errorString = await GetCompilationErrors(_msBuildWorkspace.Value.Diagnostics, solution.Projects);
            if (errorString != "") {
                _progress.Report($"Please fix compilation erorrs before conversion, or use the best effort conversion option:{Environment.NewLine}{errorString}");
                if (_bestEffortConversion) _progress.Report("Attempting best effort conversion on broken input due to override");
                else throw new InvalidOperationException($"Fix compilation erorrs before conversion for an accurate conversion, or use the best effort conversion option:{Environment.NewLine}{errorString}");
            }
            return solution;
        }

        /// <summary>
        /// If you've changed the source project not to compile, the results will be very confusing
        /// If this happens randomly, updating the Microsoft.Build dependency may help - it may have to line up with a version installed on the machine in some way.
        /// </summary>
        private static async Task<string> GetCompilationErrors(
            ImmutableList<WorkspaceDiagnostic> valueDiagnostics, IEnumerable<Project> projectsToConvert)
        {
            var errors = await projectsToConvert.ParallelSelectAwait(async x => {
                var c = await x.GetCompilationAsync();
                return new[] { CompilationWarnings.WarningsForCompilation(c, c.AssemblyName) }.Concat(
                    valueDiagnostics.Where(d => d.Kind > WorkspaceDiagnosticKind.Warning).Select(d => d.Message));
            }, Env.MaxDop, default).ToArrayAsync();
            var errorString = string.Join("\r\n", errors.SelectMany(w => w).Where(w => w != null));
            return errorString;
        }

        private static async Task RestorePackagesForSolution(string solutionFile)
        {
            var psi = new ProcessStartInfo("dotnet", $"restore \"{solutionFile}\"") { UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true };
            Process dotnetRestore = Process.Start(psi); //TODO Redirect stdout/stderr
            await dotnetRestore.WaitForExitAsync();
            if (dotnetRestore.ExitCode != 0) throw new InvalidOperationException($"dotnet restore had a non-zero exit code.");
        }

        private static MSBuildWorkspace CreateWorkspace()
        {
            try {
                return CreateWorkspaceUnhandled();
            } catch (NullReferenceException e) {
                throw new ApplicationException("MSBuild nullrefs on creation sometimes, please try again.", e);
            }
        }

        private static MSBuildWorkspace CreateWorkspaceUnhandled()
        {
            var instances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
            var instance = instances.OrderByDescending(x => x.Version).FirstOrDefault() 
                ?? throw new InvalidOperationException("Could not find version 16+ of Visual Studio");
            MSBuildLocator.RegisterInstance(instance);
            return MSBuildWorkspace.Create(new Dictionary<string, string>()
            {
                {"Configuration", "Debug"},
                {"Platform", "AnyCPU"}
            });
        }
    }

    public static class ConversionResultWriter
    {
        public static async Task WriteConvertedAsync(IAsyncEnumerable<ConversionResult> conversionResultsEnumerable, string solutionFilePath, string targetDirectoryPath, bool wipeTargetDirectory, bool copyOriginalDirectory)
        {
            var solutionFile = new FileInfo(solutionFilePath);
            var targetDirectory = targetDirectoryPath != null ? new DirectoryInfo(targetDirectoryPath) : solutionFile.Directory;
            var conversionResults = await conversionResultsEnumerable.ToDictionaryAsync(c => c.TargetPathOrNull, StringComparer.OrdinalIgnoreCase);

            if (wipeTargetDirectory) targetDirectory.Delete(true);
            if (copyOriginalDirectory) FileSystem.CopyDirectory(solutionFile.Directory.FullName, targetDirectory.FullName);

            foreach (var conversionResult in conversionResults) {
                var expectedFilePath =
                    conversionResult.Key.Replace(solutionFile.Directory.FullName, targetDirectory.FullName);
                Directory.CreateDirectory(Path.GetDirectoryName(expectedFilePath));
                File.WriteAllText(expectedFilePath, conversionResult.Value.ConvertedCode);
            }
        }
    }
}
