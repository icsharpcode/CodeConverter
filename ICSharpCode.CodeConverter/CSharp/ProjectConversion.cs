using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace ICSharpCode.CodeConverter.CSharp
{
    public class ProjectConversion
    {
        private static readonly AdhocWorkspace AdhocWorkspace = new AdhocWorkspace();


        public static async Task<ConversionResult> ConvertSingle(Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation compilation, Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxTree syntaxTree, TextSpan selected)
        {
            var root = await syntaxTree.GetRootAsync();
            if (selected.Length > 0) {
                var annotatedSyntaxTree = GetSyntaxTreeWithAnnotatedSelection(selected, root);
                compilation = compilation.ReplaceSyntaxTree(syntaxTree, annotatedSyntaxTree);
                syntaxTree = (Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxTree) annotatedSyntaxTree;
            }

            try {
                var cSharpSyntaxNode = ConvertSingleUnhandled(compilation, syntaxTree);
                var annotatedNode = cSharpSyntaxNode.GetAnnotatedNodes(TriviaConverter.SelectedNodeAnnotationKind).SingleOrDefault();
                if (annotatedNode != null) cSharpSyntaxNode = (CSharpSyntaxNode) annotatedNode;
                var formattedNode = Formatter.Format(cSharpSyntaxNode, AdhocWorkspace);
                return new ConversionResult(formattedNode.ToFullString());
            } catch (Exception ex) {
                return new ConversionResult(ex);
            }
        }

        public static async Task ConvertProjects(IEnumerable<Project> projects)
        {
            foreach (var project in projects) {
                var compilation = await project.GetCompilationAsync();
                var syntaxTrees = project.Documents
                    .Where(d => Path.GetExtension(d.FilePath).Equals(".vb", StringComparison.OrdinalIgnoreCase))
                    .Select(d => d.GetSyntaxTreeAsync().GetAwaiter().GetResult())
                    .OfType<Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxTree>();
                foreach (var pathNodePair in ConvertMultiple((Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation)compilation, syntaxTrees)) {
                    var formattedNode = Formatter.Format(pathNodePair.Value, AdhocWorkspace);
                    var path = Path.ChangeExtension(pathNodePair.Key, ".cs");
                    File.WriteAllText(path, formattedNode.ToFullString());
                }
            }
        }

        public static ConversionResult ConvertText(string text, IReadOnlyCollection<MetadataReference> references)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));
            if (references == null)
                throw new ArgumentNullException(nameof(references));
            
            try {
                var cSharpSyntaxNode = ConvertTextUnhandled(references, text);
                var formattedNode = Formatter.Format(cSharpSyntaxNode, AdhocWorkspace);
                return new ConversionResult(formattedNode.ToFullString());
            } catch (Exception ex) {
                return new ConversionResult(ex);
            }
        }

        public static Dictionary<string, CSharpSyntaxNode> ConvertMultiple(Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation compilation, IEnumerable<Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxTree> syntaxTrees)
        {
            var cSharpFirstPass = syntaxTrees.ToDictionary(tree => tree.FilePath ?? "unknown",
                tree => VisualBasicConverter.ConvertCompilationTree(compilation, tree));
            var cSharpCompilation = CSharpCompilation.Create("Conversion", cSharpFirstPass.Values, compilation.References);
            return cSharpFirstPass.ToDictionary(cs => cs.Key, cs => new CompilationErrorFixer(cSharpCompilation, cs.Value).Fix());
        }

        private static SyntaxTree GetSyntaxTreeWithAnnotatedSelection(TextSpan selected, SyntaxNode root)
        {
            var selectedNode = root.FindNode(selected);
            var annotatatedNode = selectedNode.WithAdditionalAnnotations(new SyntaxAnnotation(TriviaConverter.SelectedNodeAnnotationKind));
            var syntaxTree = root.ReplaceNode(selectedNode, annotatatedNode).SyntaxTree;
            return syntaxTree;
        }

        private static CSharpSyntaxNode ConvertSingleUnhandled(Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation compilation, Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxTree syntaxTree)
        {
            return ConvertMultiple(compilation, new[] {syntaxTree}).Values.Single();
        }

        private static SyntaxNode ConvertTextUnhandled(IReadOnlyCollection<MetadataReference> references, string text)
        {
            try
            {
                return ConvertFullTree(references, text);
            }
            catch (NotImplementedOrRequiresSurroundingMethodDeclaration) {
                text =
                    $@"Class SurroundingClass
Sub SurroundingSub()
{text}
End Sub
End Class";
                return ConvertFullTree(references, text).DescendantNodes().OfType<MethodDeclarationSyntax>().First().Body;
            }
        }

        private static SyntaxNode ConvertFullTree(IReadOnlyCollection<MetadataReference> references, string fullTreeText)
        {
            var tree = CreateTree(fullTreeText);
            var compilation = CreateCompilationFromTree(tree, references);
            return ConvertSingleUnhandled(compilation, tree);
        }

        private static Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxTree CreateTree(string text)
        {
            return (Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxTree) Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory.ParseSyntaxTree(SourceText.From(text));
        }

        private static Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation CreateCompilationFromTree(Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxTree tree, IEnumerable<MetadataReference> references)
        {
            var compilationOptions = new Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithRootNamespace("TestProject")
                .WithGlobalImports(Microsoft.CodeAnalysis.VisualBasic.GlobalImport.Parse("System", "System.Collections.Generic", "System.Linq",
                    "Microsoft.VisualBasic"));
            var compilation = Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation.Create("Conversion", new[] {tree}, references)
                .WithOptions(compilationOptions);
            return compilation;
        }
    }
}