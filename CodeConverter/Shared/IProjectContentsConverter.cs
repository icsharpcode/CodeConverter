using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Shared
{
    public interface IProjectContentsConverter
    {
        Task InitializeSourceAsync(Project project);
        string LanguageVersion { get; }
        string RootNamespace { get; }
        Project Project { get; }
        Task<SyntaxNode> SingleFirstPass(Document document);
        Task<(Project project, List<WipFileConversion<DocumentId>> firstPassDocIds)> GetConvertedProject(WipFileConversion<SyntaxNode>[] firstPassResults);
        public IAsyncEnumerable<ConversionResult> GetAdditionalConversionResults(IReadOnlyCollection<TextDocument> additionalDocumentsToConvert, CancellationToken cancellationToken);
    }
}