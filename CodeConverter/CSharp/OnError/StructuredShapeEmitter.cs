using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// Emits README.md shape 1: the covered statements in a <c>try</c> with the handler in its <c>catch</c>.
/// </summary>
/// <remarks>
/// Where the handler goes depends on how it's reached and left:
/// * Only reached by an error: in the <c>catch</c>. When it falls off the end of the method, the <c>catch</c> must return rather than carry on after the <c>try</c>.
/// * Also reached by running into its label: after the <c>try</c>, as ordinary code, and the <c>catch</c> jumps to it past any statements in between.
/// * Segmented (re-entrant handler): after all the segments, each segment's <c>catch</c> jumping to it, so a secondary error re-enters it as in VB.
/// When the handler leaves to a label after itself, the statements from there on (the tail) follow the <c>try</c>, so the handler's jump to the label is a fall through.
/// </remarks>
internal sealed class StructuredShapeEmitter
{
    private readonly OnErrorConversionContext _context;
    private readonly OnErrorModel _model;
    private readonly StructuredPlan _plan;
    private readonly ErrStatements _err;
    private readonly LocalHoisting _hoisting;
    private readonly string _exceptionVariableName;

    public StructuredShapeEmitter(OnErrorConversionContext context, OnErrorModel model, StructuredPlan plan, ErrStatements err)
    {
        _context = context;
        _model = model;
        _plan = plan;
        _err = err;
        _hoisting = new LocalHoisting(context, model, plan.Segments.Append(UnprotectedRange(plan)).Append(plan.Tail));
        _exceptionVariableName = err.IsMaintained ? context.Visitor.GetUniqueVariableNameInScope(model.Statements[plan.OnErrorIndex], "ex") : null;
        context.Visitor.OnErrorStatementConverter = new StructuredStatementConverter(err, model);
    }

    /// <summary>
    /// Whether every local that has to be declared before the <c>try</c>, because it is used outside it, can be (see <see cref="DeclarationSplitter.CanSplit"/>).
    /// </summary>
    public static bool CanEmit(OnErrorConversionContext context, OnErrorModel model, StructuredPlan plan) =>
        new LocalHoisting(context, model, plan.Segments.Append(UnprotectedRange(plan)).Append(plan.Tail)).CanDeclareAll;

    private VBSyntax.StatementSyntax OnErrorStatement => _model.Statements[_plan.OnErrorIndex];
    private int HandlerLabelIndex => _plan.Handler.LabelIndex;
    private string HandlerLabel => _model.LabelText(_plan.Handler.Label);

    public async Task<IReadOnlyCollection<StatementSyntax>> EmitAsync()
    {
        var before = DefaultInitializers.Add(await ConvertRangeAsync(0, _plan.OnErrorIndex)).ToList();
        var protectedAndHandler = _plan.IsSegmented ? await EmitSegmentedAsync() : await EmitPlainAsync();
        var statements = before.Concat(StatementReachability.EndIsReachable(before) ? _err.Clear() : default).Concat(_hoisting.Declarations).Concat(protectedAndHandler).ToList();
        var labelsRemoved = WithoutUnreferencedLabels(statements);
        return NeedsMissingReturn(labelsRemoved) ? MissingReturn.Add(_context, _model, labelsRemoved).ToList() : labelsRemoved;
    }

    /// <summary>
    /// VB's flow analysis can't see that the tail's label is reached by the handler, so it says the end of the method is unreachable and no return is added after it.
    /// </summary>
    private bool NeedsMissingReturn(List<StatementSyntax> statements) =>
        _plan.Handler.Exits.Any(e => e.Kind == HandlerExitKind.FallsOffEnd) || _plan.HasTail && StatementReachability.EndIsReachable(statements);

    private async Task<IEnumerable<StatementSyntax>> EmitPlainAsync()
    {
        var covered = await _hoisting.ConvertAsync(StatementsIn(_plan.Covered));
        var unprotected = await _hoisting.ConvertAsync(StatementsIn(UnprotectedRange(_plan)));
        var handler = await ConvertHandlerAsync();
        var tail = await _hoisting.ConvertAsync(StatementsIn(_plan.Tail));

        if (_plan.HandlerReachedWithoutError) {
            if (!unprotected.Any()) return new[] { Try(covered, Enumerable.Empty<StatementSyntax>()) }.Concat(handler);
            return new[] { Try(covered, GoTo(HandlerLabel)) }.Concat(unprotected).Append(Label(HandlerLabel)).Concat(handler);
        }

        if (unprotected.Any() && _plan.Handler.Exits.Any(e => e.Kind == HandlerExitKind.FallsOffEnd)) {
            handler.Add(_context.Visitor.CreateMethodExitStatement(OnErrorStatement));
        }
        if (handler.LastOrDefault() is GotoStatementSyntax { Expression: IdentifierNameSyntax target } && IsLabel(unprotected.Concat(tail).FirstOrDefault(), target.Identifier.ValueText)) {
            handler.RemoveAt(handler.Count - 1);
        }
        return new[] { Try(covered, handler) }.Concat(unprotected).Concat(tail);
    }

    private async Task<IEnumerable<StatementSyntax>> EmitSegmentedAsync()
    {
        var statements = new List<StatementSyntax>();
        foreach (var segment in _plan.Segments) {
            if (segment.LabelIndex >= 0) statements.AddRange(await ConvertRangeAsync(segment.LabelIndex, segment.LabelIndex + 1));
            var protectedStatements = await _hoisting.ConvertAsync(StatementsIn(segment));
            if (protectedStatements.Any()) statements.Add(Try(protectedStatements, GoTo(HandlerLabel)));
        }
        statements.AddRange(await _hoisting.ConvertAsync(StatementsIn(UnprotectedRange(_plan))));
        statements.Add(Label(HandlerLabel));
        statements.AddRange(await ConvertHandlerAsync());
        statements.AddRange(await _hoisting.ConvertAsync(StatementsIn(_plan.Tail)));
        return statements;
    }

    /// <summary>
    /// VB clears <c>Err</c> when the method is left while in a handler.
    /// </summary>
    private async Task<List<StatementSyntax>> ConvertHandlerAsync()
    {
        var handler = await _context.ConvertHandlerAsync(_model.Statements.Skip(HandlerLabelIndex + 1).Take(_plan.Handler.EndIndex - HandlerLabelIndex - 1), new[] { _model.Statements[HandlerLabelIndex] });
        if (!_err.IsMaintained) return handler;
        var withClearedErr = ClearErrOnReturnRewriter.Rewrite(handler, _err, _context.Visitor.ReturnVariable).ToList();
        if (_plan.Handler.Exits.Any(e => e.Kind == HandlerExitKind.FallsOffEnd) && StatementReachability.EndIsReachable(withClearedErr)) withClearedErr.AddRange(_err.Clear());
        return withClearedErr;
    }

    private TryStatementSyntax Try(IEnumerable<StatementSyntax> statements, IEnumerable<StatementSyntax> catchStatements) =>
        SyntaxFactory.TryStatement(SyntaxFactory.Block(statements), SyntaxFactory.SingletonList(_err.CatchClause(_exceptionVariableName, catchStatements)), null);

    /// <summary>
    /// Not in a <c>try</c>, but its locals still need hoisting when the handler, emitted before it, uses them.
    /// </summary>
    private static StatementRange UnprotectedRange(StructuredPlan plan) => new(-1, plan.UnprotectedStart, plan.Handler.LabelIndex);

    private Task<List<StatementSyntax>> ConvertRangeAsync(int start, int end) =>
        _context.ConvertAsync(_model.Statements.Skip(start).Take(end - start));

    private IEnumerable<VBSyntax.StatementSyntax> StatementsIn(StatementRange segment) =>
        _model.Statements.Skip(segment.Start).Take(segment.End - segment.Start);

    private static StatementSyntax[] GoTo(string label) =>
        new StatementSyntax[] { SyntaxFactory.GotoStatement(SyntaxKind.GotoStatement, ValidSyntaxFactory.IdentifierName(label)) };

    private static StatementSyntax Label(string label) =>
        SyntaxFactory.LabeledStatement(CommonConversions.CsEscapedIdentifier(label), SyntaxFactory.EmptyStatement());

    private static bool IsLabel(StatementSyntax statement, string label) =>
        statement is LabeledStatementSyntax labeled && labeled.Identifier.ValueText == label;

    /// <summary>
    /// The labels the handler leaves to are only needed when something still jumps to them.
    /// </summary>
    private List<StatementSyntax> WithoutUnreferencedLabels(List<StatementSyntax> statements)
    {
        var removableLabels = new HashSet<string>(_plan.Handler.ExitLabels.Append(_plan.Handler.Label).Select(_model.LabelText));
        var jumpedTo = new HashSet<string>(statements.SelectMany(s => s.DescendantNodesAndSelf()).OfType<GotoStatementSyntax>()
            .Select(g => g.Expression).OfType<IdentifierNameSyntax>().Select(i => i.Identifier.ValueText));
        return statements.Where(s => s is not LabeledStatementSyntax { Statement: EmptyStatementSyntax } label ||
                                     !removableLabels.Contains(label.Identifier.ValueText) || jumpedTo.Contains(label.Identifier.ValueText))
            .ToList();
    }
}
