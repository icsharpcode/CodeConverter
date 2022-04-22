using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using ICSharpCode.CodeConverter.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace ICSharpCode.CodeConverter.Common;

public class ProjectConversion
{
    private readonly IProjectContentsConverter _projectContentsConverter;
    private readonly IReadOnlyCollection<Document> _documentsToConvert;
    private readonly IReadOnlyCollection<TextDocument> _additionalDocumentsToConvert;
    private readonly ILanguageConversion _languageConversion;
    private readonly bool _showCompilationErrors;
    private readonly bool _returnSelectedNode;
    private static readonly string[] BannedPaths = { ".AssemblyAttributes.", "\\bin\\", "\\obj\\" };
    private readonly CancellationToken _cancellationToken;

    private ProjectConversion(IProjectContentsConverter projectContentsConverter, IEnumerable<Document> documentsToConvert, IEnumerable<TextDocument> additionalDocumentsToConvert,
        ILanguageConversion languageConversion, CancellationToken cancellationToken)
    {
        _projectContentsConverter = projectContentsConverter;
        _languageConversion = languageConversion;
        _documentsToConvert = documentsToConvert.ToList();
        _additionalDocumentsToConvert = additionalDocumentsToConvert.ToList();
        if (languageConversion.ConversionOptions is SingleConversionOptions singleOptions) {
            _returnSelectedNode = singleOptions.SelectedTextSpan.Length > 0;
            _showCompilationErrors = singleOptions.ShowCompilationErrors;
        }

        _cancellationToken = cancellationToken;
    }

    public static async Task<ConversionResult> ConvertTextAsync<TLanguageConversion>(string text, TextConversionOptions conversionOptions, IProgress<ConversionProgress> progress = null, CancellationToken cancellationToken = default) where TLanguageConversion : ILanguageConversion, new()
    {
        using var roslynEntryPoint = await RoslynEntryPointAsync(progress ??= new Progress<ConversionProgress>());

        var languageConversion = new TLanguageConversion { ConversionOptions = conversionOptions };
        var syntaxTree = languageConversion.MakeFullCompilationUnit(text, out var textSpan);
        if (conversionOptions.SourceFilePath != null) syntaxTree = syntaxTree.WithFilePath(conversionOptions.SourceFilePath);
        if (textSpan.HasValue) conversionOptions.SelectedTextSpan = textSpan.Value;
        var document = await languageConversion.CreateProjectDocumentFromTreeAsync(syntaxTree, conversionOptions.References);
        return await ConvertSingleAsync<TLanguageConversion>(document, conversionOptions, progress, cancellationToken);
    }

    public static async Task<ConversionResult> ConvertSingleAsync<TLanguageConversion>(Document document, SingleConversionOptions conversionOptions, IProgress<ConversionProgress> progress = null, CancellationToken cancellationToken = default) where TLanguageConversion : ILanguageConversion, new()
    {
        if (conversionOptions.SelectedTextSpan is { Length: > 0 } span) {
            document = await WithAnnotatedSelectionAsync(document, span);
        }
        var conversionResults = await ConvertDocumentsAsync<TLanguageConversion>(new[] {document}, conversionOptions, progress, cancellationToken).ToArrayAsync(cancellationToken);
        var codeResult = conversionResults.First(r => r.SourcePathOrNull == document.FilePath);
        codeResult.Exceptions = conversionResults.SelectMany(x => x.Exceptions).ToArray();
        return codeResult;
    }

    public static async IAsyncEnumerable<ConversionResult> ConvertDocumentsAsync<TLanguageConversion>(
        IReadOnlyCollection<Document> documents, 
        ConversionOptions conversionOptions, 
        IProgress<ConversionProgress> progress = null, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default) where TLanguageConversion : ILanguageConversion, new()
    {
        using var roslynEntryPoint = await RoslynEntryPointAsync(progress ??= new Progress<ConversionProgress>());

        var languageConversion = new TLanguageConversion { ConversionOptions = conversionOptions };
            
        var project = documents.First().Project;
        var projectContentsConverter = await languageConversion.CreateProjectContentsConverterAsync(project, progress, cancellationToken);

        documents = documents.Select(doc => projectContentsConverter.SourceProject.GetDocument(doc.Id)).ToList();

        var conversion = new ProjectConversion(projectContentsConverter, documents, Enumerable.Empty<TextDocument>(), languageConversion, cancellationToken);
        await foreach (var result in conversion.ConvertAsync(progress).WithCancellation(cancellationToken)) yield return result;
    }

    public static async IAsyncEnumerable<ConversionResult> ConvertProjectAsync(Project project,
        ILanguageConversion languageConversion, TextReplacementConverter textReplacementConverter,
        IProgress<ConversionProgress> progress,
        [EnumeratorCancellation] CancellationToken cancellationToken,
        params (string Find, string Replace, bool FirstOnly)[] replacements)
    {
        using var roslynEntryPoint = await RoslynEntryPointAsync(progress ??= new Progress<ConversionProgress>());
        var projectContentsConverter = await languageConversion.CreateProjectContentsConverterAsync(project, progress, cancellationToken);
        var sourceFilePaths = project.Documents.Concat(projectContentsConverter.SourceProject.AdditionalDocuments).Select(d => d.FilePath).ToImmutableHashSet();
        var convertProjectContents = ConvertProjectContentsAsync(projectContentsConverter, languageConversion, progress, cancellationToken);

        var results = WithProjectFileAsync(projectContentsConverter, textReplacementConverter, languageConversion, sourceFilePaths, convertProjectContents, replacements);
        await foreach (var result in results.WithCancellation(cancellationToken)) yield return result;

        progress.Report(new ConversionProgress($"Finished converting {project.Name} at {DateTime.Now:HH:mm:ss}..."));
    }

    /// <remarks>Perf: Keep lazy so that we don't keep an extra copy of all files in memory at once</remarks>
    private static async IAsyncEnumerable<ConversionResult> WithProjectFileAsync(IProjectContentsConverter projectContentsConverter, TextReplacementConverter textReplacementConverter,
        ILanguageConversion languageConversion, ImmutableHashSet<string> originalSourcePaths,
        IAsyncEnumerable<ConversionResult> convertProjectContents, (string Find, string Replace, bool FirstOnly)[] replacements)
    {
        var project = projectContentsConverter.SourceProject;
        var projectDir = project.GetDirectoryPath();
        var addedTargetFiles = new List<string>();
        var sourceToTargetMap = new List<(string, string)>();
        var projectDirSlash = projectDir + Path.DirectorySeparatorChar;

        await foreach (var conversionResult in convertProjectContents) {
            yield return conversionResult;

            var sourceRelative = Path.GetFullPath(conversionResult.SourcePathOrNull).Replace(projectDirSlash, "");
            var targetRelative = Path.GetFullPath(conversionResult.TargetPathOrNull).Replace(projectDirSlash, "");
            sourceToTargetMap.Add((sourceRelative, targetRelative));

            if (!originalSourcePaths.Contains(conversionResult.SourcePathOrNull)) {
                var relativePath = Path.GetFullPath(conversionResult.TargetPathOrNull).Replace(projectDirSlash, "");
                addedTargetFiles.Add(relativePath);
            }
        }

        var sourceTargetReplacements = sourceToTargetMap.Select(m => (Regex.Escape(m.Item1), m.Item2));
        var languageSpecificReplacements = sourceTargetReplacements.Concat(languageConversion.GetProjectFileReplacementRegexes()).Concat(languageConversion.GetProjectTypeGuidMappings())
            .Select(m => (m.Item1, m.Item2, false));

        var replacementSpecs = languageSpecificReplacements.Concat(replacements).Concat(new[] {
            AddCompiledItemsRegexFromRelativePaths(addedTargetFiles),
            ChangeRootNamespaceRegex(projectContentsConverter.RootNamespace),
            ChangeLanguageVersionRegex(projectContentsConverter.LanguageVersion)
        }).ToArray();

        yield return ConvertProjectFile(project, languageConversion, textReplacementConverter, replacementSpecs);
    }

    public static ConversionResult ConvertProjectFile(Project project,
        ILanguageConversion languageConversion,
        TextReplacementConverter textReplacementConverter,
        params (string Find, string Replace, bool FirstOnly)[] textReplacements)
    {
        var fileInfo = new FileInfo(project.FilePath);

        return textReplacementConverter.ConversionResultFromReplacements(fileInfo, textReplacements,
            languageConversion.PostTransformProjectFile);
    }

    private static (string Find, string Replace, bool FirstOnly) ChangeLanguageVersionRegex(string languageVersion) {
        return (Find: new Regex(@"<\s*LangVersion\s*>[^<]*</\s*LangVersion\s*>").ToString(), Replace: $"<LangVersion>{languageVersion}</LangVersion>", FirstOnly: true);
    }

    private static (string Find, string Replace, bool FirstOnly) ChangeRootNamespaceRegex(string rootNamespace) {
        return (Find: new Regex(@"<\s*RootNamespace\s*>([^<]*)</\s*RootNamespace\s*>").ToString(), Replace: $"<RootNamespace>{rootNamespace}</RootNamespace>", FirstOnly: true);
    }

    private static (string Find, string Replace, bool FirstOnly) AddCompiledItemsRegexFromRelativePaths(
        IEnumerable<string> relativeFilePathsToAdd)
    {
        var addFilesRegex = new Regex(@"(\s*<\s*Compile\s*Include\s*=\s*"".*\.(vb|cs)"")");
        var addedFiles = string.Join("",
            relativeFilePathsToAdd.OrderBy(x => x).Select(f => $@"{Environment.NewLine}    <Compile Include=""{f}"" />"));
        var addFilesRegexSpec = (Find: addFilesRegex.ToString(), Replace: addedFiles + @"$1", FirstOnly: true);
        return addFilesRegexSpec;
    }


    private static async IAsyncEnumerable<ConversionResult> ConvertProjectContentsAsync(
        IProjectContentsConverter projectContentsConverter, ILanguageConversion languageConversion,
        IProgress<ConversionProgress> progress, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var documentsWithLengths = await projectContentsConverter.SourceProject.Documents
            .Where(d => !BannedPaths.Any(d.FilePath.Contains))
            .SelectAsync(async d => (Doc: d, (await d.GetTextAsync(cancellationToken)).Length));

        //Perf heuristic: Decrease memory pressure on the simplification phase by converting large files first https://github.com/icsharpcode/CodeConverter/issues/524#issuecomment-590301594
        var documentsToConvert = documentsWithLengths.OrderByDescending(d => d.Length).Select(d => d.Doc);

        var projectConversion = new ProjectConversion(projectContentsConverter, documentsToConvert, projectContentsConverter.SourceProject.AdditionalDocuments, languageConversion, cancellationToken);

        var results = projectConversion.ConvertAsync(progress);
        await foreach (var result in results.WithCancellation(cancellationToken)) yield return result;
    }


    private async IAsyncEnumerable<ConversionResult> ConvertAsync(IProgress<ConversionProgress> progress)
    {
        var phaseProgress = StartPhase(progress, "Phase 1 of 2:");
        var firstPassResults = _documentsToConvert.ParallelSelectAwaitAsync(d => FirstPassLoggedAsync(d, phaseProgress), Env.MaxDop, _cancellationToken);
        var (proj1, docs1) = await _projectContentsConverter.GetConvertedProjectAsync(await firstPassResults.ToArrayAsync(_cancellationToken));

        var warnings = await GetProjectWarningsAsync(_projectContentsConverter.SourceProject, proj1);
        if (!string.IsNullOrWhiteSpace(warnings)) {
            var warningPath = Path.Combine(_projectContentsConverter.SourceProject.GetDirectoryPath(), "ConversionWarnings.txt");
            yield return new ConversionResult { SourcePathOrNull = warningPath, Exceptions = new[] { warnings } };
        }

        phaseProgress = StartPhase(progress, "Phase 2 of 2:");
        var secondPassResults = proj1.GetDocuments(docs1).ParallelSelectAwaitAsync(d => SecondPassLoggedAsync(d, phaseProgress), Env.MaxDop, _cancellationToken);
        await foreach (var result in secondPassResults.Select(CreateConversionResult).WithCancellation(_cancellationToken)) {
            yield return result;
        }
        await foreach (var result in _projectContentsConverter.GetAdditionalConversionResultsAsync(_additionalDocumentsToConvert, _cancellationToken)) {
            yield return result;
        }
    }

    private ConversionResult CreateConversionResult(WipFileConversion<SyntaxNode> r)
    {
        return new ConversionResult(r.Wip?.ToFullString()) { SourcePathOrNull = r.SourcePath, TargetPathOrNull = r.TargetPath, Exceptions = r.Errors.ToList() };
    }

    private static Progress<string> StartPhase(IProgress<ConversionProgress> progress, string phaseTitle)
    {
        progress.Report(new ConversionProgress(phaseTitle));
        var strProgress = new Progress<string>(m => progress.Report(new ConversionProgress(m, 1)));
        return strProgress;
    }

    private async Task<WipFileConversion<SyntaxNode>> SecondPassLoggedAsync(WipFileConversion<Document> firstPassResult, IProgress<string> progress)
    {
        if (firstPassResult.Wip != null) {
            LogStart(firstPassResult.SourcePath, "simplification", progress);
            var (convertedNode, errors) = await SingleSecondPassHandledAsync(firstPassResult.Wip);
            var result = firstPassResult.With(convertedNode, firstPassResult.Errors.Concat(errors).Union(GetErrorsFromAnnotations(convertedNode)).ToArray());
            LogEnd(firstPassResult, "simplification", progress);
            return result;
        }

        return firstPassResult.With(default(SyntaxNode));
    }

    private async Task<(SyntaxNode convertedDoc, string[] errors)> SingleSecondPassHandledAsync(Document convertedDocument)
    {
        SyntaxNode selectedNode = null;
        string[] errors = Array.Empty<string>();
        try {
            Document document = await _languageConversion.SingleSecondPassAsync(convertedDocument);
            if (_returnSelectedNode) {
                selectedNode = await GetSelectedNodeAsync(document);
                var extraLeadingTrivia = selectedNode.GetFirstToken().GetPreviousToken().TrailingTrivia;
                var extraTrailingTrivia = selectedNode.GetLastToken().GetNextToken().LeadingTrivia;
                selectedNode = _projectContentsConverter.OptionalOperations.Format(selectedNode, document);
                if (extraLeadingTrivia.Any(t => !t.IsWhitespaceOrEndOfLine())) selectedNode = selectedNode.WithPrependedLeadingTrivia(extraLeadingTrivia);
                if (extraTrailingTrivia.Any(t => !t.IsWhitespaceOrEndOfLine())) selectedNode = selectedNode.WithAppendedTrailingTrivia(extraTrailingTrivia);
            } else {
                selectedNode = await document.GetSyntaxRootAsync(_cancellationToken);
                selectedNode = _projectContentsConverter.OptionalOperations.Format(selectedNode, document);
                var convertedDoc = document.WithSyntaxRoot(selectedNode);
                selectedNode = await convertedDoc.GetSyntaxRootAsync(_cancellationToken);
            }
        } catch (Exception e) {
            errors = new[] { e.ToString() };
        }

        var convertedNode = selectedNode ?? await convertedDocument.GetSyntaxRootAsync(_cancellationToken);
        return (convertedNode, errors);
    }

    private async Task<string> GetProjectWarningsAsync(Project source, Project converted)
    {
        if (!_showCompilationErrors) return null;

        var sourceCompilation = await source.GetCompilationAsync(_cancellationToken);
        var convertedCompilation = await converted.GetCompilationAsync(_cancellationToken);
        return CompilationWarnings.WarningsForCompilation(sourceCompilation, "source") + CompilationWarnings.WarningsForCompilation(convertedCompilation, "target");
    }

    private async Task<WipFileConversion<SyntaxNode>> FirstPassLoggedAsync(Document document, IProgress<string> progress)
    {
        var treeFilePath = document.FilePath ?? "";
        LogStart(treeFilePath, "conversion", progress);
        var result = await FirstPassAsync(document);
        LogEnd(result, "conversion", progress);
        return result;
    }

    private async Task<WipFileConversion<SyntaxNode>> FirstPassAsync(Document document)
    {
        var treeFilePath = document.FilePath ?? "";
        try {
            var convertedNode = await _projectContentsConverter.SingleFirstPassAsync(document);
            string[] errors = GetErrorsFromAnnotations(convertedNode);
            return new(treeFilePath, convertedNode, errors);
        } catch (Exception e) {
            return new(treeFilePath, null, new[] { e.ToString() });
        }
    }

    private static string[] GetErrorsFromAnnotations(SyntaxNode convertedNode)
    {
        var errorAnnotations = convertedNode.GetAnnotations(AnnotationConstants.ConversionErrorAnnotationKind).ToList();
        string[] errors = errorAnnotations.Select(a => a.Data).ToArray();
        return errors;
    }

    private static async Task<Document> WithAnnotatedSelectionAsync(Document document, TextSpan selected)
    {
        var root = await document.GetSyntaxRootAsync();
        var selectedNode = root.FindNode(selected);
        var withAnnotatedSelection = await root.WithAnnotatedNode(selectedNode, AnnotationConstants.SelectedNodeAnnotationKind).GetRootAsync();
        return document.WithSyntaxRoot(withAnnotatedSelection);
    }

    private async Task<SyntaxNode> GetSelectedNodeAsync(Document document)
    {
        var resultNode = await document.GetSyntaxRootAsync(_cancellationToken);
        var selectedNode = resultNode.GetAnnotatedNodes(AnnotationConstants.SelectedNodeAnnotationKind)
            .FirstOrDefault();
        if (selectedNode != null) {
            var children = _languageConversion.FindSingleImportantChild(selectedNode);
            if (selectedNode.GetAnnotations(AnnotationConstants.SelectedNodeAnnotationKind)
                    .Any(n => n.Data == AnnotationConstants.AnnotatedNodeIsParentData)
                && children.Count == 1) {
                selectedNode = children.Single();
            }
        }

        return selectedNode ?? resultNode;
    }

    private void LogStart(string filePath, string action, IProgress<string> progress)
    {
        var relativePath = PathRelativeToSolutionDir(filePath);
        progress.Report($"{relativePath} - {action} started");
    }

    private void LogEnd<T>(WipFileConversion<T> convertedFile, string action, IProgress<string> progress)
    {
        var indentedException = string.Join(Environment.NewLine, convertedFile.Errors)
            .Replace(Environment.NewLine, Environment.NewLine + "    ").TrimEnd();
        var relativePath = PathRelativeToSolutionDir(convertedFile.SourcePath);

        var containsErrors = !string.IsNullOrWhiteSpace(indentedException);
        string output;
        if (convertedFile.Wip == null) {
            output = $"{relativePath} - {action} failed:{Environment.NewLine}    {indentedException}";
        } else if (containsErrors) {
            output = $"{relativePath} - {action} has errors: {Environment.NewLine}    {indentedException}";
        } else {
            output = $"{relativePath} - {action} succeeded";
        }

        progress.Report(output);
    }

    private string PathRelativeToSolutionDir(string path)
    {
        return path.Replace(this._projectContentsConverter.SourceProject.Solution.GetDirectoryPath() + Path.DirectorySeparatorChar, "");
    }

    private static async Task<IDisposable> RoslynEntryPointAsync(IProgress<ConversionProgress> progress)
    {
        JoinableTaskFactorySingleton.EnsureInitialized();
        await new SynchronizationContextRemover();
        return RoslynCrashPreventer.Create(LogError);

        void LogError(object e) => progress.Report(new ConversionProgress($"https://github.com/dotnet/roslyn threw an exception: {e}"));
    }
}