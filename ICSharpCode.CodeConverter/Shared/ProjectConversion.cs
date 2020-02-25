using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace ICSharpCode.CodeConverter.Shared
{
    public class ProjectConversion
    {
        private readonly IReadOnlyCollection<Document> _documentsToConvert;
        private readonly ILanguageConversion _languageConversion;
        private readonly bool _showCompilationErrors =
#if DEBUG && ShowCompilationErrors
            true;
#else
            false;
#endif
        private readonly bool _returnSelectedNode;
        private static readonly string[] BannedPaths = new[] { ".AssemblyAttributes.", "\\bin\\", "\\obj\\" };
        private readonly IProjectContentsConverter _projectContentsConverter;
        private readonly CancellationToken _cancellationToken;

        private ProjectConversion(IProjectContentsConverter projectContentsConverter, IEnumerable<Document> documentsToConvert,
            ILanguageConversion languageConversion, CancellationToken cancellationToken, bool returnSelectedNode = false)
        {
            _projectContentsConverter = projectContentsConverter;
            _languageConversion = languageConversion;
            _documentsToConvert = documentsToConvert.ToList();
            _returnSelectedNode = returnSelectedNode;
            _cancellationToken = cancellationToken;
        }

        public static async Task<ConversionResult> ConvertText<TLanguageConversion>(string text, TextConversionOptions conversionOptions, IProgress<ConversionProgress> progress = null, CancellationToken cancellationToken = default) where TLanguageConversion : ILanguageConversion, new()
        {
            progress = progress ?? new Progress<ConversionProgress>();
            using var roslynEntryPoint = await RoslynEntryPoint(progress);

            var languageConversion = new TLanguageConversion { ConversionOptions = conversionOptions };
            var syntaxTree = languageConversion.MakeFullCompilationUnit(text, out var textSpan);
            if (textSpan.HasValue) conversionOptions.SelectedTextSpan = textSpan.Value;
            using var workspace = new AdhocWorkspace();
            var document = languageConversion.CreateProjectDocumentFromTree(workspace, syntaxTree, conversionOptions.References);
            return await ConvertSingle<TLanguageConversion>(document, conversionOptions, progress, cancellationToken);
        }

        public static async Task<ConversionResult> ConvertSingle<TLanguageConversion>(Document document, SingleConversionOptions conversionOptions, IProgress<ConversionProgress> progress = null, CancellationToken cancellationToken = default) where TLanguageConversion : ILanguageConversion, new()
        {
            progress = progress ?? new Progress<ConversionProgress>();
            using var roslynEntryPoint = await RoslynEntryPoint(progress);

            var languageConversion = new TLanguageConversion { ConversionOptions = conversionOptions };

            bool returnSelectedNode = conversionOptions.SelectedTextSpan.Length > 0;
            if (returnSelectedNode) {
                document = await WithAnnotatedSelection(document, conversionOptions.SelectedTextSpan);
            }

            var projectContentsConverter = await languageConversion.CreateProjectContentsConverter(document.Project);

            document = projectContentsConverter.Project.GetDocument(document.Id);

            var conversion = new ProjectConversion(projectContentsConverter, new[] { document }, languageConversion, cancellationToken, returnSelectedNode);
            var conversionResults = await conversion.Convert(progress).ToArrayAsync();
            var codeResult = conversionResults.SingleOrDefault(x => !string.IsNullOrWhiteSpace(x.ConvertedCode))
                             ?? conversionResults.First();
            codeResult.Exceptions = conversionResults.SelectMany(x => x.Exceptions).ToArray();
            return codeResult;
        }

        public static async IAsyncEnumerable<ConversionResult> ConvertProject(Project project,
            ILanguageConversion languageConversion, IProgress<ConversionProgress> progress, [EnumeratorCancellation] CancellationToken cancellationToken,
            params (string Find, string Replace, bool FirstOnly)[] replacements)
        {
            progress = progress ?? new Progress<ConversionProgress>();
            using var roslynEntryPoint = await RoslynEntryPoint(progress);

            var sourceFilePathsWithoutExtension = project.Documents.Select(f => f.FilePath).ToImmutableHashSet();
            var projectContentsConverter = await languageConversion.CreateProjectContentsConverter(project);
            project = projectContentsConverter.Project;
            var convertProjectContents = ConvertProjectContents(projectContentsConverter, languageConversion, progress, cancellationToken);
            var results = WithProjectFile(projectContentsConverter, languageConversion, sourceFilePathsWithoutExtension, convertProjectContents, replacements);
            await foreach (var result in results) yield return result;
        }

        /// <remarks>Perf: Keep lazy so that we don't keep all files in memory at once</remarks>
        private static async IAsyncEnumerable<ConversionResult> WithProjectFile(IProjectContentsConverter projectContentsConverter, ILanguageConversion languageConversion, ImmutableHashSet<string> originalSourcePaths, IAsyncEnumerable<ConversionResult> convertProjectContents, (string Find, string Replace, bool FirstOnly)[] replacements)
        {
            var project = projectContentsConverter.Project;
            var projectDir = project.GetDirectoryPath();
            var addedTargetFiles = new List<string>();

            await foreach (var conversionResult in convertProjectContents) {
                yield return conversionResult;
                if (!originalSourcePaths.Contains(conversionResult.SourcePathOrNull)) {
                    var relativePath = Path.GetFullPath(conversionResult.TargetPathOrNull).Replace(projectDir + Path.DirectorySeparatorChar, "");
                    addedTargetFiles.Add(relativePath);
                }
            }

            var replacementSpecs = replacements.Concat(new[] {
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
            return (Find: new Regex(@"<\s*LangVersion>(\d|\D)*</LangVersion\s*>").ToString(), Replace: $"<LangVersion>{languageVersion}</LangVersion>", FirstOnly: true);
        }

        private static (string Find, string Replace, bool FirstOnly) ChangeRootNamespaceRegex(string rootNamespace) {
            return (Find: new Regex(@"<\s*RootNamespace>(\d|\D)*</RootNamespace\s*>").ToString(), Replace: $"<RootNamespace>{rootNamespace}</RootNamespace>", FirstOnly: true);
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
            var documentsWithLengths = await projectContentsConverter.Project.Documents
                .Where(d => !BannedPaths.Any(d.FilePath.Contains))
                .SelectAsync(async d => (Doc: d, Length: (await d.GetTextAsync()).Length));

            //Perf heuristic: Decrease memory pressure on the simplification phase by converting large files first https://github.com/icsharpcode/CodeConverter/issues/524#issuecomment-590301594
            var documentsToConvert = documentsWithLengths.OrderByDescending(d => d.Length).Select(d => d.Doc);

            var projectConversion = new ProjectConversion(projectContentsConverter, documentsToConvert, languageConversion, cancellationToken);

            var results = projectConversion.Convert(progress);
            await foreach (var result in results) yield return result;
        }


        private async IAsyncEnumerable<ConversionResult> Convert(IProgress<ConversionProgress> progress)
        {
            var phaseProgress = StartPhase(progress, "Phase 1 of 2:");
            var firstPassResults = _documentsToConvert.ParallelSelectAsync(d => FirstPass(d, phaseProgress), _cancellationToken);
            var (proj1, docs1) = await _projectContentsConverter.GetConvertedProject(await firstPassResults.ToArrayAsync());

            var warnings = await GetProjectWarnings(_projectContentsConverter.Project, proj1);
            if (warnings != null) {
                var warningPath = Path.Combine(proj1.GetDirectoryPath(), "ConversionWarnings.txt");
                yield return new ConversionResult() { SourcePathOrNull = warningPath, Exceptions = new[] { warnings } };
            }

            phaseProgress = StartPhase(progress, "Phase 2 of 2:");
            var secondPassResults = proj1.GetDocuments(docs1).ParallelSelectAsync(d => SecondPass(d, phaseProgress), _cancellationToken);
            await foreach (var result in secondPassResults) {
                yield return new ConversionResult(result.Wip?.ToFullString()) { SourcePathOrNull = result.Path, Exceptions = result.Errors.ToList() };
            };
        }

        private static Progress<string> StartPhase(IProgress<ConversionProgress> progress, string phaseTitle)
        {
            progress.Report(new ConversionProgress(phaseTitle));
            var strProgress = new Progress<string>(m => progress.Report(new ConversionProgress(m, 1)));
            return strProgress;
        }

        private async Task<WipFileConversion<SyntaxNode>> SecondPass(WipFileConversion<Document> firstPassResult, IProgress<string> progress)
        {
            if (firstPassResult.Wip != null) {
                LogProgress(firstPassResult, "Simplifying", progress);
                var (convertedNode, errors) = await SingleSecondPassHandled(firstPassResult.Wip);
                return (firstPassResult.Path, convertedNode, firstPassResult.Errors.Concat(errors).Union(GetErrorsFromAnnotations(convertedNode)).ToArray());
            }

            return (firstPassResult.Path, null, firstPassResult.Errors);
        }

        private async Task<(SyntaxNode convertedDoc, string[] errors)> SingleSecondPassHandled(Document convertedDocument)
        {
            SyntaxNode selectedNode = null;
            string[] errors = Array.Empty<string>();
            try {
                Document document = await _languageConversion.SingleSecondPass(convertedDocument);
                if (_returnSelectedNode) {
                    selectedNode = await GetSelectedNode(document);
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

        private async Task<string> GetProjectWarnings(Project source, Project converted)
        {
            if (!_showCompilationErrors) return null;

            var sourceCompilation = await source.GetCompilationAsync();
            var convertedCompilation = await converted.GetCompilationAsync();
            return CompilationWarnings.WarningsForCompilation(sourceCompilation, "source") + CompilationWarnings.WarningsForCompilation(convertedCompilation, "target");
        }

        private async Task<WipFileConversion<SyntaxNode>> FirstPass(Document document, IProgress<string> progress)
        {
            var treeFilePath = document.FilePath ?? "";
            progress.Report(treeFilePath);
            try {
                var convertedNode = await _projectContentsConverter.SingleFirstPass(document);
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

        private static async Task<Document> WithAnnotatedSelection(Document document, TextSpan selected)
        {
            var root = await document.GetSyntaxRootAsync();
            var selectedNode = root.FindNode(selected);
            var withAnnotatedSelection = await root.WithAnnotatedNode(selectedNode, AnnotationConstants.SelectedNodeAnnotationKind).GetRootAsync();
            return document.WithSyntaxRoot(withAnnotatedSelection);
        }

        private async Task<SyntaxNode> GetSelectedNode(Document document)
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

        private void LogProgress(WipFileConversion<Document> convertedFile, string action, IProgress<string> progress)
        {
            var indentedException = string.Join(Environment.NewLine, convertedFile.Errors)
                .Replace(Environment.NewLine, Environment.NewLine + "    ").TrimEnd();
            var relativePath = PathRelativeToSolutionDir(convertedFile.Path ?? "unknown");

            var containsErrors = !string.IsNullOrWhiteSpace(indentedException);
            string output;
            if (convertedFile.Wip == null) {
                output = $"Failed {action.ToLower()} {relativePath}:{Environment.NewLine}    {indentedException}";
            } else if (containsErrors) {
                output = $"Error {action.ToLower()} {relativePath}:{Environment.NewLine}    {indentedException}";
            } else {
                output = $"{action} {relativePath}";
            }

            progress.Report(output);
        }

        private string PathRelativeToSolutionDir(string path)
        {
            return path.Replace(this._projectContentsConverter.Project.Solution.GetDirectoryPath() + Path.DirectorySeparatorChar, "");
        }

        private static async Task<IDisposable> RoslynEntryPoint(IProgress<ConversionProgress> progress)
        {
            await new SynchronizationContextRemover();
            return RoslynCrashPreventer.Create(LogError);

            void LogError(object e) => progress.Report(new ConversionProgress($"https://github.com/dotnet/roslyn threw an exception: {e}"));
        }
    }
}