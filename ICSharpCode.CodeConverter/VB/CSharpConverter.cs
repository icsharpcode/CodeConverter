using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using CS = Microsoft.CodeAnalysis.CSharp;
using CSS = Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.VB
{
    public class CSharpConverter
    {
        public static async Task<SyntaxNode> ConvertCompilationTree(Document document)
        {
            var compilation = await document.Project.GetCompilationAsync();
            var tree = await document.GetSyntaxTreeAsync();
            var semanticModel = compilation.GetSemanticModel(tree, true);
            var root = (CS.CSharpSyntaxNode) await document.GetSyntaxRootAsync();

            var visualBasicSyntaxVisitor = new NodesVisitor(semanticModel);
            return root.Accept(visualBasicSyntaxVisitor.TriviaConvertingVisitor);
        }
    }
}
