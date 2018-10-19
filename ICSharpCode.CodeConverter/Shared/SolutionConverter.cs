using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.CodeConverter.CSharp;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Shared
{
    public class SolutionConverter
    {
        private readonly string _solutionFilePath;
        private readonly string _sourceSolutionContents;
        private readonly IReadOnlyCollection<Project> _projectsToConvert;
        private readonly List<(string, string)> _projectReferenceReplacements;
        private readonly IProgress<string> _progress;
        private readonly ILanguageConversion _languageConversion;

        public static SolutionConverter CreateFor<TLanguageConversion>(Solution solution, IProgress<string> progress = null)
            where TLanguageConversion : ILanguageConversion, new()
        {
            return CreateFor<TLanguageConversion>(solution, solution.Projects, progress);
        }

        public static SolutionConverter CreateFor<TLanguageConversion>(Project projectToConvert,
            IProgress<string> progress = null)
            where TLanguageConversion : ILanguageConversion, new()
        {
            return CreateFor<TLanguageConversion>(null, new Project[] { projectToConvert }, progress);
        }

        public static SolutionConverter CreateFor<TLanguageConversion>(IEnumerable<Project> projectsToConvert,
            IProgress<string> progress = null)
            where TLanguageConversion : ILanguageConversion, new()
        {
            return CreateFor<TLanguageConversion>(null, projectsToConvert, progress);
        }

        private static SolutionConverter CreateFor<TLanguageConversion>(Solution solution,
            IEnumerable<Project> projectsToConvert, IProgress<string> progress)
            where TLanguageConversion : ILanguageConversion, new()
        {
            if (solution == null)
                solution = projectsToConvert.First().Solution;

            var solutionFilePath = solution == null ? null : solution.FilePath;
            var sourceSolutionContents = solutionFilePath == null ? null : File.ReadAllText(solutionFilePath);
            var projectReferenceReplacements = sourceSolutionContents == null
                ? null : GetProjectReferenceReplacements(projectsToConvert, sourceSolutionContents);
            return new SolutionConverter(solutionFilePath, sourceSolutionContents, projectsToConvert, projectReferenceReplacements, progress, new TLanguageConversion());
        }

        private SolutionConverter(string solutionFilePath,
            string sourceSolutionContents, IEnumerable<Project> projectsToConvert,
            List<(string, string)> projectReferenceReplacements, IProgress<string> showProgressMessage,
            ILanguageConversion languageConversion)
        {
            _solutionFilePath = solutionFilePath;
            _sourceSolutionContents = sourceSolutionContents;
            _projectsToConvert = projectsToConvert.ToList();
            _projectReferenceReplacements = projectReferenceReplacements;
            _progress = showProgressMessage ?? new Progress<string>();
            _languageConversion = languageConversion;
        }

        public IEnumerable<ConversionResult> Convert()
        {
            var projectsToUpdateReferencesOnly = _projectsToConvert.First().Solution.Projects.Except(_projectsToConvert);

            return ConvertProjects()
                    .Concat(UpdateProjectReferences(projectsToUpdateReferencesOnly))
                    .Concat(ConvertSolutionFile());
        }

        private IEnumerable<ConversionResult> ConvertProjects()
        {
            var projectFileReplacementRegexes = _languageConversion.GetProjectFileReplacementRegexes().Concat(_languageConversion.GetProjectTypeGuidMappings());
            return _projectsToConvert.SelectMany(project => ConvertProject(projectFileReplacementRegexes, project));
        }

        private IEnumerable<ConversionResult> ConvertProject(IEnumerable<(string, string)> projectFileReplacementRegexes, Project project)
        {
            var replacements = _projectReferenceReplacements == null ? null : _projectReferenceReplacements.Concat(projectFileReplacementRegexes).ToArray();
            _progress.Report($"Converting {project.Name}, this may take a some time...");
            return ProjectConversion.ConvertProjectContents(project, _languageConversion).Concat(
                replacements == null ? new ConversionResult[] { }
                : new[] { ConversionResultFromReplacements(project.FilePath, replacements, s => _languageConversion.PostTransformProjectFile(s)) }
            );
        }

        private IEnumerable<ConversionResult> UpdateProjectReferences(IEnumerable<Project> projectsToUpdateReferencesOnly)
        {
            if (_projectReferenceReplacements == null)
                return new ConversionResult[] { };

            return projectsToUpdateReferencesOnly.Select(project => {
                var withReferencesReplaced =
                    ConversionResultFromReplacements(project.FilePath, _projectReferenceReplacements);
                withReferencesReplaced.TargetPathOrNull = withReferencesReplaced.SourcePathOrNull;
                return withReferencesReplaced;
            });
        }

        private static List<(string, string)> GetProjectReferenceReplacements(IEnumerable<Project> projectsToConvert,
            string sourceSolutionContents)
        {
            var projectReferenceReplacements = new List<(string, string)>();
            foreach (var project in projectsToConvert)
            {
                var projFilename = Path.GetFileName(project.FilePath);
                var newProjFilename = PathConverter.TogglePathExtension(projFilename);
                projectReferenceReplacements.Add((projFilename, newProjFilename));
                projectReferenceReplacements.Add(GetProjectGuidReplacement(projFilename, sourceSolutionContents));
            }

            return projectReferenceReplacements;
        }

        private IEnumerable<ConversionResult> ConvertSolutionFile()
        {
            if (_sourceSolutionContents == null)
                yield break;

            var projectTypeGuidMappings = _languageConversion.GetProjectTypeGuidMappings();
            var projectTypeReplacements = _projectsToConvert.SelectMany(project => GetProjectTypeReplacement(project, projectTypeGuidMappings)).ToList();
            var convertedSolutionContents = ApplyReplacements(_sourceSolutionContents, _projectReferenceReplacements.Concat(projectTypeReplacements));
            yield return new ConversionResult(convertedSolutionContents) {
                SourcePathOrNull = _solutionFilePath,
                TargetPathOrNull = _solutionFilePath
            };
        }

        private static ConversionResult ConversionResultFromReplacements(string filePath, IEnumerable<(string, string)> replacements, Func<string, string> postReplacementTransform = null)
        {
            postReplacementTransform = postReplacementTransform ?? (s => s);
            var newProjectText = File.ReadAllText(filePath);
            newProjectText = ApplyReplacements(newProjectText, replacements);
            return new ConversionResult(postReplacementTransform(newProjectText)) {SourcePathOrNull = filePath};
        }

        private static string ApplyReplacements(string originalText, IEnumerable<(string, string)> replacements)
        {
            foreach (var (oldValue, newValue) in replacements)
            {
                originalText = Regex.Replace(originalText, oldValue, newValue, RegexOptions.IgnoreCase);
            }

            return originalText;
        }

        private static (string, string) GetProjectGuidReplacement(string projFilename, string contents)
        {
            var projGuidRegex = new Regex(projFilename + @""", ""({[0-9A-Fa-f\-]{32,36}})("")");
            var projGuidMatch = projGuidRegex.Match(contents);
            var oldGuid = projGuidMatch.Groups[1].Value;
            var newGuid = GetDeterministicGuidFrom(new Guid(oldGuid));
            return (oldGuid, newGuid.ToString("B").ToUpperInvariant());
        }

        private IEnumerable<(string, string)> GetProjectTypeReplacement(Project project, IReadOnlyCollection<(string, string)> typeGuidMappings)
        {
            return typeGuidMappings.Select(guidReplacement => ($@"Project\s*\(\s*""{guidReplacement.Item1}""\s*\)\s*=\s*""{project.Name}""", $@"Project(""{guidReplacement.Item2}"") = ""{project.Name}"""));
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