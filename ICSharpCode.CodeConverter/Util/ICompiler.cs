using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Util
{
    public interface ICompiler
    {
        SyntaxTree CreateTree(string text);
        Compilation CreateCompilationFromTree(SyntaxTree tree, IEnumerable<MetadataReference> references);
    }
}
