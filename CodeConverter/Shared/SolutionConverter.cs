using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using System.IO.Abstractions;

namespace ICSharpCode.CodeConverter.Shared
{
    public class SolutionConverter
    {
        private readonly string _solutionFilePath;
        private readonly string _sourceSolutionContents;
        private readonly IReadOnlyCollection<Project> _projectsToConvert;
        private readonly List<(string Find, string Replace, bool FirstOnly)> _projectReferenceReplacements;
        private readonly IProgress<ConversionProgress> _progress;
        private readonly ILanguageConversion _languageConversion;
        private readonly SolutionFileTextEditor _solutionFileTextEditor;
        private readonly CancellationToken _cancellationToken;

        public static IFileSystem FileSystem { get; set; } = new FileSystem();

        public static SolutionConverter CreateFor<TLanguageConversion>(IReadOnlyCollection<Project> projectsToConvert, string sourceSolutionContents)
            where TLanguageConversion : ILanguageConversion, new()
        {
            return CreateFor<TLanguageConversion>(projectsToConvert, solutionContents: sourceSolutionContents);
        }

        public static SolutionConverter CreateFor<TLanguageConversion>(IReadOnlyCollection<Project> projectsToConvert,
            ConversionOptions conversionOptions = default,
            IProgress<ConversionProgress> progress = null,
            CancellationToken cancellationToken = default,
            string solutionContents = "") where TLanguageConversion : ILanguageConversion, new()
        {
            var conversion = new TLanguageConversion { ConversionOptions = conversionOptions };
            return CreateFor(conversion, projectsToConvert, progress, cancellationToken, solutionContents);
        }

        public static SolutionConverter CreateFor(ILanguageConversion languageConversion, IReadOnlyCollection<Project> projectsToConvert,
            IProgress<ConversionProgress> progress,
            CancellationToken cancellationToken, string solutionContents = "")
        {
            languageConversion.ConversionOptions ??= new ConversionOptions();
            var solutionFilePath = projectsToConvert.First().Solution.FilePath;
            var sourceSolutionContents = File.Exists(solutionFilePath)
                ? FileSystem.File.ReadAllText(solutionFilePath)
                : solutionContents;

            var projTuples = projectsToConvert.Select(proj =>
            {
                var relativeProjPath = PathConverter.GetRelativePath(solutionFilePath, proj.FilePath);
                var projContents = FileSystem.File.ReadAllText(proj.FilePath);

                return (proj.Name, RelativeProjPath: relativeProjPath, ProjContents: projContents);
            });

            var solutionFileTextEditor = new SolutionFileTextEditor();
            var projectReferenceReplacements = solutionFileTextEditor.GetProjectFileProjectReferenceReplacements(projTuples, sourceSolutionContents);

            return new SolutionConverter(solutionFilePath, sourceSolutionContents, projectsToConvert, projectReferenceReplacements,
                progress ?? new Progress<ConversionProgress>(), cancellationToken, languageConversion, solutionFileTextEditor);
        }

        private SolutionConverter(string solutionFilePath,
            string sourceSolutionContents, IReadOnlyCollection<Project> projectsToConvert,
            List<(string Find, string Replace, bool FirstOnly)> projectReferenceReplacements,
            IProgress<ConversionProgress> showProgressMessage,
            CancellationToken cancellationToken, ILanguageConversion languageConversion,
            SolutionFileTextEditor solutionFileTextEditor)
        {
            _solutionFilePath = solutionFilePath;
            _sourceSolutionContents = sourceSolutionContents;
            _projectsToConvert = projectsToConvert;
            _projectReferenceReplacements = projectReferenceReplacements;
            _progress = showProgressMessage;
            _languageConversion = languageConversion;
            _cancellationToken = cancellationToken;
            _solutionFileTextEditor = solutionFileTextEditor;
        }

        public async IAsyncEnumerable<ConversionResult> Convert()
        {
            var projectsToUpdateReferencesOnly = _projectsToConvert.First().Solution.Projects.Except(_projectsToConvert);
            var solutionResult = string.IsNullOrWhiteSpace(_sourceSolutionContents) ? Enumerable.Empty<ConversionResult>() : ConvertSolutionFile().Yield();
            var convertedProjects = await ConvertProjects();
            var projectsAndSolutionResults = UpdateProjectReferences(projectsToUpdateReferencesOnly).Concat(solutionResult).ToAsyncEnumerable();
            await foreach (var p in convertedProjects.Concat(projectsAndSolutionResults)) {
                yield return p;
            }
        }

        private async Task<IAsyncEnumerable<ConversionResult>> ConvertProjects()
        {
            var assemblies = _projectsToConvert.Select(t => t.GetCompilationAsync(_cancellationToken));
            var assembliesBeingConverted = (await Task.WhenAll(assemblies)).Select(t => t.Assembly).ToList();
            return _projectsToConvert.ToAsyncEnumerable().SelectMany(project => ConvertProject(project, assembliesBeingConverted));
        }

        private IAsyncEnumerable<ConversionResult> ConvertProject(Project project, IEnumerable<IAssemblySymbol> assembliesBeingConverted)
        {
            var replacements = _projectReferenceReplacements.ToArray();
            _progress.Report(new ConversionProgress($"Converting {project.Name}..."));
            return ProjectConversion.ConvertProject(project, _languageConversion, _progress, assembliesBeingConverted, _cancellationToken, replacements);
        }

        private IEnumerable<ConversionResult> UpdateProjectReferences(IEnumerable<Project> projectsToUpdateReferencesOnly)
        {
            var conversionResults = projectsToUpdateReferencesOnly
               .Where(p => p.FilePath != null) //Some project types like Websites don't have a project file
               .Select(project => {
                    var withReferencesReplaced =
                        new FileInfo(project.FilePath).ConversionResultFromReplacements(_projectReferenceReplacements);
                    withReferencesReplaced.TargetPathOrNull = withReferencesReplaced.SourcePathOrNull;
                    return withReferencesReplaced;
                });

            return conversionResults.Where(c => !c.IsIdentity);
        }

        public ConversionResult ConvertSolutionFile()
        {
            var projectTypeGuidMappings = _languageConversion.GetProjectTypeGuidMappings();
            var relativeProjPaths = _projectsToConvert.Select(proj =>
                (proj.Name, RelativeProjPath: PathConverter.GetRelativePath(_solutionFilePath, proj.FilePath)));

            var slnProjectReferenceReplacements = _solutionFileTextEditor.GetSolutionFileProjectReferenceReplacements(relativeProjPaths,
                _sourceSolutionContents, projectTypeGuidMappings);

            var convertedSolutionContents = _sourceSolutionContents.Replace(slnProjectReferenceReplacements);
            return new ConversionResult(convertedSolutionContents) {
                SourcePathOrNull = _solutionFilePath,
                TargetPathOrNull = _solutionFilePath
            };
        }
    }
}