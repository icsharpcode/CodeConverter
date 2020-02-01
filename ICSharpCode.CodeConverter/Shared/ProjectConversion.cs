using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        private static readonly string[] BannedPaths = new[] { ".AssemblyAttributes.", "\\bin\\", "\\obj\\"};
        private readonly IProjectContentsConverter _projectContentsConverter;

        private ProjectConversion(IProjectContentsConverter projectContentsConverter, IEnumerable<Document> documentsToConvert,
            ILanguageConversion languageConversion, bool returnSelectedNode = false)
        {
            _projectContentsConverter = projectContentsConverter;
            _languageConversion = languageConversion;
            _documentsToConvert = documentsToConvert.ToList();
            _returnSelectedNode = returnSelectedNode;
        }

        public static async Task<ConversionResult> ConvertText<TLanguageConversion>(string text, TextConversionOptions conversionOptions) where TLanguageConversion : ILanguageConversion, new()
        {
            await new SynchronizationContextRemover();

            var languageConversion = new TLanguageConversion {ConversionOptions = conversionOptions};
            var syntaxTree = languageConversion.MakeFullCompilationUnit(text, out var textSpan);
            if (textSpan.HasValue) conversionOptions.SelectedTextSpan = textSpan.Value;
            using (var workspace = new AdhocWorkspace()) {
                var document = languageConversion.CreateProjectDocumentFromTree(workspace, syntaxTree, conversionOptions.References);
                return await ConvertSingle<TLanguageConversion>(document, conversionOptions);
            }
        }

        public static async Task<ConversionResult> ConvertSingle<TLanguageConversion>(Document document, SingleConversionOptions conversionOptions) where TLanguageConversion : ILanguageConversion, new()
        {
            await new SynchronizationContextRemover();

            var languageConversion = new TLanguageConversion { ConversionOptions = conversionOptions };

            bool returnSelectedNode = conversionOptions.SelectedTextSpan.Length > 0;
            if (returnSelectedNode) {
                document = await WithAnnotatedSelection(document, conversionOptions.SelectedTextSpan);
            }

            var projectContentsConverter = await languageConversion.CreateProjectContentsConverter(document.Project);

            document = projectContentsConverter.Project.GetDocument(document.Id);

            var conversion = new ProjectConversion(projectContentsConverter, new[] { document }, languageConversion, returnSelectedNode);
            var conversionResults = (await ConvertProjectContents(conversion, new Progress<ConversionProgress>())).ToList();
            var codeResult = conversionResults.SingleOrDefault(x => !string.IsNullOrWhiteSpace(x.ConvertedCode))
                             ?? conversionResults.First();
            codeResult.Exceptions = conversionResults.SelectMany(x => x.Exceptions).ToArray();
            return codeResult;
        }

        public static async Task<IEnumerable<ConversionResult>> ConvertProject(Project project,
            ILanguageConversion languageConversion, IProgress<ConversionProgress> progress,
            params (string Find, string Replace, bool FirstOnly)[] replacements)
        {
            await new SynchronizationContextRemover();

            var sourceFilePathsWithoutExtension = project.Documents.Select(f => f.FilePath).ToImmutableHashSet();
            var projectContentsConverter = await languageConversion.CreateProjectContentsConverter(project);
            project = projectContentsConverter.Project;
            var convertProjectContents = (await ConvertProjectContents(projectContentsConverter, languageConversion, progress)).ToArray();
            var projectPath = Path.GetFullPath(project.GetDirectoryPath());
            string[] relativeFilePathsToAdd =
                convertProjectContents.Select(r => r.SourcePathOrNull).Where(p => !sourceFilePathsWithoutExtension.Contains(p))
                    .Select(p => PathConverter.TogglePathExtension(Path.GetFullPath(p)).Replace(projectPath +"\\", ""))
                    .OrderBy(x => x).ToArray();

            var replacementSpecs = replacements.Concat(new[] {
                AddCompiledItemsRegexFromRelativePaths(relativeFilePathsToAdd),
                ChangeRootNamespaceRegex(projectContentsConverter.RootNamespace),
                ChangeLanguageVersionRegex(projectContentsConverter.LanguageVersion)
            }).ToArray();

            return convertProjectContents.Concat(new[]
                {ConvertProjectFile(project, languageConversion, replacementSpecs)}
            );
        }

        private static (string Find, string Replace, bool FirstOnly) ChangeLanguageVersionRegex(string languageVersion) {
            return (Find: new Regex(@"<\s*LangVersion>(\d|\D)*</LangVersion\s*>").ToString(), Replace: $"<LangVersion>{languageVersion}</LangVersion>", FirstOnly: true);
        }
        private static (string Find, string Replace, bool FirstOnly) ChangeRootNamespaceRegex(string rootNamespace) {
            return (Find: new Regex(@"<\s*RootNamespace>(\d|\D)*</RootNamespace\s*>").ToString(), Replace: $"<RootNamespace>{rootNamespace}</RootNamespace>", FirstOnly: true);
        }

        private static (string Find, string Replace, bool FirstOnly) AddCompiledItemsRegexFromRelativePaths(
            string[] relativeFilePathsToAdd)
        {
            var addFilesRegex = new Regex(@"(\s*<\s*Compile\s*Include\s*=\s*"".*\.(vb|cs)"")");
            var addedFiles = string.Join("",
                relativeFilePathsToAdd.Select(f => $@"{Environment.NewLine}    <Compile Include=""{f}"" />"));
            var addFilesRegexSpec = (Find: addFilesRegex.ToString(), Replace: addedFiles + @"$1", FirstOnly: true);
            return addFilesRegexSpec;
        }


        private static async Task<IEnumerable<ConversionResult>> ConvertProjectContents(
            IProjectContentsConverter projectContentsConverter, ILanguageConversion languageConversion,
            IProgress<ConversionProgress> progress)
        {
            var documentsToConvert = projectContentsConverter.Project.Documents.Where(d => !BannedPaths.Any(d.FilePath.Contains));
            var projectConversion = new ProjectConversion(projectContentsConverter, documentsToConvert, languageConversion);

            return await ConvertProjectContents(projectConversion, progress);
        }

        public static ConversionResult ConvertProjectFile(Project project,
            ILanguageConversion languageConversion,
            params (string Find, string Replace, bool FirstOnly)[] textReplacements)
        {
            return new FileInfo(project.FilePath).ConversionResultFromReplacements(textReplacements,
                languageConversion.PostTransformProjectFile);
        }

        private static async Task<IEnumerable<ConversionResult>> ConvertProjectContents(
            ProjectConversion projectConversion, IProgress<ConversionProgress> progress)
        {
            var pathNodePairs = await projectConversion.Convert(progress);
            var results = pathNodePairs.Select(pathNodePair => new ConversionResult(pathNodePair.Node?.ToFullString())
                {SourcePathOrNull = pathNodePair.Path, Exceptions = pathNodePair.Errors.ToList() });

            var warnings = await projectConversion.GetProjectWarnings(projectConversion._projectContentsConverter.Project, pathNodePairs);
            if (warnings != null) {
                string projectDir = projectConversion._projectContentsConverter.Project.GetDirectoryPath()
                                    ?? projectConversion._projectContentsConverter.Project.AssemblyName;
                var warningPath = Path.Combine(projectDir, "ConversionWarnings.txt");
                results = results.Concat(new[]{new ConversionResult { SourcePathOrNull = warningPath, Exceptions = new[] { warnings } }});
            }

            return results;
        }

        private async Task<(string Path, SyntaxNode Node, string[] Errors)[]> Convert(
            IProgress<ConversionProgress> progress)
        {
            var firstPassResults = await ExecutePhase(_documentsToConvert, FirstPass, progress, "Phase 1 of 2:");
            var (proj1, docs1) = await _projectContentsConverter.GetConvertedProject(firstPassResults);
            return await ExecutePhase(proj1.GetDocuments(docs1), SecondPass, progress, "Phase 2 of 2:");
        }

        private async Task<(string Path, SyntaxNode Node, string[] Errors)[]> ExecutePhase<T>(IEnumerable<T> parameters, Func<T, Progress<string>, Task<(string treeFilePath, SyntaxNode convertedDoc,
                string[] errors)>> executePass, IProgress<ConversionProgress> progress, string phaseTitle)
        {
            progress.Report(new ConversionProgress(phaseTitle));
            var strProgress = new Progress<string>(m => progress.Report(new ConversionProgress(m, 1)));
            return await parameters.ParallelSelectAsync(d => executePass(d, strProgress), Env.MaxDop);
        }

        private async Task<(string Path, SyntaxNode Node, string[] Errors)> SecondPass((string Path, Document document, string[] Errors) firstPassResult, IProgress<string> progress)
        {
            if (firstPassResult.document != null) {
                progress.Report(firstPassResult.Path);
                var (convertedNode, errors) = await SingleSecondPassHandled(firstPassResult.document);
                return (firstPassResult.Path, convertedNode, firstPassResult.Errors.Concat(errors).ToArray());
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
                errors = new[] {e.ToString()};
            }

            return (selectedNode ?? await convertedDocument.GetSyntaxRootAsync(), errors);
        }

        private async Task<string> GetProjectWarnings(Project source, (string Path, SyntaxNode Node, string[] Errors)[] converted)
        {
            if (!_showCompilationErrors) return null;

            var sourceCompilation = await source.GetCompilationAsync();
            var convertedCompilation = await (await _projectContentsConverter.GetConvertedProject(converted)).project.GetCompilationAsync();
            return CompilationWarnings.WarningsForCompilation(sourceCompilation, "source") + CompilationWarnings.WarningsForCompilation(convertedCompilation, "target");
        }

        private async Task<(string treeFilePath, SyntaxNode convertedDoc, string[] errors)> FirstPass(Document document, IProgress<string> progress)
        {
            var treeFilePath = document.FilePath ?? "";
            progress.Report(treeFilePath);
            try {
                var convertedNode = await _projectContentsConverter.SingleFirstPass(document);
                var errorAnnotations = convertedNode.GetAnnotations(AnnotationConstants.ConversionErrorAnnotationKind).ToList();
                string[] errors = errorAnnotations.Select(a => a.Data).ToArray();

                return (treeFilePath, convertedNode, errors);
            }
            catch (Exception e)
            {
                return (treeFilePath, null, new[]{e.ToString()});
            }
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
            if (selectedNode != null)
            {
                var children = _languageConversion.FindSingleImportantChild(selectedNode);
                if (selectedNode.GetAnnotations(AnnotationConstants.SelectedNodeAnnotationKind)
                        .Any(n => n.Data == AnnotationConstants.AnnotatedNodeIsParentData)
                    && children.Count == 1)
                {
                    selectedNode = children.Single();
                }
            }

            return selectedNode ?? resultNode;
        }
    }
}