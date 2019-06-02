using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace ICSharpCode.CodeConverter.Util
{
    public class CSharpCompiler : ICompiler
    {
        public SyntaxTree CreateTree(string text)
        {
            return SyntaxFactory.ParseSyntaxTree(text, encoding: Encoding.UTF8);
        }

        public Compilation CreateCompilationFromTree(SyntaxTree tree, IEnumerable<MetadataReference> references)
        {
            return CreateCSharpCompilation(references).AddSyntaxTrees(tree);
        }

        public static CSharpCompilation CreateCSharpCompilation(IEnumerable<MetadataReference> references)
        {
            return CSharpCompilation.Create("Conversion", references: references, options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }
    }
}