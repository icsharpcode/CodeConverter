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

        private static async Task InitializeWithNoSynchronizationContext(ILanguageConversion languageConversion, Project documentProject)
        {
            await new SynchronizationContextRemover();
            await languageConversion.Initialize(documentProject);
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
            await InitializeWithNoSynchronizationContext(languageConversion, document.Project);
            var conversionResults = (await ConvertProjectContents(conversion, new Progress<ConversionProgress>())).ToList();
            var codeResult = conversionResults.SingleOrDefault(x => !string.IsNullOrWhiteSpace(x.ConvertedCode))
                             ?? conversionResults.First();
            codeResult.Exceptions = conversionResults.SelectMany(x => x.Exceptions).ToArray();
            return codeResult;
        }

        public static async Task<IEnumerable<ConversionResult>> ConvertProject(Project project, ILanguageConversion languageConversion, IProgress<ConversionProgress> progress, params (string, string)[] replacements)
        {
            return (await ConvertProjectContents(project, progress, languageConversion)).Concat(new[]
                {ConvertProjectFile(project, languageConversion, replacements)}
            );
        }


        private static async Task<IEnumerable<ConversionResult>> ConvertProjectContents(Project project, IProgress<ConversionProgress> progress, ILanguageConversion languageConversion)
        {
            var documentsToConvert = project.Documents.Where(d => !BannedPaths.Any(d.FilePath.Contains));
            var projectConversion = new ProjectConversion(project, documentsToConvert, languageConversion);
            await InitializeWithNoSynchronizationContext(languageConversion, project);
            return await ConvertProjectContents(projectConversion, progress);
        }

        public static ConversionResult ConvertProjectFile(Project project,
            ILanguageConversion languageConversion,
            params (string, string)[] textReplacements)
        {
            return new FileInfo(project.FilePath).ConversionResultFromReplacements(textReplacements,
                languageConversion.PostTransformProjectFile);
        }

        private static async Task<IEnumerable<ConversionResult>> ConvertProjectContents(
            ProjectConversion projectConversion, IProgress<ConversionProgress> progress)
        {
            var pathNodePairs = await projectConversion.Convert(progress);
            var results = pathNodePairs.Select(pathNodePair => new ConversionResult(pathNodePair.Node.ToFullString())
                {SourcePathOrNull = pathNodePair.Path, Exceptions = pathNodePair.Errors.ToList() });

            var warnings = await projectConversion.GetProjectWarnings();
            if (warnings != null) {
                string projectFilePath = projectConversion._project.FilePath;
                string projectDir = projectFilePath != null ? Path.GetDirectoryName(projectFilePath) : projectConversion._project.AssemblyName;
                var warningPath = Path.Combine(projectDir, "ConversionWarnings.txt");
                results = results.Concat(new[]{new ConversionResult { SourcePathOrNull = warningPath, Exceptions = new[] { warnings } }});
            }

            return results;
        }

        private async Task<(string Path, SyntaxNode Node, string[] Errors)[]> Convert(
            IProgress<ConversionProgress> progress)
        {
            progress.Report(new ConversionProgress("Phase 1 of 2:"));
            var strProgress = new Progress<string>(m => progress.Report(new ConversionProgress(m, 1)));
            var firstPassResults = await _documentsToConvert.SelectAsync(d => FirstPass(d, strProgress), 1);
            progress.Report(new ConversionProgress("Phase 2 of 2:"));
            var secondPass = await firstPassResults.SelectAsync(r => SecondPass(r, strProgress), 1);
            return secondPass;
        }

        private async Task<(string Path, SyntaxNode Node, string[] Errors)> SecondPass(
            (string Path, Document Doc, string[] Errors) firstPassResult, IProgress<string> progress)
        {
            if (firstPassResult.Doc != null) {
                progress.Report(firstPassResult.Path);
                return await SingleSecondPassHandled(firstPassResult);
            }

            return (firstPassResult.Path, null, firstPassResult.Errors);
        }

        private async Task<(string treeFilePath, SyntaxNode convertedDoc, string[] errors)> SingleSecondPassHandled((string treeFilePath, Document convertedDoc, string[] errors) firstPassResult)
        {
            SyntaxNode selectedNode = null;
            string[] errors = new string[0];
            try {
                var document = await firstPassResult.convertedDoc.WithSimplifiedSyntaxRootAsync();
                if (_returnSelectedNode) {
                    selectedNode = await GetSelectedNode(document);
                    selectedNode = Formatter.Format(selectedNode, document.Project.Solution.Workspace);
                } else {
                    selectedNode = await document.GetSyntaxRootAsync();
                    selectedNode = Formatter.Format(selectedNode, document.Project.Solution.Workspace);
                    var convertedDoc = document.WithSyntaxRoot(selectedNode);
                    selectedNode = await _languageConversion.SingleSecondPass(convertedDoc);
                }
            } catch (Exception e) {
                errors = new[] {e.ToString()};
            }

            return (firstPassResult.treeFilePath, selectedNode ?? await firstPassResult.convertedDoc.GetSyntaxRootAsync(), firstPassResult.errors.Concat(errors).ToArray());
        }

        private async Task<string> GetProjectWarnings()
        {
            if (!_showCompilationErrors) return null;
            return await _languageConversion.GetWarningsOrNull();
        }

        private async Task<(string treeFilePath, Document convertedDoc, string[] errors)> FirstPass(Document document, IProgress<string> progress)
        {
            var treeFilePath = document.FilePath ?? "";
            progress.Report(treeFilePath);
            try {
                var convertedDoc = await _languageConversion.SingleFirstPass(document);
                var errorAnnotations = (await convertedDoc.GetSyntaxRootAsync()).GetAnnotations(AnnotationConstants.ConversionErrorAnnotationKind).ToList();
                string[] errors = errorAnnotations.Select(a => a.Data).ToArray();

                return (treeFilePath, convertedDoc, errors);
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

        #region ObsoletePublicApi
        
        [Obsolete("Please use the overload which passes an IProgress")]
        public static async Task<IEnumerable<ConversionResult>> ConvertProject(Project project, ILanguageConversion languageConversion,
            params (string, string)[] replacements)
        {
            return await ConvertProject(project, languageConversion, new Progress<ConversionProgress>(), replacements);
        }

        [Obsolete("Please use the overload which passes an IProgress")]
        public static async Task<IEnumerable<ConversionResult>> ConvertProjectContents(Project project,
            ILanguageConversion languageConversion)
        {
            return await ConvertProjectContents(project, new Progress<ConversionProgress>(), languageConversion);
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

        #endregion
    }
}