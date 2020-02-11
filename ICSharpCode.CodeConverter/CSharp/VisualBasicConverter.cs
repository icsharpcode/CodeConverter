using System;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using CSS = Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class VisualBasicConverter
    {
        public static async Task<SyntaxNode> ConvertCompilationTree(Document document,
            CSharpCompilation csharpViewOfVbSymbols, Project csharpReferenceProject)
        {
            document = await document.WithExpandedRootAsync();
            var root = await document.GetSyntaxRootAsync() as VBSyntax.CompilationUnitSyntax ??
                       throw new InvalidOperationException(NullRootError(document));

            var compilation = await document.Project.GetCompilationAsync();
            var tree = await document.GetSyntaxTreeAsync();


            var csSyntaxGenerator = SyntaxGenerator.GetGenerator(csharpReferenceProject);
            var semanticModel = compilation.GetSemanticModel(tree, true);
            var visualBasicSyntaxVisitor = new
                DeclarationNodeVisitor(document, compilation, semanticModel, csharpViewOfVbSymbols, csSyntaxGenerator);
            var converted = (CSS.CompilationUnitSyntax)await root.AcceptAsync(visualBasicSyntaxVisitor.TriviaConvertingDeclarationVisitor);

            try {
                converted = (CSS.CompilationUnitSyntax)Formatter.Format(converted, document.Project.Solution.Workspace);
                return LineTriviaMapper.MapSourceTriviaToTarget(root, converted);
            } catch (Exception) { //TODO log
                return converted;
            }
        }

        private static string NullRootError(Document document)
        {
            string initial = document.Project.Language != LanguageNames.VisualBasic
                ? "Document cannot be converted because it's not within a VB project."
                : "Could not find valid VB within document.";
            return initial + " For best results, convert a VB document from within a VB project which compiles successfully.";
        }
    }
}
