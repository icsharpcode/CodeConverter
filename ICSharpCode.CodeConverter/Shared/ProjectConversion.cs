using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Util;
using ICSharpCode.CodeConverter.VB;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;

namespace ICSharpCode.CodeConverter.Shared
{
    public class ProjectConversion
    {
        private readonly IReadOnlyCollection<Document> _documentsToConvert;
        private readonly ConcurrentDictionary<string, string> _errors = new ConcurrentDictionary<string, string>();
        private readonly Dictionary<string, Document> _firstPassResults = new Dictionary<string, Document>();
        private readonly Project _project;
        private readonly ILanguageConversion _languageConversion;
        private readonly bool _showCompilationErrors =
#if DEBUG && ShowCompilationErrors
            true;
#else
            false;
#endif
        private readonly bool _returnSelectedNode;
        private static readonly string[] BannedPaths = new[] { ".AssemblyAttributes.", "\\bin\\", "\\obj\\"};

        private ProjectConversion(Project project, IEnumerable<Document> documentsToConvert,
            ILanguageConversion languageConversion, bool returnSelectedNode = false)
        {
            _project = project;
            _languageConversion = languageConversion;
            _documentsToConvert = documentsToConvert.ToList();
            _returnSelectedNode = returnSelectedNode;
        }

        public static Task<ConversionResult> ConvertText<TLanguageConversion>(string text, IReadOnlyCollection<PortableExecutableReference> references, string rootNamespace = null) where TLanguageConversion : ILanguageConversion, new()
        {
            var languageConversion = new TLanguageConversion {
                RootNamespace = rootNamespace
            };
            var syntaxTree = languageConversion.MakeFullCompilationUnit(text, out var textSpan);
            using (var workspace = new AdhocWorkspace()) {
                var document = languageConversion.CreateProjectDocumentFromTree(workspace, syntaxTree, references);
                return ConvertSingle(document, textSpan ?? new TextSpan(0,0), languageConversion);
            }
        }

        public static async Task<ConversionResult> ConvertSingle(Document document, TextSpan selected, ILanguageConversion languageConversion)
        {
            bool returnSelectedNode = selected.Length > 0;
            if (returnSelectedNode) {
                document = await WithAnnotatedSelection(document, selected);
            }

            var conversion = new ProjectConversion(document.Project, new[] { document}, languageConversion, returnSelectedNode);
            await languageConversion.Initialize(document.Project);
            var conversionResults = (await ConvertProjectContents(conversion)).ToList();
            var codeResult = conversionResults.SingleOrDefault(x => !string.IsNullOrWhiteSpace(x.ConvertedCode))
                             ?? conversionResults.First();
            codeResult.Exceptions = conversionResults.SelectMany(x => x.Exceptions).ToArray();
            return codeResult;
        }

        [Obsolete("Use an alternate overload of ConvertSingle or ConvertText")]
        public static async Task<ConversionResult> ConvertSingle(Compilation compilation, SyntaxTree syntaxTree, TextSpan selected,
            ILanguageConversion languageConversion, Project containingProject = null)
        {
            if (containingProject != null) {
                return await ConvertSingle(containingProject.GetDocument(syntaxTree), selected, languageConversion);
            }
            using (var workspace = new AdhocWorkspace()) {
                var document = languageConversion.CreateProjectDocumentFromTree(workspace, syntaxTree, compilation.References);
                return await ConvertSingle(document, new TextSpan(0, 0), languageConversion);
            }
        }

        public static async Task<IEnumerable<ConversionResult>> ConvertProject(Project project, ILanguageConversion languageConversion,
            params (string, string)[] replacements)
        {
            return (await ConvertProjectContents(project, languageConversion)).Concat(new[]
                {ConvertProjectFile(project, languageConversion, replacements)}
            );
        }

        public static async Task<IEnumerable<ConversionResult>> ConvertProjectContents(Project project,
            ILanguageConversion languageConversion)
        {
            var documentsToConvert = project.Documents.Where(d => !BannedPaths.Any(d.FilePath.Contains));
            var projectConversion = new ProjectConversion(project, documentsToConvert, languageConversion);
            await languageConversion.Initialize(project);
            return await ConvertProjectContents(projectConversion);
        }

        public static ConversionResult ConvertProjectFile(Project project, ILanguageConversion languageConversion, params (string, string)[] textReplacements)
        {
            return new FileInfo(project.FilePath).ConversionResultFromReplacements(textReplacements, languageConversion.PostTransformProjectFile);
        }

        private static async Task<IEnumerable<ConversionResult>> ConvertProjectContents(ProjectConversion projectConversion)
        {
            var pathNodePairs = await Task.WhenAll(await projectConversion.Convert());
            var results = pathNodePairs.Select(pathNodePair => {
                var errors = projectConversion._errors.TryRemove(pathNodePair.Path, out var nonFatalException)
                    ? new[] {nonFatalException}
                    : new string[0];
                return new ConversionResult(pathNodePair.Node.ToFullString())
                    {SourcePathOrNull = pathNodePair.Path, Exceptions = errors};
            });

            await projectConversion.AddProjectWarnings();

            return results.Concat(projectConversion._errors
                .Select(error => new ConversionResult {SourcePathOrNull = error.Key, Exceptions = new[] {error.Value}})
            );
        }

        private async Task<IEnumerable<Task<(string Path, SyntaxNode Node)>>> Convert()
        {
            await FirstPass();
            return SecondPass();
        }

        private IEnumerable<Task<(string Path, SyntaxNode Node)>> SecondPass()
        {
            foreach (var firstPassResult in _firstPassResults) {
                yield return SingleSecondPassHandled(firstPassResult);
            }
        }

        private async Task<(string Key, SyntaxNode singleSecondPass)> SingleSecondPassHandled(KeyValuePair<string, Document> firstPassResult)
        {
            SyntaxNode selectedNode = null;
            try {
                var document = await firstPassResult.Value.WithSimplifiedSyntaxRootAsync();
                if (_returnSelectedNode) {
                    selectedNode = await GetSelectedNode(document);
                    selectedNode = Formatter.Format(selectedNode, document.Project.Solution.Workspace);
                } else {
                    selectedNode = await document.GetSyntaxRootAsync();
                    selectedNode = Formatter.Format(selectedNode, document.Project.Solution.Workspace);
                    var convertedDoc = document.WithSyntaxRoot(selectedNode);
                    selectedNode = await _languageConversion.SingleSecondPass(convertedDoc);
                }
            }
            catch (Exception e)
            {
                _errors.TryAdd(firstPassResult.Key, e.ToString());
            }

            return (firstPassResult.Key, selectedNode ?? await firstPassResult.Value.GetSyntaxRootAsync());
        }

        private async Task AddProjectWarnings()
        {
            if (!_showCompilationErrors) return;

            var nonFatalWarningsOrNull = await _languageConversion.GetWarningsOrNull();
            if (!string.IsNullOrWhiteSpace(nonFatalWarningsOrNull))
            {
                var warningsDescription = Path.Combine(_project.AssemblyName, "ConversionWarnings.txt");
                _errors.TryAdd(warningsDescription, nonFatalWarningsOrNull);
            }
        }

        private async Task FirstPass()
        {
            foreach (var document in _documentsToConvert)
            {
                var treeFilePath = document.FilePath ?? "";
                try {
                    var convertedDoc = await _languageConversion.SingleFirstPass(document);
                    _firstPassResults.Add(treeFilePath, convertedDoc);
                    var errorAnnotations = (await convertedDoc.GetSyntaxRootAsync()).GetAnnotations(AnnotationConstants.ConversionErrorAnnotationKind).ToList();
                    if (errorAnnotations.Any()) {
                        _errors.TryAdd(treeFilePath,
                            string.Join(Environment.NewLine, errorAnnotations.Select(a => a.Data))
                        );
                    }
                }
                catch (Exception e)
                {
                    _errors.TryAdd(treeFilePath, e.ToString());
                }
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