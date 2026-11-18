using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Common;
using ICSharpCode.CodeConverter.CSharp;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Tests.TestRunners;

/// <summary>
/// Converts as if the user's C# code style says "Prefer braces: No" (csharp_prefer_braces = false).
/// In Visual Studio, Tools > Options code style preferences reach the converter through <see cref="Solution.Options"/>,
/// which <see cref="Document.GetOptionsAsync"/> falls back to, and the simplifier honours them.
/// </summary>
public class VBToCSPreferNoBracesConversion : ILanguageConversion
{
    private readonly VBToCSConversion _baseConversion = new();

    async Task<Document> ILanguageConversion.CreateProjectDocumentFromTreeAsync(SyntaxTree tree, IEnumerable<MetadataReference> references)
    {
        var document = await _baseConversion.CreateProjectDocumentFromTreeAsync(tree, references);
        var solution = document.Project.Solution;
        var options = solution.Options.WithPreferBraces("None") ?? throw new InvalidOperationException("Can't set the \"Prefer braces\" option on this Roslyn version");
        return solution.WithOptions(options).GetDocument(document.Id);
    }

    string ILanguageConversion.TargetLanguage => _baseConversion.TargetLanguage;

    ConversionOptions ILanguageConversion.ConversionOptions {
        get => _baseConversion.ConversionOptions;
        set => _baseConversion.ConversionOptions = value;
    }

    Task<IProjectContentsConverter> ILanguageConversion.CreateProjectContentsConverterAsync(Project project,
        IProgress<ConversionProgress> progress, CancellationToken cancellationToken) =>
        _baseConversion.CreateProjectContentsConverterAsync(project, progress, cancellationToken);

    Task<Document> ILanguageConversion.SingleSecondPassAsync(Document doc) => _baseConversion.SingleSecondPassAsync(doc);
    SyntaxTree ILanguageConversion.CreateTree(string text) => _baseConversion.CreateTree(text);
    List<SyntaxNode> ILanguageConversion.FindSingleImportantChild(SyntaxNode annotatedNode) => _baseConversion.FindSingleImportantChild(annotatedNode);
    bool ILanguageConversion.CanBeContainedByMethod(SyntaxNode node) => _baseConversion.CanBeContainedByMethod(node);
    bool ILanguageConversion.MustBeContainedByClass(SyntaxNode node) => _baseConversion.MustBeContainedByClass(node);
    string ILanguageConversion.WithSurroundingMethod(string text) => _baseConversion.WithSurroundingMethod(text);
    string ILanguageConversion.WithSurroundingClass(string text) => _baseConversion.WithSurroundingClass(text);
    SyntaxNode ILanguageConversion.GetSurroundedNode(IEnumerable<SyntaxNode> descendantNodes, bool surroundedWithMethod) => _baseConversion.GetSurroundedNode(descendantNodes, surroundedWithMethod);
    IReadOnlyCollection<(string, string)> ILanguageConversion.GetProjectTypeGuidMappings() => _baseConversion.GetProjectTypeGuidMappings();
    IEnumerable<(string, string)> ILanguageConversion.GetProjectFileReplacementRegexes() => _baseConversion.GetProjectFileReplacementRegexes();
    string ILanguageConversion.PostTransformProjectFile(string xml) => _baseConversion.PostTransformProjectFile(xml);
}
