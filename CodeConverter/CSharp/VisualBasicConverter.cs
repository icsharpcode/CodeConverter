using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;

namespace ICSharpCode.CodeConverter.CSharp;

internal static class VisualBasicConverter
{
    public static async Task<SyntaxNode> ConvertCompilationTreeAsync(Document document,
        CSharpCompilation csharpViewOfVbSymbols, Project csharpReferenceProject,
        OptionalOperations optionalOperations, ILookup<ITypeSymbol, ITypeSymbol> typeToInheritors, CancellationToken cancellationToken)
    {
        document = await document.WithExpandedRootAsync(cancellationToken);
        var root = await document.GetSyntaxRootAsync(cancellationToken) as VBSyntax.CompilationUnitSyntax ??
                   throw new InvalidOperationException(NullRootError(document));

        var compilation = await document.Project.GetCompilationAsync(cancellationToken);
        var tree = await document.GetSyntaxTreeAsync(cancellationToken);


        var csSyntaxGenerator = SyntaxGenerator.GetGenerator(csharpReferenceProject);
        var semanticModel = compilation.GetSemanticModel(tree, true);
        var visualBasicSyntaxVisitor = new
            DeclarationNodeVisitor(document, compilation, semanticModel, csharpViewOfVbSymbols, csSyntaxGenerator, typeToInheritors);
        var converted = await root.AcceptAsync<CSSyntax.CompilationUnitSyntax>(visualBasicSyntaxVisitor.TriviaConvertingDeclarationVisitor);

        return optionalOperations.MapSourceTriviaToTargetHandled(root, converted, document);
    }

    private static string NullRootError(Document document)
    {
        string initial = document.Project.Language != LanguageNames.VisualBasic
            ? "Document cannot be converted because it's not within a VB project."
            : "Could not find valid VB within document.";
        return initial + " For best results, convert a VB document from within a VB project which compiles successfully.";
    }
}