using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;

namespace ICSharpCode.CodeConverter.Util
{
    public class VisualBasicCompiler : ICompiler
    {
        private readonly string _rootNamespace;

        // ReSharper disable once UnusedMember.Global - Used via generics
        public VisualBasicCompiler() : this("")
        {
        }

        public VisualBasicCompiler(string rootNamespace)
        {
            _rootNamespace = rootNamespace;
        }

        public SyntaxTree CreateTree(string text)
        {
            return SyntaxFactory.ParseSyntaxTree(text, encoding: Encoding.UTF8);
        }

        public Compilation CreateCompilationFromTree(SyntaxTree tree, IEnumerable<MetadataReference> references)
        {
            var compilation = CreateVisualBasicCompilation(references, _rootNamespace);
            return compilation.AddSyntaxTrees(tree);
        }

        public static VisualBasicCompilation CreateVisualBasicCompilation(IEnumerable<MetadataReference> references, string rootNamespace = null)
        {
            var compilationOptions = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithRootNamespace(rootNamespace)
                .WithGlobalImports(GlobalImport.Parse(
                    "System",
                    "System.Collections.Generic",
                    "System.Diagnostics",
                    "System.Globalization",
                    "System.IO",
                    "System.Linq",
                    "System.Reflection",
                    "System.Runtime.CompilerServices",
                    "System.Security",
                    "System.Text",
                    "System.Threading.Tasks",
                    "Microsoft.VisualBasic"));
            var compilation = VisualBasicCompilation.Create("Conversion", references: references)
                .WithOptions(compilationOptions);
            return compilation;
        }
    }
}