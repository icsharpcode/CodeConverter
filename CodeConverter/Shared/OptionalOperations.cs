using Microsoft.CodeAnalysis.Formatting;

namespace ICSharpCode.CodeConverter.Common;

/// <summary>
/// https://github.com/icsharpcode/CodeConverter/issues/598#issuecomment-663773878
/// </summary>
public class OptionalOperations
{
    private readonly TimeSpan _abandonTasksIfNoActivityFor;
    private readonly IProgress<ConversionProgress> _progress;
    private readonly CancellationToken _wholeTaskCancellationToken;

    public OptionalOperations(TimeSpan abandonTasksIfNoActivityFor, IProgress<ConversionProgress> progress,
        CancellationToken wholeTaskCancellationToken)
    {
        _abandonTasksIfNoActivityFor = abandonTasksIfNoActivityFor;
        _progress = progress;
        _wholeTaskCancellationToken = wholeTaskCancellationToken;
    }

    public SyntaxNode MapSourceTriviaToTargetHandled<TSource, TTarget>(TSource root,
        TTarget converted, Document document)
        where TSource : SyntaxNode, ICompilationUnitSyntax where TTarget : SyntaxNode, ICompilationUnitSyntax
    {
        try
        {
            converted = (TTarget) Format(converted, document);
            return LineTriviaMapper.MapSourceTriviaToTarget(root, converted);
        }
        catch (Exception e)
        {
            _progress.Report(new ConversionProgress($"Error while formatting and converting comments: {e}"));
            return converted;
        }
    }

    public SyntaxNode Format(SyntaxNode node, Document document)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(_wholeTaskCancellationToken);
        var token = cts.Token;
        cts.CancelAfter(_abandonTasksIfNoActivityFor);
        try {
            // This call is very expensive for large documents. Should look for a more performant version, e.g. Is NormalizeWhitespace good enough?
            return Formatter.Format(node, document.Project.Solution.Workspace, cancellationToken: token);
        } catch (OperationCanceledException) {
            _progress.Report(new ConversionProgress("Timeout expired - falling back to basic formatting. If within Visual Studio you can adjust the timeout in Tools -> Options -> Code Converter.", 1));
            return node.NormalizeWhitespace();
        }
    }
}