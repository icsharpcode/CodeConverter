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

        public MSBuildWorkspaceConverter(string solutionFilePath)
        {
            _msBuildWorkspace = new Lazy<MSBuildWorkspace>(CreateWorkspace);
            _solution = new AsyncLazy<Solution>(() => GetSolutionAsync(solutionFilePath));
        }

        public async IAsyncEnumerable<ConversionResult> ConvertProjectsWhere<TLanguageConversion>(Func<Project, bool> shouldConvertProject, IProgress<ConversionProgress> progress, [EnumeratorCancellation] CancellationToken token) where TLanguageConversion : ILanguageConversion, new()
        {
            var languageNameToConvert = typeof(TLanguageConversion) == typeof(VBToCSConversion)
                ? LanguageNames.VisualBasic
                : LanguageNames.CSharp;

            var projectsToConvert = (await _solution.GetValueAsync()).Projects.Where(p => p.Language == languageNameToConvert && shouldConvertProject(p)).ToArray();
            var results = SolutionConverter.CreateFor<TLanguageConversion>(projectsToConvert, progress: progress, cancellationToken: token).Convert();
            await foreach (var r in results) yield return r;
        }

        private async Task<Solution> GetSolutionAsync(string solutionFile)
        {
            await RestorePackagesForSolution(solutionFile);
            var solution = await _msBuildWorkspace.Value.OpenSolutionAsync(solutionFile);
            await AssertMSBuildIsWorkingAndProjectsValid(_msBuildWorkspace.Value.Diagnostics, solution.Projects);
            return solution;
        }

        /// <summary>
        /// If you've changed the source project not to compile, the results will be very confusing
        /// If this happens randomly, updating the Microsoft.Build dependency may help - it may have to line up with a version installed on the machine in some way.
        /// </summary>
        private static async Task AssertMSBuildIsWorkingAndProjectsValid(
            ImmutableList<WorkspaceDiagnostic> valueDiagnostics, IEnumerable<Project> projectsToConvert)
        {
            var errors = await projectsToConvert.ParallelSelectAwait(async x => {
                var c = await x.GetCompilationAsync();
                return new[] { CompilationWarnings.WarningsForCompilation(c, c.AssemblyName) }.Concat(
                    valueDiagnostics.Where(d => d.Kind > WorkspaceDiagnosticKind.Warning).Select(d => d.Message));
            }, Env.MaxDop, default).ToArrayAsync();
            var errorString = string.Join("\r\n", errors.SelectMany(w => w).Where(w => w != null));
            if (errorString != "") throw new InvalidOperationException($"Compilation errors detected before conversion:{Environment.NewLine}{errorString}");
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
            var instances = MSBuildLocator.QueryVisualStudioInstances();
            var instance = instances.OrderByDescending(x => x.Version).FirstOrDefault(x => x.Version.Major >= 16) 
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
