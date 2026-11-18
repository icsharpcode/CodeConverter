using VBasicSyntaxKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// Analysis of a method body's unstructured error handling, made before any statement is converted. Produces no C# syntax.
/// </summary>
/// <remarks>
/// Follows VB's lowering, where an <c>On Error</c> statement sets the active handler when it executes:
/// https://github.com/dotnet/roslyn/blob/main/src/Compilers/VisualBasic/Portable/Lowering/LocalRewriter/LocalRewriter_UnstructuredExceptionHandling.vb
/// The active handler is only known per statement when every <c>On Error</c> statement is at method level and no jump crosses between the regions they start.
/// </remarks>
internal sealed class OnErrorModel
{
    private readonly Dictionary<SyntaxNode, int> _methodLevelIndexes;
    private readonly IReadOnlyDictionary<string, VBSyntax.LabelStatementSyntax> _labels;

    private OnErrorModel(IReadOnlyList<VBSyntax.StatementSyntax> statements, SemanticModel semanticModel)
    {
        Statements = statements;
        _methodLevelIndexes = statements.Select((s, i) => (s, i)).ToDictionary(x => (SyntaxNode)x.s, x => x.i);
        var descendants = statements.SelectMany(s => s.DescendantNodesAndSelf(n => n is not VBSyntax.LambdaExpressionSyntax)).ToArray();
        OnErrorStatements = descendants.Where(IsOnErrorStatement).Cast<VBSyntax.StatementSyntax>().ToArray();
        ResumeStatements = descendants.OfType<VBSyntax.ResumeStatementSyntax>().ToArray();
        _labels = descendants.OfType<VBSyntax.LabelStatementSyntax>().GroupBy(LabelName.Of).ToDictionary(g => g.Key, g => g.First());
        Jumps = descendants.Select(AsJumpOrNull).Where(j => j != null).ToArray();
        ActiveHandlers = GetStaticActiveHandlers();
        Handlers = OnErrorStatements.OfType<VBSyntax.OnErrorGoToStatementSyntax>()
            .Where(s => s.IsKind(VBasicSyntaxKind.OnErrorGoToLabelStatement))
            .Select(s => LabelName.Of(s.Label)).Distinct()
            .Select(label => CreateHandlerRegion(label, semanticModel))
            .ToArray();
        IsDynamic = !AreActiveHandlersStatic(ignoreGoToZeroInHandlers: false);
        HandlersOnlyTurnErrorHandlingOff = IsDynamic && AreActiveHandlersStatic(ignoreGoToZeroInHandlers: true);
    }

    public static OnErrorModel Create(IReadOnlyCollection<VBSyntax.StatementSyntax> statements, SemanticModel semanticModel) =>
        new(statements.ToArray(), semanticModel);

    public static bool ContainsOnErrorOrResume(SyntaxNode node) =>
        node.DescendantNodes(n => n is not VBSyntax.LambdaExpressionSyntax).Any(n => IsOnErrorStatement(n) || n is VBSyntax.ResumeStatementSyntax);

    /// <summary>The method-level statements.</summary>
    public IReadOnlyList<VBSyntax.StatementSyntax> Statements { get; }

    /// <summary><c>On Error GoTo</c> and <c>On Error Resume Next</c> statements at any depth.</summary>
    public IReadOnlyList<VBSyntax.StatementSyntax> OnErrorStatements { get; }

    public IReadOnlyList<VBSyntax.ResumeStatementSyntax> ResumeStatements { get; }

    /// <summary><c>GoTo L</c> and <c>Resume L</c> at any depth.</summary>
    public IReadOnlyList<Jump> Jumps { get; }

    /// <summary>The handler in effect while each method-level statement runs, assuming <see cref="IsDynamic"/> is false.</summary>
    public IReadOnlyList<ActiveHandler> ActiveHandlers { get; }

    /// <summary>One region per label named by an <c>On Error GoTo</c>.</summary>
    public IReadOnlyList<HandlerRegion> Handlers { get; }

    /// <summary>The active handler can't be known per statement, so it needs recording at runtime.</summary>
    public bool IsDynamic { get; }

    /// <summary>
    /// <see cref="IsDynamic"/> only because a handler runs <c>On Error GoTo 0</c>. That turns off what the handler resumes to, which the structured shape doesn't protect anyway.
    /// </summary>
    public bool HandlersOnlyTurnErrorHandlingOff { get; }

    public bool HasOnErrorOrResume => OnErrorStatements.Any() || ResumeStatements.Any();

    /// <summary>Index of the method-level statement that is or contains <paramref name="node"/>, or -1.</summary>
    public int MethodLevelIndexOf(SyntaxNode node) =>
        node.AncestorsAndSelf().Select(a => _methodLevelIndexes.TryGetValue(a, out var i) ? i : -1).Where(i => i >= 0).DefaultIfEmpty(-1).First();

    /// <summary>Index of the label's statement when it's at method level, otherwise -1.</summary>
    public int MethodLevelIndexOfLabel(string label) =>
        _labels.TryGetValue(label, out var statement) && _methodLevelIndexes.TryGetValue(statement, out var i) ? i : -1;

    /// <summary>Index of the method-level statement containing the label, or -1 if there's no such label.</summary>
    public int IndexContainingLabel(string label) => _labels.TryGetValue(label, out var statement) ? MethodLevelIndexOf(statement) : -1;

    /// <summary>The label as declared: C# labels are case sensitive.</summary>
    public string LabelText(string label) => _labels.TryGetValue(label, out var statement) ? statement.LabelToken.Text : label;

    public HandlerRegion HandlerContaining(int statementIndex) => Handlers.FirstOrDefault(h => h.Contains(statementIndex));

    public static bool IsOnErrorStatement(SyntaxNode node) => node is VBSyntax.OnErrorGoToStatementSyntax or VBSyntax.OnErrorResumeNextStatementSyntax;

    /// <summary>
    /// Whether execution can continue past the statement, if it starts. Jumps and exits are decided by their kind, VB's flow analysis decides the rest.
    /// Flow analysis doesn't know that <c>On Error GoTo H</c> can reach <c>H</c>, so for a handler's statements it can only say they're unreachable: assume they can complete.
    /// </summary>
    public static bool EndPointIsReachable(VBSyntax.StatementSyntax statement, SemanticModel semanticModel)
    {
        switch (statement) {
            case VBSyntax.GoToStatementSyntax or VBSyntax.ResumeStatementSyntax or VBSyntax.ReturnStatementSyntax or VBSyntax.ThrowStatementSyntax:
            case VBSyntax.ExitStatementSyntax exit when IsMethodExit(exit):
            case VBSyntax.StopOrEndStatementSyntax { StopOrEndKeyword.RawKind: (int)VBasicSyntaxKind.EndKeyword }:
                return false;
            case VBSyntax.LabelStatementSyntax or VBSyntax.ExitStatementSyntax or VBSyntax.OnErrorGoToStatementSyntax or VBSyntax.OnErrorResumeNextStatementSyntax:
                return true;
            default:
                var flow = semanticModel.AnalyzeControlFlow(statement);
                return !flow.StartPointIsReachable || flow.EndPointIsReachable;
        }
    }

    public static bool IsMethodExit(VBSyntax.ExitStatementSyntax exit) =>
        VBasic.VisualBasicExtensions.Kind(exit.BlockKeyword) is VBasicSyntaxKind.SubKeyword or VBasicSyntaxKind.FunctionKeyword or VBasicSyntaxKind.PropertyKeyword;

    private Jump AsJumpOrNull(SyntaxNode node) => node switch {
        VBSyntax.GoToStatementSyntax goTo => new Jump(goTo, LabelName.Of(goTo.Label)),
        VBSyntax.ResumeStatementSyntax { Label: { } label } resume when resume.IsKind(VBasicSyntaxKind.ResumeLabelStatement) => new Jump(resume, LabelName.Of(label)),
        _ => null
    };

    private IReadOnlyList<ActiveHandler> GetStaticActiveHandlers()
    {
        var activeHandlers = new List<ActiveHandler>();
        var current = ActiveHandler.None;
        foreach (var statement in Statements) {
            activeHandlers.Add(current);
            current = statement switch {
                VBSyntax.OnErrorResumeNextStatementSyntax => ActiveHandler.ResumeNext,
                VBSyntax.OnErrorGoToStatementSyntax s when s.IsKind(VBasicSyntaxKind.OnErrorGoToLabelStatement) => ActiveHandler.ForLabel(s.Label.LabelToken.Text),
                VBSyntax.OnErrorGoToStatementSyntax s when s.IsKind(VBasicSyntaxKind.OnErrorGoToZeroStatement) => ActiveHandler.None,
                _ => current
            };
        }
        return activeHandlers;
    }

    /// <summary>
    /// A handler's statements run with that handler still active, whatever their position. On Error inside one changes what's active wherever it resumes to.
    /// </summary>
    private bool AreActiveHandlersStatic(bool ignoreGoToZeroInHandlers)
    {
        bool IgnoredInHandler(VBSyntax.StatementSyntax s) => ignoreGoToZeroInHandlers && s.IsKind(VBasicSyntaxKind.OnErrorGoToZeroStatement) && HandlerContaining(MethodLevelIndexOf(s)) != null;
        if (OnErrorStatements.Any(s => !_methodLevelIndexes.ContainsKey(s) && !IgnoredInHandler(s))) return false;
        if (OnErrorStatements.Any(s => HandlerContaining(MethodLevelIndexOf(s)) != null && !IgnoredInHandler(s))) return false;
        return Jumps.All(j => {
            var sourceIndex = MethodLevelIndexOf(j.Statement);
            var targetIndex = IndexContainingLabel(j.TargetLabel);
            if (targetIndex < 0) return true;
            var sourceHandler = HandlerContaining(sourceIndex) is { } handler ? ActiveHandler.ForLabel(handler.Label) : ActiveHandlers[sourceIndex];
            return sourceHandler == ActiveHandlers[targetIndex];
        });
    }

    private HandlerRegion CreateHandlerRegion(string label, SemanticModel semanticModel)
    {
        var labelIndex = MethodLevelIndexOfLabel(label);
        if (labelIndex < 0) return new HandlerRegion(label, -1, -1, Array.Empty<HandlerExit>());

        var endIndex = Enumerable.Range(labelIndex + 1, Statements.Count - labelIndex - 1)
            .Where(i => Statements[i] is VBSyntax.LabelStatementSyntax).DefaultIfEmpty(Statements.Count).First();
        var regionStatements = Statements.Skip(labelIndex + 1).Take(endIndex - labelIndex - 1).ToArray();
        var regionLabels = new HashSet<string>(regionStatements
            .SelectMany(s => s.DescendantNodesAndSelf().OfType<VBSyntax.LabelStatementSyntax>()).Select(LabelName.Of));

        var exits = regionStatements.SelectMany(s => s.DescendantNodesAndSelf(n => n is not VBSyntax.LambdaExpressionSyntax))
            .Select(n => AsExitOrNull(n, regionLabels))
            .Where(e => e != null)
            .ToList();
        if (!regionStatements.Any() || EndPointIsReachable(regionStatements.Last(), semanticModel)) {
            exits.Add(endIndex == Statements.Count
                ? new HandlerExit(HandlerExitKind.FallsOffEnd)
                : new HandlerExit(HandlerExitKind.FallsIntoLabel, LabelName.Of((VBSyntax.LabelStatementSyntax)Statements[endIndex])));
        }
        return new HandlerRegion(label, labelIndex, endIndex, exits.Distinct().ToArray());
    }

    private static HandlerExit AsExitOrNull(SyntaxNode node, HashSet<string> regionLabels) => node switch {
        VBSyntax.ResumeStatementSyntax r when r.IsKind(VBasicSyntaxKind.ResumeNextStatement) => new HandlerExit(HandlerExitKind.ResumeNext),
        VBSyntax.ResumeStatementSyntax r when r.IsKind(VBasicSyntaxKind.ResumeLabelStatement) => new HandlerExit(HandlerExitKind.ResumeLabel, LabelName.Of(r.Label)),
        VBSyntax.ResumeStatementSyntax => new HandlerExit(HandlerExitKind.Resume),
        VBSyntax.GoToStatementSyntax g when !regionLabels.Contains(LabelName.Of(g.Label)) => new HandlerExit(HandlerExitKind.GoToLabel, LabelName.Of(g.Label)),
        VBSyntax.ExitStatementSyntax e when IsMethodExit(e) => new HandlerExit(HandlerExitKind.ExitMethod),
        VBSyntax.ReturnStatementSyntax => new HandlerExit(HandlerExitKind.ExitMethod),
        _ => null
    };

    public sealed record Jump(VBSyntax.StatementSyntax Statement, string TargetLabel);
}
