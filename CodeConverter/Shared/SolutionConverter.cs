using System.IO.Abstractions;

namespace ICSharpCode.CodeConverter.Shared;

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
    private readonly TextReplacementConverter _textReplacementConverter;

    public static SolutionConverter CreateFor<TLanguageConversion>(IReadOnlyCollection<Project> projectsToConvert,
        ConversionOptions conversionOptions = default,
        IProgress<ConversionProgress> progress = null,
        CancellationToken cancellationToken = default,
        IFileSystem fileSystem = null,
        string solutionContents = "") where TLanguageConversion : ILanguageConversion, new()
    {
        var conversion = new TLanguageConversion { ConversionOptions = conversionOptions };
        return CreateFor(conversion, projectsToConvert, progress, cancellationToken, fileSystem, solutionContents);
    }

    public static SolutionConverter CreateFor(ILanguageConversion languageConversion, IReadOnlyCollection<Project> projectsToConvert,
        IProgress<ConversionProgress> progress, CancellationToken cancellationToken, IFileSystem fileSystem = null, string solutionContents = "")
    {
        languageConversion.ConversionOptions ??= new ConversionOptions();
        fileSystem ??= new FileSystem();

        var solutionFilePath = projectsToConvert.First().Solution.FilePath;
        var sourceSolutionContents = fileSystem.File.Exists(solutionFilePath)
            ? fileSystem.File.ReadAllText(solutionFilePath)
            : solutionContents;

        var projTuples = projectsToConvert.Select(proj =>
        {
            var relativeProjPath = PathConverter.GetRelativePath(solutionFilePath, proj.FilePath);
            var projContents = fileSystem.File.ReadAllText(proj.FilePath);

            return (proj.Name, RelativeProjPath: relativeProjPath, ProjContents: projContents);
        });

        var solutionFileTextEditor = new SolutionFileTextEditor();
        var projectReferenceReplacements = solutionFileTextEditor.GetProjectFileProjectReferenceReplacements(projTuples, sourceSolutionContents);

        return new SolutionConverter(solutionFilePath, sourceSolutionContents, projectsToConvert, projectReferenceReplacements,
            progress ?? new Progress<ConversionProgress>(), cancellationToken, languageConversion, solutionFileTextEditor, fileSystem);
    }

    private SolutionConverter(string solutionFilePath,
        string sourceSolutionContents, IReadOnlyCollection<Project> projectsToConvert,
        List<(string Find, string Replace, bool FirstOnly)> projectReferenceReplacements,
        IProgress<ConversionProgress> showProgressMessage,
        CancellationToken cancellationToken, ILanguageConversion languageConversion,
        SolutionFileTextEditor solutionFileTextEditor, IFileSystem fileSystem)
    {
        _solutionFilePath = solutionFilePath;
        _sourceSolutionContents = sourceSolutionContents;
        _projectsToConvert = projectsToConvert;
        _projectReferenceReplacements = projectReferenceReplacements;
        _progress = showProgressMessage;
        _languageConversion = languageConversion;
        _cancellationToken = cancellationToken;
        _solutionFileTextEditor = solutionFileTextEditor;
        _textReplacementConverter = new TextReplacementConverter(fileSystem);
    }

    public async IAsyncEnumerable<ConversionResult> Convert()
    {
        var projectsToUpdateReferencesOnly = _projectsToConvert.First().Solution.Projects.Except(_projectsToConvert);
        var solutionResult = string.IsNullOrWhiteSpace(_sourceSolutionContents) ? Enumerable.Empty<ConversionResult>() : ConvertSolutionFile().Yield();
        var convertedProjects = await ConvertProjectsAsync();
        var projectsAndSolutionResults = UpdateProjectReferences(projectsToUpdateReferencesOnly).Concat(solutionResult).ToAsyncEnumerable();
        await foreach (var p in convertedProjects.Concat(projectsAndSolutionResults)) {
            yield return p;
        }
    }

    private async Task<IAsyncEnumerable<ConversionResult>> ConvertProjectsAsync()
    {
        return _projectsToConvert.ToAsyncEnumerable().SelectMany(ConvertProject);
    }

    private IAsyncEnumerable<ConversionResult> ConvertProject(Project project)
    {
        var replacements = _projectReferenceReplacements.ToArray();
        _progress.Report(new ConversionProgress($"Begin converting {project.Name} at {DateTime.Now:HH:mm:ss}..."));
        return ProjectConversion.ConvertProject(project, _languageConversion, _textReplacementConverter, _progress, _cancellationToken, replacements);
    }

    private IEnumerable<ConversionResult> UpdateProjectReferences(IEnumerable<Project> projectsToUpdateReferencesOnly)
    {
        var conversionResults = projectsToUpdateReferencesOnly
            .Where(p => p.FilePath != null) //Some project types like Websites don't have a project file
            .Select(project => {
                var fileInfo = new FileInfo(project.FilePath);

                var withReferencesReplaced = 
                    _textReplacementConverter.ConversionResultFromReplacements(fileInfo, _projectReferenceReplacements);
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

        var slnProjectReferenceReplacements = SolutionFileTextEditor.GetSolutionFileProjectReferenceReplacements(relativeProjPaths,
            _sourceSolutionContents, projectTypeGuidMappings);

        var convertedSolutionContents = TextReplacementConverter.Replace(_sourceSolutionContents, slnProjectReferenceReplacements);
        return new ConversionResult(convertedSolutionContents) {
            SourcePathOrNull = _solutionFilePath,
            TargetPathOrNull = _solutionFilePath
        };
    }
}