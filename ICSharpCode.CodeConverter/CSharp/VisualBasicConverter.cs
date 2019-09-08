using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class VisualBasicConverter
    {
        public static async Task<SyntaxNode> ConvertCompilationTree(Document document,
            CSharpCompilation csharpViewOfVbSymbols, Project csharpReferenceProject)
        {
            var compilation = await document.Project.GetCompilationAsync();
            var tree = await document.GetSyntaxTreeAsync();
            var semanticModel = compilation.GetSemanticModel(tree, true);
            var root = (VBasic.VisualBasicSyntaxNode)await document.GetSyntaxRootAsync();
            var csSyntaxGenerator = SyntaxGenerator.GetGenerator(csharpReferenceProject);
            var visualBasicSyntaxVisitor = new 
                DeclarationNodeVisitor(document, compilation, semanticModel, csharpViewOfVbSymbols, csSyntaxGenerator);
            return await root.Accept(visualBasicSyntaxVisitor.TriviaConvertingVisitor);
        }
    }
}
