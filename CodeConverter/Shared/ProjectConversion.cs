using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Threading;

namespace ICSharpCode.CodeConverter.Shared
{
    public class ProjectConversion
    {
        private readonly IProjectContentsConverter _projectContentsConverter;
        private readonly IReadOnlyCollection<Document> _documentsToConvert;
        private readonly IReadOnlyCollection<TextDocument> _additionalDocumentsToConvert;
        private readonly ILanguageConversion _languageConversion;
        private readonly bool _showCompilationErrors;
        private readonly bool _returnSelectedNode;
        private static readonly string[] BannedPaths = new[] { ".AssemblyAttributes.", "\\bin\\", "\\obj\\" };
        private readonly CancellationToken _cancellationToken;

        private ProjectConversion(IProjectContentsConverter projectContentsConverter, IEnumerable<Document> documentsToConvert, IEnumerable<TextDocument> additionalDocumentsToConvert,
            ILanguageConversion languageConversion, CancellationToken cancellationToken, bool showCompilationErrors, bool returnSelectedNode = false)
        {
            _projectContentsConverter = projectContentsConverter;
            _languageConversion = languageConversion;
            _documentsToConvert = documentsToConvert.ToList();
            _additionalDocumentsToConvert = additionalDocumentsToConvert.ToList();
            _showCompilationErrors = showCompilationErrors;
            _returnSelectedNode = returnSelectedNode;
            _cancellationToken = cancellationToken;
        }

        public static async Task<ConversionResult> ConvertTextAsync<TLanguageConversion>(string text, TextConversionOptions conversionOptions, IProgress<ConversionProgress> progress = null, CancellationToken cancellationToken = default) where TLanguageConversion : ILanguageConversion, new()
        {
            progress ??= new Progress<ConversionProgress>();
            using var roslynEntryPoint = await RoslynEntryPointAsync(progress);

            var languageConversion = new TLanguageConversion { ConversionOptions = conversionOptions };
            var syntaxTree = languageConversion.MakeFullCompilationUnit(text, out var textSpan);
            if (conversionOptions.SourceFilePath != null) syntaxTree = syntaxTree.WithFilePath(conversionOptions.SourceFilePath);
            if (textSpan.HasValue) conversionOptions.SelectedTextSpan = textSpan.Value;
            var document = await languageConversion.CreateProjectDocumentFromTreeAsync(syntaxTree, conversionOptions.References);
            return await ConvertSingleAsync<TLanguageConversion>(document, conversionOptions, progress, cancellationToken);
        }

        public static async Task<ConversionResult> ConvertSingleAsync<TLanguageConversion>(Document document, SingleConversionOptions conversionOptions, IProgress<ConversionProgress> progress = null, CancellationToken cancellationToken = default) where TLanguageConversion : ILanguageConversion, new()
        {
            progress ??= new Progress<ConversionProgress>();
            using var roslynEntryPoint = await RoslynEntryPointAsync(progress);

            var languageConversion = new TLanguageConversion { ConversionOptions = conversionOptions };

            bool returnSelectedNode = conversionOptions.SelectedTextSpan.Length > 0;
            if (returnSelectedNode) {
                document = await WithAnnotatedSelectionAsync(document, conversionOptions.SelectedTextSpan);
            }

            var projectContentsConverter = await languageConversion.CreateProjectContentsConverterAsync(document.Project, progress, cancellationToken);

            document = projectContentsConverter.SourceProject.GetDocument(document.Id);

            var conversion = new ProjectConversion(projectContentsConverter, new[] { document }, Enumerable.Empty<TextDocument>(), languageConversion, cancellationToken, conversionOptions.ShowCompilationErrors, returnSelectedNode);
            var conversionResults = await conversion.Convert(progress).ToArrayAsync();
            return GetSingleResultForDocument(conversionResults, document);
        }

        private static ConversionResult GetSingleResultForDocument(ConversionResult[] conversionResults, Document document)
        {
            var codeResult = conversionResults.First(r => r.SourcePathOrNull == document.FilePath);
            codeResult.Exceptions = conversionResults.SelectMany(x => x.Exceptions).ToArray();
            return codeResult;
        }

        public static async IAsyncEnumerable<ConversionResult> ConvertProject(Project project,
            ILanguageConversion languageConversion, IProgress<ConversionProgress> progress, [EnumeratorCancellation] CancellationToken cancellationToken,
            params (string Find, string Replace, bool FirstOnly)[] replacements)
        {
            progress ??= new Progress<ConversionProgress>();
            using var roslynEntryPoint = await RoslynEntryPointAsync(progress);

            var projectContentsConverter = await languageConversion.CreateProjectContentsConverterAsync(project, progress, cancellationToken);
            var sourceFilePaths = project.Documents.Concat(projectContentsConverter.SourceProject.AdditionalDocuments).Select(d => d.FilePath).ToImmutableHashSet();
            project = projectContentsConverter.SourceProject;
            var convertProjectContents = ConvertProjectContents(projectContentsConverter, languageConversion, progress, cancellationToken);

            var results = WithProjectFile(projectContentsConverter, languageConversion, sourceFilePaths, convertProjectContents, replacements);
            await foreach (var result in results) yield return result;
        }

        /// <remarks>Perf: Keep lazy so that we don't keep an extra copy of all files in memory at once</remarks>
        private static async IAsyncEnumerable<ConversionResult> WithProjectFile(IProjectContentsConverter projectContentsConverter, ILanguageConversion languageConversion, ImmutableHashSet<string> originalSourcePaths, IAsyncEnumerable<ConversionResult> convertProjectContents, (string Find, string Replace, bool FirstOnly)[] replacements)
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

            yield return ConvertProjectFile(project, languageConversion, replacementSpecs);
        }

        public static ConversionResult ConvertProjectFile(Project project,
            ILanguageConversion languageConversion,
            params (string Find, string Replace, bool FirstOnly)[] textReplacements)
        {
            return new FileInfo(project.FilePath).ConversionResultFromReplacements(textReplacements,
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


        private static async IAsyncEnumerable<ConversionResult> ConvertProjectContents(
            IProjectContentsConverter projectContentsConverter, ILanguageConversion languageConversion,
            IProgress<ConversionProgress> progress, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var documentsWithLengths = await projectContentsConverter.SourceProject.Documents
                .Where(d => !BannedPaths.Any(d.FilePath.Contains))
                .SelectAsync(async d => (Doc: d, Length: (await d.GetTextAsync()).Length));

            //Perf heuristic: Decrease memory pressure on the simplification phase by converting large files first https://github.com/icsharpcode/CodeConverter/issues/524#issuecomment-590301594
            var documentsToConvert = documentsWithLengths.OrderByDescending(d => d.Length).Select(d => d.Doc);

            var projectConversion = new ProjectConversion(projectContentsConverter, documentsToConvert, projectContentsConverter.SourceProject.AdditionalDocuments, languageConversion, cancellationToken, false);

            var results = projectConversion.Convert(progress);
            await foreach (var result in results) yield return result;
        }


        private async IAsyncEnumerable<ConversionResult> Convert(IProgress<ConversionProgress> progress)
        {
            var phaseProgress = StartPhase(progress, "Phase 1 of 2:");
            var firstPassResults = _documentsToConvert.ParallelSelectAwait(d => FirstPassLoggedAsync(d, phaseProgress), Env.MaxDop, _cancellationToken);
            var (proj1, docs1) = await _projectContentsConverter.GetConvertedProjectAsync(await firstPassResults.ToArrayAsync());

            var warnings = await GetProjectWarningsAsync(_projectContentsConverter.SourceProject, proj1);
            if (!string.IsNullOrWhiteSpace(warnings)) {
                var warningPath = Path.Combine(_projectContentsConverter.SourceProject.GetDirectoryPath(), "ConversionWarnings.txt");
                yield return new ConversionResult() { SourcePathOrNull = warningPath, Exceptions = new[] { warnings } };
            }

            phaseProgress = StartPhase(progress, "Phase 2 of 2:");
            var secondPassResults = proj1.GetDocuments(docs1).ParallelSelectAwait(d => SecondPassLoggedAsync(d, phaseProgress), Env.MaxDop, _cancellationToken);
            await foreach (var result in secondPassResults.Select(CreateConversionResult)) {
                yield return result;
            }
            await foreach (var result in _projectContentsConverter.GetAdditionalConversionResults(_additionalDocumentsToConvert, _cancellationToken)) {
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
                    selectedNode = Formatter.Format(selectedNode, document.Project.Solution.Workspace);
                    if (extraLeadingTrivia.Any(t => !t.IsWhitespaceOrEndOfLine())) selectedNode = selectedNode.WithPrependedLeadingTrivia(extraLeadingTrivia);
                    if (extraTrailingTrivia.Any(t => !t.IsWhitespaceOrEndOfLine())) selectedNode = selectedNode.WithAppendedTrailingTrivia(extraTrailingTrivia);
                } else {
                    selectedNode = await document.GetSyntaxRootAsync();
                    selectedNode = Formatter.Format(selectedNode, document.Project.Solution.Workspace);
                    var convertedDoc = document.WithSyntaxRoot(selectedNode);
                    selectedNode = await convertedDoc.GetSyntaxRootAsync();
                }
            } catch (Exception e) {
                errors = new[] { e.ToString() };
            }

            var convertedNode = selectedNode ?? await convertedDocument.GetSyntaxRootAsync();
            return (convertedNode, errors);
        }

        private async Task<string> GetProjectWarningsAsync(Project source, Project converted)
        {
            if (!_showCompilationErrors) return null;

            var sourceCompilation = await source.GetCompilationAsync();
            var convertedCompilation = await converted.GetCompilationAsync();
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
                return (treeFilePath, convertedNode, errors);
            } catch (Exception e) {
                return (treeFilePath, null, new[] { e.ToString() });
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
            var resultNode = await document.GetSyntaxRootAsync();
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

        private WipFileConversion<T> LogEnd<T>(WipFileConversion<T> convertedFile, string action, IProgress<string> progress)
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
            return convertedFile;
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
}