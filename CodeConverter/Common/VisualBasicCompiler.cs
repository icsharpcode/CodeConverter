﻿using System.Text;
using Microsoft.CodeAnalysis.VisualBasic;

namespace ICSharpCode.CodeConverter.Common;

internal class VisualBasicCompiler : ICompiler
{
    private static readonly Lazy<VisualBasicCompilation> LazyVisualBasicCompilation = new(CreateVisualBasicCompilation);
    private readonly string _rootNamespace;

    // ReSharper disable once UnusedMember.Global - Used via generics
    public VisualBasicCompiler() : this("")
    {
    }

    public VisualBasicCompiler(string rootNamespace)
    {
        _rootNamespace = rootNamespace;
    }

    public SyntaxTree CreateTree(string text)
    {
        return SyntaxFactory.ParseSyntaxTree(text, ParseOptions, encoding: Encoding.UTF8);
    }

    public Compilation CreateCompilationFromTree(SyntaxTree tree, IEnumerable<MetadataReference> references)
    {
        var withReferences = CreateVisualBasicCompilation(references, _rootNamespace);
        return withReferences.AddSyntaxTrees(tree);
    }

    public static VisualBasicCompilation CreateVisualBasicCompilation(IEnumerable<MetadataReference> references, string rootNamespace = null)
    {
        var visualBasicCompilation = LazyVisualBasicCompilation.Value;
        var withReferences = visualBasicCompilation
            .WithOptions(visualBasicCompilation.Options.WithRootNamespace(rootNamespace))
            .WithReferences(visualBasicCompilation.References.Concat(references).Distinct());
        return withReferences;
    }

    private static VisualBasicCompilation CreateVisualBasicCompilation()
    {
        var compilationOptions = CreateCompilationOptions();
        return VisualBasicCompilation.Create("Conversion")
            .WithOptions(compilationOptions);
    }

    public static VisualBasicCompilationOptions CreateCompilationOptions(string rootNamespace = null)
    {
        // Caution: The simplifier won't always remove imports unused by the code
        // Known cases are unresolved usings and overload resolution across namespaces (e.g. System.Data.Where
        // and System.Linq.Where)

        var compilationOptions = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithGlobalImports(GlobalImport.Parse(
                "System",
                "System.Collections",
                "System.Collections.Generic",
                "System.Diagnostics",
                "System.Globalization",
                "System.IO",
                "System.Linq",
                "System.Reflection",
                "System.Runtime.CompilerServices",
                "System.Runtime.InteropServices",
                "System.Security",
                "System.Text",
                "System.Threading.Tasks",
                "System.Xml.Linq",
                "Microsoft.VisualBasic"))
            .WithOptionExplicit(true)
            .WithOptionCompareText(false)
            .WithOptionStrict(OptionStrict.Off)
            .WithOptionInfer(true)
            .WithRootNamespace(rootNamespace);
        return compilationOptions;
    }

    public static VisualBasicParseOptions ParseOptions { get; } = new(LanguageVersion.Latest);
}