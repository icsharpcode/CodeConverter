using System;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.VisualBasic;
using CS = Microsoft.CodeAnalysis.CSharp;
using CSS = Microsoft.CodeAnalysis.CSharp.Syntax;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System.Threading;

namespace ICSharpCode.CodeConverter.VB
{
    internal static class CSharpConverter
    {
        public static async Task<SyntaxNode> ConvertCompilationTreeAsync(Document document,
            VisualBasicCompilation vbViewOfCsSymbols, Project vbReferenceProject, CancellationToken cancellationToken)
        {
            document = await document.WithExpandedRootAsync(cancellationToken);
            var compilation = await document.Project.GetCompilationAsync(cancellationToken);
            var tree = await document.GetSyntaxTreeAsync(cancellationToken);
            var semanticModel = compilation.GetSemanticModel(tree, true);
            var root = await document.GetSyntaxRootAsync(cancellationToken) as CSS.CompilationUnitSyntax ??
                       throw new InvalidOperationException(NullRootError(document));

            var vbSyntaxGenerator = SyntaxGenerator.GetGenerator(vbReferenceProject);
            var numberOfLines = tree.GetLineSpan(root.FullSpan).EndLinePosition.Line;

            var visualBasicSyntaxVisitor = new NodesVisitor(document, (CS.CSharpCompilation)compilation, semanticModel, vbViewOfCsSymbols, vbSyntaxGenerator, numberOfLines);
            var converted = (VBSyntax.CompilationUnitSyntax)root.Accept(visualBasicSyntaxVisitor.TriviaConvertingVisitor);

            try {
                // This call is very expensive for large documents. Should look for a more performant version, e.g. Is NormalizeWhitespace good enough?
                converted = (VBSyntax.CompilationUnitSyntax)Formatter.Format(converted, document.Project.Solution.Workspace, cancellationToken: cancellationToken);
                return LineTriviaMapper.MapSourceTriviaToTarget(root, converted);
            } catch (Exception) { //TODO log
                return converted;
            }
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
