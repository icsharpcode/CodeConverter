using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        private readonly IProgress<ConversionProgress> _progress;
        private readonly ILanguageConversion _languageConversion;

        public static SolutionConverter CreateFor<TLanguageConversion>(IReadOnlyCollection<Project> projectsToConvert,
            IProgress<ConversionProgress> progress = null) where TLanguageConversion : ILanguageConversion, new()
        {
            var solutionFilePath = projectsToConvert.First().Solution.FilePath;
            var sourceSolutionContents = File.ReadAllText(solutionFilePath);
            var projectReferenceReplacements = GetProjectReferenceReplacements(projectsToConvert, sourceSolutionContents);
            return new SolutionConverter(solutionFilePath, sourceSolutionContents, projectsToConvert, projectReferenceReplacements, progress ?? new Progress<ConversionProgress>(), new TLanguageConversion());
        }

        private SolutionConverter(string solutionFilePath,
            string sourceSolutionContents, IReadOnlyCollection<Project> projectsToConvert,
            List<(string, string)> projectReferenceReplacements, IProgress<ConversionProgress> showProgressMessage,
            ILanguageConversion languageConversion)
        {
            _solutionFilePath = solutionFilePath;
            _sourceSolutionContents = sourceSolutionContents;
            _projectsToConvert = projectsToConvert;
            _projectReferenceReplacements = projectReferenceReplacements;
            _progress = showProgressMessage;
            _languageConversion = languageConversion;
        }

        public async Task<IEnumerable<ConversionResult>> Convert()
        {
            var projectsToUpdateReferencesOnly = _projectsToConvert.First().Solution.Projects.Except(_projectsToConvert);
            var projectContents = await ConvertProjects();
            return projectContents.SelectMany(x => x)
                .Concat(UpdateProjectReferences(projectsToUpdateReferencesOnly))
                .Concat(new[] { ConvertSolutionFile() });
        }

        private Task<IEnumerable<ConversionResult>[]> ConvertProjects()
        {
            var projectFileReplacementRegexes = _languageConversion.GetProjectFileReplacementRegexes().Concat(_languageConversion.GetProjectTypeGuidMappings());
            return Task.WhenAll(_projectsToConvert.Select(project => ConvertProject(projectFileReplacementRegexes, project)));
        }

        private async Task<IEnumerable<ConversionResult>> ConvertProject(IEnumerable<(string, string)> projectFileReplacementRegexes, Project project)
        {
            var replacements = _projectReferenceReplacements.Concat(projectFileReplacementRegexes).ToArray();
            _progress.Report($"Converting {project.Name}, this may take some time...");
            return await ProjectConversion.ConvertProject(project, _languageConversion, _progress, replacements);
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
                });
        }

        private static List<(string, string)> GetProjectReferenceReplacements(IReadOnlyCollection<Project> projectsToConvert,
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


        [Obsolete("Please use the overload with a IProgress<ConversionProgress> type")]
        public static SolutionConverter CreateFor<TLanguageConversion>(IReadOnlyCollection<Project> projectsToConvert,
            IProgress<string> progress) where TLanguageConversion : ILanguageConversion, new()
        {
            var showProgressMessage = new Progress<ConversionProgress>(p => progress?.Report(p.Message));
            return CreateFor<TLanguageConversion>(projectsToConvert, showProgressMessage);
        }
    }
}