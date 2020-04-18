using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;

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
        private readonly CancellationToken _cancellationToken;

        public static SolutionConverter CreateFor<TLanguageConversion>(IReadOnlyCollection<Project> projectsToConvert,
            ConversionOptions conversionOptions = default,
            IProgress<ConversionProgress> progress = null,
            CancellationToken cancellationToken = default) where TLanguageConversion : ILanguageConversion, new()
        {
            var conversion = new TLanguageConversion {ConversionOptions = conversionOptions };
            return CreateFor(conversion, projectsToConvert, progress, cancellationToken);
        }

        public static SolutionConverter CreateFor(ILanguageConversion languageConversion, IReadOnlyCollection<Project> projectsToConvert,
            IProgress<ConversionProgress> progress,
            CancellationToken cancellationToken)
        {
            languageConversion.ConversionOptions ??= new ConversionOptions();
            var solutionFilePath = projectsToConvert.First().Solution.FilePath;
            var sourceSolutionContents = File.Exists(solutionFilePath) ? File.ReadAllText(solutionFilePath) : "";
            var projectReferenceReplacements = GetProjectReferenceReplacements(projectsToConvert, sourceSolutionContents);
            return new SolutionConverter(solutionFilePath, sourceSolutionContents, projectsToConvert, projectReferenceReplacements, progress ?? new Progress<ConversionProgress>(), cancellationToken, languageConversion);
        }

        private SolutionConverter(string solutionFilePath,
            string sourceSolutionContents, IReadOnlyCollection<Project> projectsToConvert,
            List<(string Find, string Replace, bool FirstOnly)> projectReferenceReplacements, IProgress<ConversionProgress> showProgressMessage,
            CancellationToken cancellationToken, ILanguageConversion languageConversion)
        {
            _solutionFilePath = solutionFilePath;
            _sourceSolutionContents = sourceSolutionContents;
            _projectsToConvert = projectsToConvert;
            _projectReferenceReplacements = projectReferenceReplacements;
            _progress = showProgressMessage;
            _languageConversion = languageConversion;
            _cancellationToken = cancellationToken;
        }

        public IAsyncEnumerable<ConversionResult> Convert()
        {
            var projectsToUpdateReferencesOnly = _projectsToConvert.First().Solution.Projects.Except(_projectsToConvert);
            var solutionResult = string.IsNullOrWhiteSpace(_sourceSolutionContents) ? Enumerable.Empty<ConversionResult>() : ConvertSolutionFile().Yield();
            return ConvertProjects()
                .Concat(UpdateProjectReferences(projectsToUpdateReferencesOnly).Concat(solutionResult).ToAsyncEnumerable());
        }

        private IAsyncEnumerable<ConversionResult> ConvertProjects()
        {
            return _projectsToConvert.ToAsyncEnumerable().SelectMany(project => ConvertProject(project));
        }

        private IAsyncEnumerable<ConversionResult> ConvertProject(Project project)
        {
            var replacements = _projectReferenceReplacements.ToArray();
            _progress.Report(new ConversionProgress($"Converting {project.Name}..."));
            return ProjectConversion.ConvertProject(project, _languageConversion, _progress, _cancellationToken, replacements);
        }

        private IEnumerable<ConversionResult> UpdateProjectReferences(IEnumerable<Project> projectsToUpdateReferencesOnly)
        {
            return projectsToUpdateReferencesOnly
                .Where(p => p.FilePath != null) //Some project types like Websites don't have a project file
                .Select(project => {
                    var withReferencesReplaced =
                        new FileInfo(project.FilePath).ConversionResultFromReplacements(_projectReferenceReplacements);
                    withReferencesReplaced.TargetPathOrNull = withReferencesReplaced.SourcePathOrNull;
                    return withReferencesReplaced;
                }).Where(c => !c.IsIdentity);
        }

        private static List<(string Find, string Replace, bool FirstOnly)> GetProjectReferenceReplacements(IReadOnlyCollection<Project> projectsToConvert,
            string sourceSolutionContents)
        {
            var projectReferenceReplacements = new List<(string Find, string Replace, bool FirstOnly)>();
            foreach (var project in projectsToConvert)
            {
                var projFilename = Path.GetFileName(project.FilePath);
                var newProjFilename = PathConverter.TogglePathExtension(projFilename);
                projectReferenceReplacements.Add((projFilename, newProjFilename, false));
                if (!string.IsNullOrWhiteSpace(sourceSolutionContents)) projectReferenceReplacements.Add(GetProjectGuidReplacement(projFilename, sourceSolutionContents));
            }

            return projectReferenceReplacements;
        }

        private ConversionResult ConvertSolutionFile()
        {
            var projectTypeGuidMappings = _languageConversion.GetProjectTypeGuidMappings();
            var projectTypeReplacements = _projectsToConvert.SelectMany(project => GetProjectTypeReplacement(project, projectTypeGuidMappings)).ToList();

            var convertedSolutionContents = _sourceSolutionContents.Replace(_projectReferenceReplacements.Concat(projectTypeReplacements));
            return new ConversionResult(convertedSolutionContents) {
                SourcePathOrNull = _solutionFilePath,
                TargetPathOrNull = _solutionFilePath
            };
        }

        private static (string Find, string Replace, bool FirstOnly) GetProjectGuidReplacement(string projFilename, string contents)
        {
            var projGuidRegex = new Regex(projFilename + @""", ""({[0-9A-Fa-f\-]{32,36}})("")");
            var projGuidMatch = projGuidRegex.Match(contents);
            var oldGuid = projGuidMatch.Groups[1].Value;
            var newGuid = GetDeterministicGuidFrom(new Guid(oldGuid));
            return (oldGuid, newGuid.ToString("B").ToUpperInvariant(), false);
        }

        private IEnumerable<(string, string, bool)> GetProjectTypeReplacement(Project project, IReadOnlyCollection<(string, string)> typeGuidMappings)
        {
            return typeGuidMappings.Select(guidReplacement => ($@"Project\s*\(\s*""{guidReplacement.Item1}""\s*\)\s*=\s*""{project.Name}""", $@"Project(""{guidReplacement.Item2}"") = ""{project.Name}""", false));
        }

        private static Guid GetDeterministicGuidFrom(Guid guidToConvert)
        {
            var codeConverterStaticGuid = new Guid("{B224816B-CC58-4FF1-8258-CA7E629734A0}");
            var deterministicNewBytes = codeConverterStaticGuid.ToByteArray().Zip(guidToConvert.ToByteArray(),
                (fromFirst, fromSecond) => (byte)(fromFirst ^ fromSecond));
            return new Guid(deterministicNewBytes.ToArray());
        }
    }
}