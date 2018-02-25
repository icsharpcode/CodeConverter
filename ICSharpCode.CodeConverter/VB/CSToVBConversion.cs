using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.VB;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using VBSyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory;
using CSSyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ICSharpCode.CodeConverter.CSharp
{
    public class CSToVBConversion : ILanguageConversion
    {
        public SyntaxTree SingleFirstPass(Compilation sourceCompilation, SyntaxTree tree)
        {
            var converted = CSharpConverter.ConvertCompilationTree((CSharpCompilation)sourceCompilation, (CSharpSyntaxTree)tree);
            var convertedTree = VBSyntaxFactory.SyntaxTree(converted);
            return convertedTree;
        }

        public SyntaxNode SingleSecondPass(KeyValuePair<string, SyntaxTree> cs)
        {
            return cs.Value.GetRoot();
        }

        public string WithSurroundingClassAndMethod(string text)
        {
            return $@"class SurroundingClass
{{
void SurroundingSub()
{{
{text}
}}
}}";
        }

        public SyntaxNode RemoveSurroundingClassAndMethod(SyntaxNode secondPassNode)
        {
            return secondPassNode.DescendantNodes().OfType<MethodBlockSyntax>().First();
        }

        public SyntaxTree CreateTree(string text)
        {
            return CSSyntaxFactory.ParseSyntaxTree(SourceText.From(text));
        }

        public Compilation CreateCompilationFromTree(SyntaxTree tree, IEnumerable<MetadataReference> references)
        {
            return CSharpCompilation.Create("Conversion", new[] { tree }, references);
        }
    }
}