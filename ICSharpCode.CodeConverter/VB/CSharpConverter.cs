using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using CS = Microsoft.CodeAnalysis.CSharp;
using CSS = Microsoft.CodeAnalysis.CSharp.Syntax;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.VB
{
    internal static class CSharpConverter
    {
        public static async Task<SyntaxNode> ConvertCompilationTree(Document document,
            VisualBasicCompilation vbViewOfCsSymbols, Project vbReferenceProject)
        {
            document = await document.WithExpandedRootAsync();
            var compilation = await document.Project.GetCompilationAsync();
            var tree = await document.GetSyntaxTreeAsync();
            var semanticModel = compilation.GetSemanticModel(tree, true);
            var root = await document.GetSyntaxRootAsync() as CSS.CompilationUnitSyntax ??
                       throw new InvalidOperationException(NullRootError(document));

            var vbSyntaxGenerator = SyntaxGenerator.GetGenerator(vbReferenceProject);
            var numberOfLines = tree.GetLineSpan(root.FullSpan).EndLinePosition.Line;

            var visualBasicSyntaxVisitor = new NodesVisitor(document, (CS.CSharpCompilation)compilation, semanticModel, vbViewOfCsSymbols, vbSyntaxGenerator, numberOfLines);
            var converted = (VBSyntax.CompilationUnitSyntax)root.Accept(visualBasicSyntaxVisitor.TriviaConvertingVisitor);

            try {
                converted = (VBSyntax.CompilationUnitSyntax)Formatter.Format(converted, document.Project.Solution.Workspace);
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
