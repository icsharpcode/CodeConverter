using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ICSharpCode.CodeConverter.CSharp
{
    public class VBToCSConversion : ILanguageConversion
    {
        private Compilation _sourceCompilation;
        private readonly List<SyntaxTree> _firstPassResults = new List<SyntaxTree>();
        private readonly Lazy<CSharpCompilation> _targetCompilation;

        public VBToCSConversion()
        {
            _targetCompilation = new Lazy<CSharpCompilation>(() => CSharpCompilation.Create("Conversion", _firstPassResults, _sourceCompilation.References));
        }

        public SyntaxTree SingleFirstPass(Compilation sourceCompilation, SyntaxTree tree)
        {
            _sourceCompilation = sourceCompilation;
            var converted = VisualBasicConverter.ConvertCompilationTree((VisualBasicCompilation)sourceCompilation, (VisualBasicSyntaxTree)tree);
            var convertedTree = SyntaxFactory.SyntaxTree(converted);
            _firstPassResults.Add(convertedTree);
            return convertedTree;
        }

        public SyntaxNode SingleSecondPass(KeyValuePair<string, SyntaxTree> cs)
        {
            return new CompilationErrorFixer(_targetCompilation.Value, (CSharpSyntaxTree)cs.Value).Fix();
        }

        public string WithSurroundingClassAndMethod(string text)
        {
            return $@"Class SurroundingClass
Sub SurroundingSub()
{text}
End Sub
End Class";
        }

        public SyntaxNode RemoveSurroundingClassAndMethod(SyntaxNode secondPassNode)
        {
            return secondPassNode.DescendantNodes().OfType<MethodDeclarationSyntax>().First().Body;
        }

        public SyntaxTree CreateTree(string text)
        {
            return Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory.ParseSyntaxTree(SourceText.From(text));
        }

        public Compilation CreateCompilationFromTree(SyntaxTree tree, IEnumerable<MetadataReference> references)
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