using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace ICSharpCode.CodeConverter.Util
{
    public class CSharpCompiler : ICompiler
    {
        private static readonly Lazy<CSharpCompilation> LazyCSharpCompilation = new Lazy<CSharpCompilation>(CreateCSharpCompilation);

        public SyntaxTree CreateTree(string text)
        {
            return SyntaxFactory.ParseSyntaxTree(text, encoding: Encoding.UTF8);
        }

        public Compilation CreateCompilationFromTree(SyntaxTree tree, IEnumerable<MetadataReference> references)
        {
            return CreateCSharpCompilation(references).AddSyntaxTrees(tree);
        }

        public static Compilation CreateCSharpCompilation(IEnumerable<MetadataReference> references)
        {
            return LazyCSharpCompilation.Value.WithReferences(references);
        }

        private static CSharpCompilation CreateCSharpCompilation()
        {
            return CSharpCompilation.Create("Conversion", options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }
    }
}