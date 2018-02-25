using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ICSharpCode.CodeConverter.CSharp
{
    public class ProjectConversion
    {
        private bool _methodBodyOnly;
        private Compilation _sourceCompilation;
        private IEnumerable<SyntaxTree> _syntaxTreesToConvert;
        private static readonly AdhocWorkspace AdhocWorkspace = new AdhocWorkspace();
        private readonly ConcurrentDictionary<string, Exception> _errors = new ConcurrentDictionary<string, Exception>();
        private readonly Dictionary<string, SyntaxTree> _firstPassResults = new Dictionary<string, SyntaxTree>();
        private Compilation _targetCompilation;

        private ProjectConversion(Compilation sourceCompilation, string solutionDir)
            : this(sourceCompilation, sourceCompilation.SyntaxTrees.Where(t => t.FilePath.StartsWith(solutionDir)))
        {
        }

        private ProjectConversion(Compilation sourceCompilation, IEnumerable<SyntaxTree> syntaxTreesToConvert)
        {
            _sourceCompilation = sourceCompilation;
            _syntaxTreesToConvert = syntaxTreesToConvert;
        }

        public static ConversionResult ConvertText(string text, IReadOnlyCollection<MetadataReference> references)
        {
            var syntaxTree = CreateTree(text);
            var compilation = CreateCompilationFromTree(syntaxTree, references);
            return ConvertSingle(compilation, syntaxTree, new TextSpan(0, 0)).GetAwaiter().GetResult();
        }

        public static async Task<ConversionResult> ConvertSingle(Compilation compilation, SyntaxTree syntaxTree, TextSpan selected)
        {
            var root = await syntaxTree.GetRootAsync();
            if (selected.Length > 0) {
                var annotatedSyntaxTree = GetSyntaxTreeWithAnnotatedSelection(selected, root);
                compilation = compilation.ReplaceSyntaxTree(syntaxTree, annotatedSyntaxTree);
                syntaxTree = annotatedSyntaxTree;
            }

            var conversion = new ProjectConversion(compilation, new [] {syntaxTree});
            var converted = conversion.Convert();

            if (!converted.Any()) {
                var conversionError = conversion._errors.Single();
                return new ConversionResult(conversionError.Value) { SourcePathOrNull = conversionError.Key };
            }
            var resultPair = converted.Single();
            var resultNode = GetSelectedNode(resultPair.Value);
            return new ConversionResult(resultNode.ToFullString()) { SourcePathOrNull = resultPair.Key };
        }

        public static IEnumerable<ConversionResult> ConvertProjects(IEnumerable<Project> projects)
        {
            var solutionDir = Path.GetDirectoryName(projects.First().Solution.FilePath);
            foreach (var project in projects) {
                var compilation = project.GetCompilationAsync().GetAwaiter().GetResult();
                var projectConversion = new ProjectConversion(compilation, solutionDir);
                foreach (var pathNodePair in projectConversion.Convert()) {
                    yield return new ConversionResult(pathNodePair.Value.ToFullString()) {SourcePathOrNull = pathNodePair.Key};
                }

                foreach (var error in projectConversion._errors) {
                    yield return new ConversionResult(error.Value) { SourcePathOrNull = error.Key };
                }
                
            }

        }

        private Dictionary<string, SyntaxNode> Convert()
        {
            FirstPass();
            _targetCompilation = CSharpCompilation.Create("Conversion", _firstPassResults.Values, _sourceCompilation.References);
            var secondPassByFilePath = _firstPassResults.ToDictionary(cs => cs.Key, SingleSecondPass);
            return secondPassByFilePath;
        }

        private void FirstPass()
        {
            foreach (var tree in _syntaxTreesToConvert)
            {
                var treeFilePath = tree.FilePath ?? "";
                try
                {
                    SingleFirstPass(tree, treeFilePath);
                }
                catch (NotImplementedOrRequiresSurroundingMethodDeclaration)
                    when (!_methodBodyOnly && _sourceCompilation.SyntaxTrees.Count() == 1)
                {
                    SingleFirstPassSurroundedByClassAndMethod(tree);
                }
                catch (Exception e)
                {
                    _errors.TryAdd(treeFilePath, e);
                }
            }
        }

        private void SingleFirstPassSurroundedByClassAndMethod(SyntaxTree tree)
        {
            var newTree = CreateTree(WithSurroundingClassAndMethod(tree.GetText().ToString()));
            _methodBodyOnly = true;
            _sourceCompilation = _sourceCompilation.AddSyntaxTrees(newTree);
            _syntaxTreesToConvert = new[] {newTree};
            Convert();
        }

        private static SyntaxTree GetSyntaxTreeWithAnnotatedSelection(TextSpan selected, SyntaxNode root)
        {
            var selectedNode = root.FindNode(selected);
            var annotatatedNode = selectedNode.WithAdditionalAnnotations(new SyntaxAnnotation(TriviaConverter.SelectedNodeAnnotationKind));
            return root.ReplaceNode(selectedNode, annotatatedNode).SyntaxTree;
        }

        private static SyntaxNode GetSelectedNode(SyntaxNode resultNode)
        {
            var annotatedNode = resultNode.GetAnnotatedNodes(TriviaConverter.SelectedNodeAnnotationKind).SingleOrDefault();
            return annotatedNode == null ? resultNode : Formatter.Format(annotatedNode, AdhocWorkspace);
        }

        private void SingleFirstPass(SyntaxTree tree, string treeFilePath)
        {
            var converted = VisualBasicConverter.ConvertCompilationTree((VisualBasicCompilation)_sourceCompilation, (VisualBasicSyntaxTree)tree);
            _firstPassResults.Add(treeFilePath, SyntaxFactory.SyntaxTree(converted));
        }

        private SyntaxNode SingleSecondPass(KeyValuePair<string, SyntaxTree> cs)
        {
            var secondPassNode = new CompilationErrorFixer((CSharpCompilation)_targetCompilation, (CSharpSyntaxTree)cs.Value).Fix();
            if (_methodBodyOnly) secondPassNode = secondPassNode.DescendantNodes().OfType<MethodDeclarationSyntax>().First().Body;
            return Formatter.Format(secondPassNode, AdhocWorkspace);
        }

        private static string WithSurroundingClassAndMethod(string text)
        {
            return $@"Class SurroundingClass
Sub SurroundingSub()
{text}
End Sub
End Class";
        }

        private static SyntaxTree CreateTree(string text)
        {
            return Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory.ParseSyntaxTree(SourceText.From(text));
        }

        private static Compilation CreateCompilationFromTree(SyntaxTree tree, IEnumerable<MetadataReference> references)
        {
            var compilationOptions = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithRootNamespace("TestProject")
                .WithGlobalImports(GlobalImport.Parse("System", "System.Collections.Generic", "System.Linq",
                    "Microsoft.VisualBasic"));
            var compilation = VisualBasicCompilation.Create("Conversion", new[] {tree}, references)
                .WithOptions(compilationOptions);
            return compilation;
        }
    }
}