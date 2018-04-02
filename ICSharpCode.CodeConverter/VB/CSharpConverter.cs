using Microsoft.CodeAnalysis.VisualBasic;
using CS = Microsoft.CodeAnalysis.CSharp;
using CSS = Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.VB
{
    public class CSharpConverter
    {
        public static VisualBasicSyntaxNode ConvertCompilationTree(CS.CSharpCompilation compilation, CS.CSharpSyntaxTree tree)
        {
            var visualBasicSyntaxVisitor = new NodesVisitor(compilation.GetSemanticModel(tree, true));
            return tree.GetRoot().Accept(visualBasicSyntaxVisitor.TriviaConvertingVisitor);
        }
    }
}
