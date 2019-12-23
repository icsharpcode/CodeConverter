using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.VisualBasic;
using CS = Microsoft.CodeAnalysis.CSharp;
using CSS = Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.VB
{
    internal class CSharpConverter
    {
        public static async Task<SyntaxNode> ConvertCompilationTree(Document document,
            VisualBasicCompilation vbViewOfCsSymbols, Project vbReferenceProject)
        {
            var compilation = await document.Project.GetCompilationAsync();
            var tree = await document.GetSyntaxTreeAsync();
            var semanticModel = compilation.GetSemanticModel(tree, true);
            var root = await document.GetSyntaxRootAsync() as CS.CSharpSyntaxNode ??
                       throw new InvalidOperationException(NullRootError(document));

            var vbSyntaxGenerator = SyntaxGenerator.GetGenerator(vbReferenceProject);
            var visualBasicSyntaxVisitor = new NodesVisitor(document, (CS.CSharpCompilation) compilation, semanticModel, vbViewOfCsSymbols, vbSyntaxGenerator);
            return root.Accept(visualBasicSyntaxVisitor.TriviaConvertingVisitor);
        }

        private static string NullRootError(Document document)
        {
            var initial = document.Project.Language != LanguageNames.CSharp
                ? "Document cannot be converted because it's not within a C# project."
                : "Could not find valid C# within document.";
            return initial + " For best results, convert a c# document from within a C# project which compiles successfully.";
        }
    }
}
