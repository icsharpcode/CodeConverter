using System.Collections.Generic;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using Microsoft.CodeAnalysis;
using System.Threading;
using System;

namespace ICSharpCode.CodeConverter
{
    public interface ILanguageConversion
    {
        Task<Document> SingleSecondPassAsync(Document doc);
        SyntaxTree CreateTree(string text);
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
        ConversionOptions ConversionOptions { get; set; }

        Task<IProjectContentsConverter> CreateProjectContentsConverterAsync(Project project, IProgress<ConversionProgress> progress, CancellationToken cancellationToken);
        string PostTransformProjectFile(string xml);

        Task<Document> CreateProjectDocumentFromTreeAsync(SyntaxTree tree, IEnumerable<MetadataReference> references);
    }
}