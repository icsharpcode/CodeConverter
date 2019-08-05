using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.CSharp
{
    public interface ILanguageConversion
    {
        Task<Document> SingleFirstPass(Document document);
        Task<SyntaxNode> SingleSecondPass(KeyValuePair<string, Document> cs);
        Task<string> GetWarningsOrNull();
        SyntaxTree CreateTree(string text);
        Document CreateProjectDocumentFromTree(Workspace workspace, SyntaxTree tree,
            IEnumerable<MetadataReference> references);
        List<SyntaxNode> FindSingleImportantChild(SyntaxNode annotatedNode);
        bool CanBeContainedByMethod(SyntaxNode node);
        bool MustBeContainedByClass(SyntaxNode node);
        string WithSurroundingMethod(string text);
        string WithSurroundingClass(string text);

        SyntaxNode GetSurroundedNode(IEnumerable<SyntaxNode> descendantNodes,
            bool surroundedWithMethod);
        IReadOnlyCollection<(string, string)> GetProjectTypeGuidMappings();
        IEnumerable<(string, string)> GetProjectFileReplacementRegexes();
        string TargetLanguage { get; }
        string RootNamespace { get; set; }
        Task Initialize(Project project);
        string PostTransformProjectFile(string xml);
    }
}