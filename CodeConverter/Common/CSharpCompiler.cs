﻿using System.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace ICSharpCode.CodeConverter.Common;

internal class CSharpCompiler : ICompiler
{
    private static readonly Lazy<CSharpCompilation> LazyCSharpCompilation = new(CreateCSharpCompilation);

    public SyntaxTree CreateTree(string text)
    {
        return SyntaxFactory.ParseSyntaxTree(text, ParseOptions, encoding: Encoding.UTF8);
    }

    public Compilation CreateCompilationFromTree(SyntaxTree tree, IEnumerable<MetadataReference> references)
    {
        return CreateCSharpCompilation(references).AddSyntaxTrees(tree);
    }

    public static Compilation CreateCSharpCompilation(IEnumerable<MetadataReference> references)
    {
        var cSharpCompilation = LazyCSharpCompilation.Value;
        return cSharpCompilation.WithReferences(cSharpCompilation.References.Concat(references).Distinct());
    }

    private static CSharpCompilation CreateCSharpCompilation()
    {
        return CSharpCompilation.Create("Conversion", options: CreateCompilationOptions());
    }

    public static CSharpCompilationOptions CreateCompilationOptions()
    {
        return new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);
    }

    public static CSharpParseOptions ParseOptions { get; } = new(LanguageVersion.Latest);
}