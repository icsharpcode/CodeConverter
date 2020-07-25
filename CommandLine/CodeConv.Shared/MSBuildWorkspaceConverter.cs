using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
using McMaster.Extensions.CommandLineUtils;
using System.IO;
using ICSharpCode.CodeConverter.CommandLine.Util;
using Microsoft.VisualStudio.Threading;
using CodeConv.Shared.Util;
using System.ComponentModel.DataAnnotations;
using Microsoft.CodeAnalysis.Host.Mef;

namespace ICSharpCode.CodeConverter.CommandLine
{
    public sealed class MSBuildWorkspaceConverter : IDisposable
    {
        private readonly bool _bestEffortConversion;
        private readonly string _solutionFilePath;
        private readonly Dictionary<string, string> _buildProps;
        private readonly AsyncLazy<MSBuildWorkspace> _workspace; //Cached to avoid NullRef from OptionsService when initialized concurrently (e.g. in our tests)
        private AsyncLazy<Solution>? _cachedSolution; //Cached for performance of tests
        private readonly bool _isNetCore;

        public MSBuildWorkspaceConverter(string solutionFilePath, bool isNetCore, JoinableTaskFactory joinableTaskFactory, bool bestEffortConversion = false, Dictionary<string, string>? buildProps = null)
        {
            _bestEffortConversion = bestEffortConversion;
            _buildProps = buildProps ?? new Dictionary<string, string>();
            _buildProps.TryAdd("Configuration", "Debug");
            _buildProps.TryAdd("Platform", "AnyCPU");
            _solutionFilePath = solutionFilePath;
            _isNetCore = isNetCore;
            _workspace = new AsyncLazy<MSBuildWorkspace>(() => CreateWorkspaceAsync(_buildProps), joinableTaskFactory);
        }

        public async IAsyncEnumerable<ConversionResult> ConvertProjectsWhereAsync(Func<Project, bool> shouldConvertProject, Language? targetLanguage, IProgress<ConversionProgress> progress, [EnumeratorCancellation] CancellationToken token)
        {
            var strProgress = new Progress<string>(s => progress.Report(new ConversionProgress(s)));
#pragma warning disable VSTHRD012 // Provide JoinableTaskFactory where allowed - Shouldn't need main thread, and I can't access ThreadHelper without referencing VS shell.
            _cachedSolution ??= new AsyncLazy<Solution>(async () => await GetSolutionAsync(_solutionFilePath, strProgress));
#pragma warning restore VSTHRD012 // Provide JoinableTaskFactory where allowed
            var solution = await _cachedSolution.GetValueAsync();

            if (!targetLanguage.HasValue) {
                targetLanguage = solution.Projects.Any(p => p.Language == LanguageNames.VisualBasic) ? Language.CS : Language.VB;
            }

            var languageConversion = targetLanguage == Language.CS
                ? (ILanguageConversion)new VBToCSConversion()
                : new CSToVBConversion();
            var languageNameToConvert = targetLanguage == Language.CS
                ? LanguageNames.VisualBasic
                : LanguageNames.CSharp;

            var projectsToConvert = solution.Projects.Where(p => p.Language == languageNameToConvert && shouldConvertProject(p)).ToArray();
            var results = SolutionConverter.CreateFor(languageConversion, projectsToConvert, progress, token).Convert();
            await foreach (var r in results) yield return r;
        }

        private async Task<Solution> GetSolutionAsync(string projectOrSolutionFile, IProgress<string> progress)
        {
            progress.Report($"Running dotnet restore on {projectOrSolutionFile}");
            await RestorePackagesForSolutionAsync(projectOrSolutionFile);

            var workspace = await _workspace.GetValueAsync();
            var solution = string.Equals(Path.GetExtension(projectOrSolutionFile), ".sln", StringComparison.OrdinalIgnoreCase) ? await workspace.OpenSolutionAsync(projectOrSolutionFile)
                : (await workspace.OpenProjectAsync(projectOrSolutionFile)).Solution;

            var errorString = await GetCompilationErrorsAsync(solution.Projects);
            if (string.IsNullOrEmpty(errorString)) return solution;
            progress.Report($"Compilation errors found before conversion.:{Environment.NewLine}{errorString}");

            bool wrongFramework = new[] { "Type 'System.Void' is not defined", "is missing from assembly" }.Any(errorString.Contains);
            if (_bestEffortConversion) {
                progress.Report("Attempting best effort conversion on broken input due to override");
            } else if (wrongFramework && _isNetCore) {
                throw new ValidationException($"Compiling with dotnet core caused compilation errors, install VS2019+ or use the option `{CodeConvProgram.CoreOptionDefinition} false` to force attempted conversion with older versions (not recommended)");
            } else if (wrongFramework && !_isNetCore) {
                throw new ValidationException($"Compiling with .NET Framework MSBuild caused compilation errors, use the {CodeConvProgram.CoreOptionDefinition} true option if this is a .NET core only solution");
            } else {
                var mainMessage = "Fix compilation errors before conversion for an accurate conversion, or as a last resort, use the best effort conversion option";
                throw new ValidationException($"{mainMessage}:{Environment.NewLine}{errorString}{Environment.NewLine}{mainMessage}");
            }
            return solution;
        }

        private async Task<string> GetCompilationErrorsAsync(
            IEnumerable<Project> projectsToConvert)
        {
            var workspaceErrors = (await _workspace.GetValueAsync()).Diagnostics.GetErrorString();
            var errors = await projectsToConvert.ParallelSelectAwait(async x => {
                var c = await x.GetCompilationAsync() ?? throw new InvalidOperationException($"Compilation could not be created for {x.Language}");
                return new[] { CompilationWarnings.WarningsForCompilation(c, c.AssemblyName) };
            }, Env.MaxDop, default).ToArrayAsync();
            var errorString = string.Join("\r\n", workspaceErrors.Yield().Concat(errors.SelectMany(w => w)).Where(w => w != null));
            return errorString;
        }

        private static async Task RestorePackagesForSolutionAsync(string solutionFile)
        {
            var restoreExitCode = await ProcessRunner.ConnectConsoleGetExitCodeAsync(DotNetExe.FullPathOrDefault(), "restore", solutionFile);
            if (restoreExitCode != 0) throw new ValidationException("dotnet restore had a non-zero exit code.");
        }

        private static async Task<MSBuildWorkspace> CreateWorkspaceAsync(Dictionary<string, string> buildProps)
        {
            if (MSBuildLocator.CanRegister) {
                var instances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
                var instance = instances.OrderByDescending(x => x.Version).FirstOrDefault()
                    ?? throw new ValidationException("No Visual Studio instance available");
                MSBuildLocator.RegisterInstance(instance);
                AppDomain.CurrentDomain.UseVersionAgnosticAssemblyResolution();
            }

            var hostServices = await ThreadSafeWorkspaceHelper.CreateHostServicesAsync(MSBuildMefHostServices.DefaultAssemblies);
            return MSBuildWorkspace.Create(buildProps, hostServices);
        }

        public void Dispose()
        {
            if (_workspace.IsValueCreated) _workspace.GetValueAsync().Dispose();
        }
    }
}
