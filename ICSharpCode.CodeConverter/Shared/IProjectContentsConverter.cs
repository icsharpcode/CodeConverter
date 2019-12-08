using System.Collections.Generic;
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

        Task<(Project project, List<(string Path, DocumentId DocId, string[] Errors)> firstPassDocIds)>
            GetConvertedProject((string Path, SyntaxNode Node, string[] Errors)[] firstPassResults);
    }
}