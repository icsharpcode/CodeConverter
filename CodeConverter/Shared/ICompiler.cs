﻿namespace ICSharpCode.CodeConverter.Shared;

public interface ICompiler
{
    SyntaxTree CreateTree(string text);
    Compilation CreateCompilationFromTree(SyntaxTree tree, IEnumerable<MetadataReference> references);
}