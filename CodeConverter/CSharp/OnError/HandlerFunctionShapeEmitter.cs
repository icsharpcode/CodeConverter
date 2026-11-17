using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// Emits README.md shape 3: the handler becomes a local function and each guard's <c>catch</c> calls it, then resumes, retries or leaves as it says.
/// </summary>
/// <remarks>
/// The function is declared right after the first <c>On Error GoTo</c>, so what it captures is declared before it: locals declared later that the handler uses are declared there too.
/// An exception thrown by the handler propagates out of the guard's <c>catch</c>, as VB's filter lets an error in a handler propagate.
/// </remarks>
internal sealed class HandlerFunctionShapeEmitter
{
    private readonly OnErrorConversionContext _context;
    private readonly OnErrorModel _model;
    private readonly HandlerFunctionPlan _plan;
    private readonly ErrStatements _err;
    private readonly string _functionName;
    private readonly string _resultVariable;
    private readonly bool _declaresResultVariable;
    private readonly StatementGuards _guards;

    public HandlerFunctionShapeEmitter(OnErrorConversionContext context, OnErrorModel model, HandlerFunctionPlan plan, ErrStatements err)
    {
        _context = context;
        _model = model;
        _plan = plan;
        _err = err;
        var first = model.Statements.First();
        var visitor = context.Visitor;
        _functionName = visitor.GetUniqueVariableNameInScope(first, model.LabelText(plan.Handler.Label) + FunctionNameSuffix(plan.Ways));
        _declaresResultVariable = plan.ReturnsValue && !visitor.HasReturnVariable;
        _resultVariable = visitor.HasReturnVariable ? visitor.ReturnVariable.Identifier.ValueText : _declaresResultVariable ? visitor.GetUniqueVariableNameInScope(first, "result") : null;
        var exitStatement = _resultVariable != null && plan.ReturnsValue
            ? SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(_resultVariable))
            : visitor.CreateMethodExitStatement(first);
        var points = HandlerFunctionPlanner.CreateResumePoints(model, plan.Handler, context.SemanticModel);
        var exceptionName = err.IsMaintained ? visitor.GetUniqueVariableNameInScope(first, "ex") : null;
        _guards = new StatementGuards(points, new HandlerFunctionGuardPolicy(plan, _functionName, exitStatement), err,
            new DeclarationSplitter(context.SemanticModel, context.CommonConversions), context.SemanticModel, visitor.GetUniqueVariableNameInScope, exceptionName,
            visitor.TryHoistOutOfLoop, DeclaredAtTop());
    }

    /// <summary>The suffix says what <c>true</c> means, for the functions that return a <c>bool</c>.</summary>
    private static string FunctionNameSuffix(HandlerWays ways) => ways switch {
        HandlerWays.ResumeNext => "",
        HandlerWays.ExitOrResumeNext => "Exits",
        _ => "Retries"
    };

    /// <summary>Locals declared after the first <c>On Error</c> at method level that the handler uses.</summary>
    private Func<ILocalSymbol, bool> DeclaredAtTop()
    {
        var semanticModel = _context.SemanticModel;
        var used = _model.Statements.Skip(_plan.Handler.LabelIndex).Take(_plan.Handler.EndIndex - _plan.Handler.LabelIndex)
            .SelectMany(s => s.DescendantNodesAndSelf()).OfType<VBSyntax.IdentifierNameSyntax>()
            .Select(i => semanticModel.GetSymbolInfo(i).Symbol).OfType<ILocalSymbol>().ToArray();
        var declaredLater = _model.Statements.Skip(_plan.FirstOnErrorIndex + 1).Where((_, i) => !_plan.Handler.Contains(i + _plan.FirstOnErrorIndex + 1))
            .OfType<VBSyntax.LocalDeclarationStatementSyntax>().SelectMany(d => d.Declarators).SelectMany(d => d.Names)
            .Select(n => semanticModel.GetDeclaredSymbol(n)).OfType<ILocalSymbol>().ToArray();
        return local => used.Contains(local, SymbolEqualityComparer.Default) && declaredLater.Contains(local, SymbolEqualityComparer.Default);
    }

    public async Task<IReadOnlyCollection<StatementSyntax>> EmitAsync()
    {
        var visitor = _context.Visitor;
        visitor.OnErrorStatementConverter = new HandlerFunctionStatementConverter(_err, _plan);
        visitor.DecorateStatementVisitor(wrapped => new GuardingStatementVisitor(wrapped, _guards));

        var before = await ConvertRangeAsync(0, _plan.FirstOnErrorIndex + 1);
        var function = await ConvertHandlerAsync();
        var after = await _context.ConvertAsync(_model.Statements.Where((_, i) => i > _plan.FirstOnErrorIndex && !_plan.Handler.Contains(i)));
        return MissingReturn.Add(_context, _model, _guards, before.Concat(_guards.TopDeclarations).Concat(DeclareResultVariable()).Append(function).Concat(after).ToList()).ToList();
    }

    private IEnumerable<StatementSyntax> DeclareResultVariable()
    {
        if (!_declaresResultVariable) yield break;
        var returnStatement = _model.Statements.Skip(_plan.Handler.LabelIndex).SelectMany(s => s.DescendantNodesAndSelf()).OfType<VBSyntax.ReturnStatementSyntax>().First(r => r.Expression != null);
        var type = _context.CommonConversions.GetTypeSyntax(_context.SemanticModel.GetTypeInfo(returnStatement.Expression).ConvertedType);
        yield return SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(type, SyntaxFactory.SingletonSeparatedList(
            SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(_resultVariable), null,
                SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression))))));
    }

    private async Task<StatementSyntax> ConvertHandlerAsync()
    {
        var body = await ConvertRangeAsync(_plan.Handler.LabelIndex + 1, _plan.Handler.EndIndex);
        var exitValue = SyntaxFactory.LiteralExpression(_plan.ExitValue ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);
        var statements = HandlerExitRewriter.Rewrite(body, _err, _resultVariable, exitValue).ToList();
        if (_plan.Handler.Exits.Any(e => e.Kind == HandlerExitKind.FallsOffEnd)) statements.AddRange(HandlerExitRewriter.Exit(_err, exitValue));
        var returnType = _plan.Ways == HandlerWays.ResumeNext
            ? SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword))
            : SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword));
        return SyntaxFactory.LocalFunctionStatement(returnType, _functionName).WithBody(SyntaxFactory.Block(statements));
    }

    private Task<List<StatementSyntax>> ConvertRangeAsync(int start, int end) =>
        _context.ConvertAsync(_model.Statements.Skip(start).Take(end - start));
}
