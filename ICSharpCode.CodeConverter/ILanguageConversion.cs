using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.CSharp
{
    public interface ILanguageConversion
    {
        SyntaxTree SingleFirstPass(Compilation sourceCompilation, SyntaxTree tree);
        SyntaxNode SingleSecondPass(KeyValuePair<string, SyntaxTree> cs);
        SyntaxTree CreateTree(string text);
        Compilation CreateCompilationFromTree(SyntaxTree tree, IEnumerable<MetadataReference> references);
        List<SyntaxNode> FindSingleImportantChild(SyntaxNode annotatedNode);
        bool MustBeContainedByMethod(SyntaxNode m);
        bool MustBeContainedByClass(SyntaxNode m);
        string WithSurroundingMethod(string text);
        string WithSurroundingClass(string text);

        SyntaxNode GetSurroundedNode(IEnumerable<SyntaxNode> descendantNodes,
            bool surroundedWithMethod);
    }
}