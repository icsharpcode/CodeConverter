using System;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using Xunit;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.Tests.LanguageAgnostic;

/// <summary>
/// <see cref="OptionalOperations.SimplifyStatementsAsync{T}"/> reports a timeout and keeps the unsimplified document
/// only if the cancellation reaches it: the expansion and reduction steps must not turn it into a conversion warning.
/// </summary>
/// <remarks>
/// The token is already cancelled, and the calls before each step's try block (getting the syntax root and the
/// semantic model) are served from Roslyn's caches, warmed up by <see cref="CreateWarmedUpDocumentAsync"/>, without
/// checking it, so the cancellation is first seen inside the try block. If Roslyn starts checking the token on those cached paths, these tests stop failing without the fix.
/// </remarks>
public class SimplificationCancellationTests
{
    private static readonly CancellationToken Cancelled = new(canceled: true);

    [Theory]
    [InlineData(LanguageNames.VisualBasic)]
    [InlineData(LanguageNames.CSharp)]
    public async Task ReductionRethrowsCancellationAsync(string language)
    {
        var document = await CreateWarmedUpDocumentAsync(language);
        var root = await document.GetSyntaxRootAsync();

        // Not through SimplifyStatementsAsync: its GetDiagnostics call checks the token before the reduction is reached
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => document.WithReducedRootAsync(root, Cancelled));
    }

    [Theory]
    [InlineData(LanguageNames.VisualBasic)]
    [InlineData(LanguageNames.CSharp)]
    public async Task ExpansionRethrowsCancellationAsync(string language)
    {
        var document = await CreateWarmedUpDocumentAsync(language);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => document.WithExpandedRootAsync(Cancelled));
    }

    [Theory]
    [InlineData(LanguageNames.VisualBasic)]
    [InlineData(LanguageNames.CSharp)]
    public async Task SimplificationWithoutCancellationSimplifiesAsync(string language)
    {
        var document = await CreateWarmedUpDocumentAsync(language);

        var simplified = await SimplifyAsync(document, CancellationToken.None);

        Assert.DoesNotContain("System.", (await simplified.GetTextAsync()).ToString());
        Assert.False((await simplified.GetSyntaxRootAsync()).HasAnnotations(AnnotationConstants.ConversionErrorAnnotationKind));
    }

    private static Task<Document> SimplifyAsync(Document document, CancellationToken cancellationToken) =>
        document.Project.Language == LanguageNames.VisualBasic
            ? document.SimplifyStatementsAsync<VBSyntax.ImportsStatementSyntax>("BC40056", cancellationToken)
            : document.SimplifyStatementsAsync<CSSyntax.UsingDirectiveSyntax>("CS0246", cancellationToken);

    private static async Task<Document> CreateWarmedUpDocumentAsync(string language)
    {
        Document document;
        if (language == LanguageNames.VisualBasic) {
            var vbProject = await new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .CreateProjectAsync(DefaultReferences.NetStandard2, VisualBasicParseOptions.Default);
            document = vbProject.AddDocumentFromTree(VisualBasicSyntaxTree.ParseText("Imports System\r\nClass C\r\n    Private f As System.Int32\r\nEnd Class"));
        } else {
            var csProject = await new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .CreateProjectAsync(DefaultReferences.NetStandard2, CSharpParseOptions.Default);
            document = csProject.AddDocumentFromTree(CSharpSyntaxTree.ParseText("using System;\r\nclass C\r\n{\r\n    private System.Int32 f;\r\n}"));
        }
        var semanticModel = await document.GetSemanticModelAsync();
        semanticModel.GetDiagnostics();
        return document;
    }
}
