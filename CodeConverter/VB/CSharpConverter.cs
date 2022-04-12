using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.VisualBasic;

namespace ICSharpCode.CodeConverter.VB;

internal static class CSharpConverter
{
    public static async Task<SyntaxNode> ConvertCompilationTreeAsync(Document document,
        VisualBasicCompilation vbViewOfCsSymbols, Project vbReferenceProject, OptionalOperations optionalOperations, CancellationToken cancellationToken)
    {
        document = await document.WithExpandedRootAsync(cancellationToken);
        var compilation = await document.Project.GetCompilationAsync(cancellationToken);
        var tree = await document.GetSyntaxTreeAsync(cancellationToken);
        var semanticModel = compilation.GetSemanticModel(tree, true);
        var root = await document.GetSyntaxRootAsync(cancellationToken) as CSSyntax.CompilationUnitSyntax ??
                   throw new InvalidOperationException(NullRootError(document));

        var vbSyntaxGenerator = SyntaxGenerator.GetGenerator(vbReferenceProject);
        _ = tree.GetLineSpan(root.FullSpan, cancellationToken).EndLinePosition.Line;

        var visualBasicSyntaxVisitor = new NodesVisitor((CS.CSharpCompilation)compilation, semanticModel, vbViewOfCsSymbols, vbSyntaxGenerator);
        var converted = (VBSyntax.CompilationUnitSyntax)root.Accept(visualBasicSyntaxVisitor.TriviaConvertingVisitor);

        return optionalOperations.MapSourceTriviaToTargetHandled(root, converted, document);
    }

    private static string NullRootError(Document document)
    {
        var initial = document.Project.Language != LanguageNames.CSharp
            ? "Document cannot be converted because it's not within a C# project."
            : "Could not find valid C# within document.";
        return initial + " For best results, convert a c# document from within a C# project which compiles successfully.";
    }
}