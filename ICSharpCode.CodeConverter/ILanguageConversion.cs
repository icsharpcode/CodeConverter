using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.CSharp
{
    public interface ILanguageConversion
    {
        SyntaxTree SingleFirstPass(Compilation sourceCompilation, SyntaxTree tree);
        SyntaxNode SingleSecondPass(KeyValuePair<string, SyntaxTree> cs);
        string GetWarningsOrNull();
        SyntaxTree CreateTree(string text);
        Compilation CreateCompilationFromTree(SyntaxTree tree, IEnumerable<MetadataReference> references);
        List<SyntaxNode> FindSingleImportantChild(SyntaxNode annotatedNode);
        bool CanBeContainedByMethod(SyntaxNode node);
        bool MustBeContainedByClass(SyntaxNode node);
        string WithSurroundingMethod(string text);
        string WithSurroundingClass(string text);

        SyntaxNode GetSurroundedNode(IEnumerable<SyntaxNode> descendantNodes,
            bool surroundedWithMethod);
        IReadOnlyCollection<(string, string)> GetProjectTypeGuidMappings();
        IEnumerable<(string, string)> GetProjectFileReplacementRegexes();
    }
}