namespace ICSharpCode.CodeConverter.Common;

public interface IProjectContentsConverter
{
    Task InitializeSourceAsync(Project project);
    string LanguageVersion { get; }
    string RootNamespace { get; }
    Project SourceProject { get; }
    OptionalOperations OptionalOperations { get; }
    Task<SyntaxNode> SingleFirstPassAsync(Document document);
    Task<(Project project, List<WipFileConversion<DocumentId>> firstPassDocIds)> GetConvertedProjectAsync(WipFileConversion<SyntaxNode>[] firstPassResults);
    public IAsyncEnumerable<ConversionResult> GetAdditionalConversionResultsAsync(IReadOnlyCollection<TextDocument> additionalDocumentsToConvert, CancellationToken cancellationToken);
}