namespace ICSharpCode.CodeConverter.Common;

public interface ICompiler
{
    SyntaxTree CreateTree(string text);
    Compilation CreateCompilationFromTree(SyntaxTree tree, IEnumerable<MetadataReference> references);
}