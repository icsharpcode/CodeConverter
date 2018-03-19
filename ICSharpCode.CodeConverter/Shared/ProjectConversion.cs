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
    public class ProjectConversion<TLanguageConversion> where TLanguageConversion : ILanguageConversion, new()
    {
        private readonly Compilation _sourceCompilation;
        private readonly IEnumerable<SyntaxTree> _syntaxTreesToConvert;
        private static readonly AdhocWorkspace AdhocWorkspace = new AdhocWorkspace();
        private readonly ConcurrentDictionary<string, Exception> _errors = new ConcurrentDictionary<string, Exception>();
        private readonly Dictionary<string, SyntaxTree> _firstPassResults = new Dictionary<string, SyntaxTree>();
        private readonly TLanguageConversion _languageConversion;

        private ProjectConversion(Compilation sourceCompilation, string solutionDir)
            : this(sourceCompilation, sourceCompilation.SyntaxTrees.Where(t => t.FilePath.StartsWith(solutionDir)))
        {
        }

        private ProjectConversion(Compilation sourceCompilation, IEnumerable<SyntaxTree> syntaxTreesToConvert)
        {
            _languageConversion = new TLanguageConversion();
            _sourceCompilation = sourceCompilation;
            _syntaxTreesToConvert = syntaxTreesToConvert;
        }

        public static ConversionResult ConvertText(string text, IReadOnlyCollection<MetadataReference> references)
        {
            var languageConversion = new TLanguageConversion();
            var syntaxTree = languageConversion.CreateTree(text);
            var compilation = languageConversion.CreateCompilationFromTree(syntaxTree, references);
            return ConvertSingle(compilation, syntaxTree, new TextSpan(0, 0)).GetAwaiter().GetResult();
        }

        public static async Task<ConversionResult> ConvertSingle(Compilation compilation, SyntaxTree syntaxTree, TextSpan selected)
        {
            if (selected.Length > 0) {
                var annotatedSyntaxTree = await GetSyntaxTreeWithAnnotatedSelection(syntaxTree, selected);
                compilation = compilation.ReplaceSyntaxTree(syntaxTree, annotatedSyntaxTree);
                syntaxTree = annotatedSyntaxTree;
            }

            var conversion = new ProjectConversion<TLanguageConversion>(compilation, new [] {syntaxTree});
            return ConvertProject(conversion).Single();
        }

        public static IEnumerable<ConversionResult> ConvertProjects(IReadOnlyCollection<Project> projects)
        {
            var solutionDir = Path.GetDirectoryName(projects.First().Solution.FilePath);
            foreach (var project in projects) {
                var compilation = project.GetCompilationAsync().GetAwaiter().GetResult();
                var projectConversion = new ProjectConversion<TLanguageConversion>(compilation, solutionDir);
                foreach (var conversionResult in ConvertProject(projectConversion)) yield return conversionResult;
            }
        }

        private static IEnumerable<ConversionResult> ConvertProject(ProjectConversion<TLanguageConversion> projectConversion)
        {
            foreach (var pathNodePair in projectConversion.Convert())
            {
                var errors = projectConversion._errors.TryRemove(pathNodePair.Key, out var nonFatalException)
                    ? new[] {nonFatalException}
                    : new Exception[0];
                yield return new ConversionResult(pathNodePair.Value.ToFullString(), errors) { SourcePathOrNull = pathNodePair.Key };
            }

            foreach (var error in projectConversion._errors)
            {
                yield return new ConversionResult(error.Value) {SourcePathOrNull = error.Key};
            }
        }

        private Dictionary<string, SyntaxNode> Convert()
        {
            FirstPass();
            var secondPassByFilePath = SecondPass();
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
                    _errors.TryAdd(treeFilePath, e);
                }
            }
            return secondPassByFilePath;
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
                    var errorAnnotations = tree.GetRoot().GetAnnotations(TriviaConverter.ConversionErrorAnnotationKind);
                    _errors.TryAdd(treeFilePath,
                        new NotImplementedException(string.Join(Environment.NewLine,
                            errorAnnotations.Select(a => a.Data))));
                }
                catch (Exception e)
                {
                    _errors.TryAdd(treeFilePath, e);
                }
            }
        }

        private void SingleFirstPass(SyntaxTree tree, string treeFilePath)
        {
            var sourceCompilation = _sourceCompilation;
            var newTree = MakeFullCompilationUnit(tree);
            if (newTree != tree) {
                sourceCompilation = sourceCompilation.ReplaceSyntaxTree(tree, newTree);
                tree = newTree;
            }
            var convertedTree = _languageConversion.SingleFirstPass(sourceCompilation, tree);
            _firstPassResults.Add(treeFilePath, convertedTree);
        }

        private SyntaxTree MakeFullCompilationUnit(SyntaxTree tree)
        {
            var root = tree.GetRoot();
            var rootChildren = root.ChildNodes().ToList();
            var requiresSurroundingClass = rootChildren.Where(_languageConversion.MustBeContainedByClass).Any();
            var requiresSurroundingMethod = rootChildren.Where(_languageConversion.MustBeContainedByMethod).Any();

            if (requiresSurroundingMethod || requiresSurroundingClass) {
                var text = root.GetText().ToString();
                if (requiresSurroundingMethod) text = _languageConversion.WithSurroundingMethod(text);
                text = _languageConversion.WithSurroundingClass(text);

                var fullCompilationUnit = _languageConversion.CreateTree(text).GetRoot();

                var selectedNode = _languageConversion.GetSurroundedNode(fullCompilationUnit.DescendantNodes(), requiresSurroundingMethod);
                tree = fullCompilationUnit.WithAnnotatedNode(selectedNode, TriviaConverter.SelectedNodeAnnotationKind, TriviaConverter.AnnotatedNodeIsParentData);
            }

            return tree;
        }

        private static async Task<SyntaxTree> GetSyntaxTreeWithAnnotatedSelection(SyntaxTree syntaxTree, TextSpan selected)
        {
            var root = await syntaxTree.GetRootAsync();
            var selectedNode = root.FindNode(selected);
            return root.WithAnnotatedNode(selectedNode, TriviaConverter.SelectedNodeAnnotationKind);
        }

        private SyntaxNode Format(SyntaxNode resultNode)
        {
            SyntaxNode selectedNode = _firstPassResults.Count == 1 ? GetSelectedNode(resultNode) : resultNode;
            return Formatter.Format(selectedNode ?? resultNode, AdhocWorkspace);
        }

        private SyntaxNode GetSelectedNode(SyntaxNode resultNode)
        {
            var selectedNode = resultNode.GetAnnotatedNodes(TriviaConverter.SelectedNodeAnnotationKind)
                .SingleOrDefault();
            if (selectedNode != null)
            {
                var children = _languageConversion.FindSingleImportantChild(selectedNode);
                if (selectedNode.GetAnnotations(TriviaConverter.SelectedNodeAnnotationKind)
                        .Any(n => n.Data == TriviaConverter.AnnotatedNodeIsParentData)
                    && children.Count == 1)
                {
                    selectedNode = children.Single();
                }
            }

            return selectedNode ?? resultNode;
        }
    }
}