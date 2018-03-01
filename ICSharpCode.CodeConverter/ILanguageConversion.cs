using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.CSharp
{
    public interface ILanguageConversion
    {
        SyntaxTree SingleFirstPass(Compilation sourceCompilation, SyntaxTree tree);
        SyntaxNode SingleSecondPass(KeyValuePair<string, SyntaxTree> cs);
        string WithSurroundingClassAndMethod(string text);
        SyntaxTree CreateTree(string text);
        Compilation CreateCompilationFromTree(SyntaxTree tree, IEnumerable<MetadataReference> references);
        SyntaxNode RemoveSurroundingClassAndMethod(SyntaxNode secondPassNode);
    }
}