using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly string _solutionDir;
        private readonly Compilation _sourceCompilation;
        private readonly IEnumerable<SyntaxTree> _syntaxTreesToConvert;
        // ReSharper disable once StaticMemberInGenericType - Stateless
        private static readonly AdhocWorkspace AdhocWorkspace = new AdhocWorkspace();
        private readonly ConcurrentDictionary<string, string> _errors = new ConcurrentDictionary<string, string>();
        private readonly Dictionary<string, SyntaxTree> _firstPassResults = new Dictionary<string, SyntaxTree>();
        private readonly ILanguageConversion _languageConversion;
        private readonly bool _handlePartialConversion;

        private ProjectConversion(Compilation sourceCompilation, string solutionDir, ILanguageConversion languageConversion)
            : this(sourceCompilation, sourceCompilation.SyntaxTrees.Where(t => t.FilePath.StartsWith(solutionDir)), languageConversion)
        {
            _solutionDir = solutionDir;
        }

        private ProjectConversion(Compilation sourceCompilation, IEnumerable<SyntaxTree> syntaxTreesToConvert, ILanguageConversion languageConversion)
        {
            _languageConversion = languageConversion;
            this._sourceCompilation = sourceCompilation;
            _syntaxTreesToConvert = syntaxTreesToConvert.ToList();
            _handlePartialConversion = _syntaxTreesToConvert.Count() == 1;
        }

        public static ConversionResult ConvertText<TLanguageConversion>(string text, IReadOnlyCollection<MetadataReference> references) where TLanguageConversion : ILanguageConversion, new()
        {
            var languageConversion = new TLanguageConversion();
            var syntaxTree = languageConversion.CreateTree(text);
            var compilation = languageConversion.CreateCompilationFromTree(syntaxTree, references);
            return ConvertSingle(compilation, syntaxTree, new TextSpan(0, 0), new TLanguageConversion()).GetAwaiter().GetResult();
        }

        public static async Task<ConversionResult> ConvertSingle(Compilation compilation, SyntaxTree syntaxTree, TextSpan selected, ILanguageConversion languageConversion)
        {
            if (selected.Length > 0) {
                var annotatedSyntaxTree = await GetSyntaxTreeWithAnnotatedSelection(syntaxTree, selected);
                compilation = compilation.ReplaceSyntaxTree(syntaxTree, annotatedSyntaxTree);
                syntaxTree = annotatedSyntaxTree;
            }

            var conversion = new ProjectConversion(compilation, new [] {syntaxTree}, languageConversion);
            var conversionResults = ConvertProjectContents(conversion).ToList();
            var codeResult = conversionResults.SingleOrDefault(x => !string.IsNullOrWhiteSpace(x.ConvertedCode)) 
                             ?? conversionResults.First();
            codeResult.Exceptions = conversionResults.SelectMany(x => x.Exceptions).ToArray();
            return codeResult;
        }

        public static IEnumerable<ConversionResult> ConvertProjectContents(Project project,
            ILanguageConversion languageConversion)
        {
            var solutionFilePath = project.Solution.FilePath;
            var solutionDir = Path.GetDirectoryName(solutionFilePath);
            var compilation = project.GetCompilationAsync().GetAwaiter().GetResult();
            var projectConversion = new ProjectConversion(compilation, solutionDir, languageConversion);
            foreach (var conversionResult in ConvertProjectContents(projectConversion)) yield return conversionResult;
        }

        private static IEnumerable<ConversionResult> ConvertProjectContents(ProjectConversion projectConversion)
        {
            foreach (var pathNodePair in projectConversion.Convert())
            {
                var errors = projectConversion._errors.TryRemove(pathNodePair.Key, out var nonFatalException)
                    ? new[] {nonFatalException}
                    : new string[0];
                yield return new ConversionResult(pathNodePair.Value.ToFullString()) { SourcePathOrNull = pathNodePair.Key, Exceptions = errors };
            }

            foreach (var error in projectConversion._errors)
            {
                yield return new ConversionResult {SourcePathOrNull = error.Key, Exceptions = new []{ error.Value } };
            }
        }

        private Dictionary<string, SyntaxNode> Convert()
        {
            FirstPass();
            var secondPassByFilePath = SecondPass();
#if DEBUG && ShowCompilationErrors
            AddProjectWarnings();
#endif
            return secondPassByFilePath;
        }

        private Dictionary<string, SyntaxNode> SecondPass()
        {
            var secondPassByFilePath = new Dictionary<string, SyntaxNode>();
            foreach (var firstPassResult in _firstPassResults) {
                var treeFilePath = firstPassResult.Key;
                try {
                    secondPassByFilePath.Add(treeFilePath, SingleSecondPass(firstPassResult));
                }  catch (Exception e) {
                    secondPassByFilePath.Add(treeFilePath, Format(firstPassResult.Value.GetRoot()));
                    _errors.TryAdd(treeFilePath, e.ToString());
                }
            }
            return secondPassByFilePath;
        }

        private void AddProjectWarnings()
        {
            var nonFatalWarningsOrNull = _languageConversion.GetWarningsOrNull();
            if (!string.IsNullOrWhiteSpace(nonFatalWarningsOrNull))
            {
                var warningsDescription = Path.Combine(_solutionDir ?? "", _sourceCompilation.AssemblyName, "ConversionWarnings.txt");
                _errors.TryAdd(warningsDescription, nonFatalWarningsOrNull);
            }
        }

        private SyntaxNode SingleSecondPass(KeyValuePair<string, SyntaxTree> cs)
        {
            var secondPassNode = _languageConversion.SingleSecondPass(cs);
            return Format(secondPassNode);
        }

        private void FirstPass()
        {
            foreach (var tree in _syntaxTreesToConvert)
            {
                var treeFilePath = tree.FilePath ?? "";
                try {
                    SingleFirstPass(tree, treeFilePath);
                    var errorAnnotations = tree.GetRoot().GetAnnotations(AnnotationConstants.ConversionErrorAnnotationKind).ToList();
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

        private void SingleFirstPass(SyntaxTree tree, string treeFilePath)
        {
            var currentSourceCompilation = this._sourceCompilation;
            var newTree = MakeFullCompilationUnit(tree);
            if (newTree != tree) {
                currentSourceCompilation = currentSourceCompilation.ReplaceSyntaxTree(tree, newTree);
                tree = newTree;
            }
            var convertedTree = _languageConversion.SingleFirstPass(currentSourceCompilation, tree);
            _firstPassResults.Add(treeFilePath, convertedTree);
        }

        private SyntaxTree MakeFullCompilationUnit(SyntaxTree tree)
        {
            if (!_handlePartialConversion) return tree;
            var root = tree.GetRoot();
            var rootChildren = root.ChildNodes().ToList();
            var requiresSurroundingClass = rootChildren.Any(_languageConversion.MustBeContainedByClass);
            var requiresSurroundingMethod = rootChildren.All(_languageConversion.CanBeContainedByMethod);

            if (requiresSurroundingMethod || requiresSurroundingClass) {
                var text = root.GetText().ToString();
                if (requiresSurroundingMethod) text = _languageConversion.WithSurroundingMethod(text);
                text = _languageConversion.WithSurroundingClass(text);

                var fullCompilationUnit = _languageConversion.CreateTree(text).GetRoot();

                var selectedNode = _languageConversion.GetSurroundedNode(fullCompilationUnit.DescendantNodes(), requiresSurroundingMethod);
                tree = fullCompilationUnit.WithAnnotatedNode(selectedNode, AnnotationConstants.SelectedNodeAnnotationKind, AnnotationConstants.AnnotatedNodeIsParentData);
            }

            return tree;
        }

        private static async Task<SyntaxTree> GetSyntaxTreeWithAnnotatedSelection(SyntaxTree syntaxTree, TextSpan selected)
        {
            var root = await syntaxTree.GetRootAsync();
            var selectedNode = root.FindNode(selected);
            return root.WithAnnotatedNode(selectedNode, AnnotationConstants.SelectedNodeAnnotationKind);
        }

        private SyntaxNode Format(SyntaxNode resultNode)
        {
            SyntaxNode selectedNode = _handlePartialConversion ? GetSelectedNode(resultNode) : resultNode;
            return Formatter.Format(selectedNode ?? resultNode, AdhocWorkspace);
        }

        private SyntaxNode GetSelectedNode(SyntaxNode resultNode)
        {
            var selectedNode = resultNode.GetAnnotatedNodes(AnnotationConstants.SelectedNodeAnnotationKind)
                .SingleOrDefault();
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