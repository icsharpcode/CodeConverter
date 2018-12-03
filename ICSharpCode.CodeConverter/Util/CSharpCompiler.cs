using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace ICSharpCode.CodeConverter.Util
{
    public class CSharpCompiler : ICompiler
    {
        public SyntaxTree CreateTree(string text)
        {
            return SyntaxFactory.ParseSyntaxTree(SourceText.From(text));
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