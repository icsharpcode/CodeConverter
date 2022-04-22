using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace ICSharpCode.CodeConverter.CSharp;

/// <summary>
/// Stores state to allow adding a syntax node to the surrounding scope (by sharing an instance of AdditionalLocals)
/// e.g. Add a local variable declaration in the scope immediately before the expression currently being visited.
/// e.g. Add a member declaration in the scope immediately before the member currently being visited.
/// The current implementation uses a guid variable name, then replaces it later with a unique name by tracking the annotation added to it.
/// </summary>
internal class PerScopeStateVisitorDecorator : VBasic.VisualBasicSyntaxVisitor<Task<SyntaxList<StatementSyntax>>>
{
    private readonly VBasic.VisualBasicSyntaxVisitor<Task<SyntaxList<StatementSyntax>>> _wrappedVisitor;
    private readonly PerScopeState _additionalLocals;
    private readonly SemanticModel _semanticModel;
    private readonly HashSet<string> _generatedNames;

    public PerScopeStateVisitorDecorator(VBasic.VisualBasicSyntaxVisitor<Task<SyntaxList<StatementSyntax>>> wrappedVisitor, PerScopeState additionalLocals,
        SemanticModel semanticModel, HashSet<string> generatedNames)
    {
        _wrappedVisitor = wrappedVisitor;
        _additionalLocals = additionalLocals;
        _semanticModel = semanticModel;
        _generatedNames = generatedNames;
    }

    public override Task<SyntaxList<StatementSyntax>> DefaultVisit(SyntaxNode node)
    {
        throw new NotImplementedException($"Conversion for {VBasic.VisualBasicExtensions.Kind(node)} not implemented, please report this issue")
            .WithNodeInformation(node);
    }

    private async Task<SyntaxList<StatementSyntax>> AddLocalVariablesAsync(VBasic.VisualBasicSyntaxNode node, VBasic.SyntaxKind exitableType = default, bool isBreakableInCs = false)
    {
        _additionalLocals.PushScope(exitableType, isBreakableInCs);
        try {
            var convertedStatements = await _wrappedVisitor.Visit(node);
            var withLocals = await _additionalLocals.CreateLocalsAsync(node, convertedStatements, _generatedNames, _semanticModel);
            var allStatements = _additionalLocals.CreateStatements(node, withLocals, _generatedNames, _semanticModel);

            if (isBreakableInCs && exitableType == VBasic.SyntaxKind.TryKeyword) {
                var doOnce = SyntaxFactory.DoStatement(SyntaxFactory.Block(allStatements), CommonConversions.Literal(false));
                allStatements = SyntaxFactory.SingletonList<StatementSyntax>(doOnce);
            }
            return allStatements;
        } finally {
            _additionalLocals.PopScope();
        }
    }

    public override Task<SyntaxList<StatementSyntax>> VisitAddRemoveHandlerStatement(VBSyntax.AddRemoveHandlerStatementSyntax node) => AddLocalVariablesAsync(node);
    public override Task<SyntaxList<StatementSyntax>> VisitAssignmentStatement(VBSyntax.AssignmentStatementSyntax node) => AddLocalVariablesAsync(node);
    public override Task<SyntaxList<StatementSyntax>> VisitCallStatement(VBSyntax.CallStatementSyntax node) => AddLocalVariablesAsync(node);
    public override Task<SyntaxList<StatementSyntax>> VisitContinueStatement(VBSyntax.ContinueStatementSyntax node) => AddLocalVariablesAsync(node);
    public override Task<SyntaxList<StatementSyntax>> VisitDoLoopBlock(VBSyntax.DoLoopBlockSyntax node) => AddLocalVariablesAsync(node, VBasic.SyntaxKind.DoKeyword, true);
    public override Task<SyntaxList<StatementSyntax>> VisitEraseStatement(VBSyntax.EraseStatementSyntax node) => AddLocalVariablesAsync(node);
    public override Task<SyntaxList<StatementSyntax>> VisitErrorStatement(VBSyntax.ErrorStatementSyntax node) => AddLocalVariablesAsync(node);
    public override Task<SyntaxList<StatementSyntax>> VisitExitStatement(VBSyntax.ExitStatementSyntax node) => AddLocalVariablesAsync(node);

    public override Task<SyntaxList<StatementSyntax>> VisitExpressionStatement(VBSyntax.ExpressionStatementSyntax node) => AddLocalVariablesAsync(node);
    public override Task<SyntaxList<StatementSyntax>> VisitForBlock(VBSyntax.ForBlockSyntax node) => AddLocalVariablesAsync(node, VBasic.SyntaxKind.ForKeyword, true);
    public override Task<SyntaxList<StatementSyntax>> VisitForEachBlock(VBSyntax.ForEachBlockSyntax node) => AddLocalVariablesAsync(node, VBasic.SyntaxKind.ForKeyword, true);
    public override Task<SyntaxList<StatementSyntax>> VisitGoToStatement(VBSyntax.GoToStatementSyntax node) => AddLocalVariablesAsync(node);
    public override Task<SyntaxList<StatementSyntax>> VisitLabelStatement(VBSyntax.LabelStatementSyntax node) => AddLocalVariablesAsync(node);
    public override Task<SyntaxList<StatementSyntax>> VisitLocalDeclarationStatement(VBSyntax.LocalDeclarationStatementSyntax node) => AddLocalVariablesAsync(node);
    public override Task<SyntaxList<StatementSyntax>> VisitMultiLineIfBlock(VBSyntax.MultiLineIfBlockSyntax node) => AddLocalVariablesAsync(node);
    public override Task<SyntaxList<StatementSyntax>> VisitOnErrorGoToStatement(VBSyntax.OnErrorGoToStatementSyntax node) => AddLocalVariablesAsync(node);
    public override Task<SyntaxList<StatementSyntax>> VisitOnErrorResumeNextStatement(VBSyntax.OnErrorResumeNextStatementSyntax node) => AddLocalVariablesAsync(node);
    public override Task<SyntaxList<StatementSyntax>> VisitPrintStatement(VBSyntax.PrintStatementSyntax node) => AddLocalVariablesAsync(node);
    public override Task<SyntaxList<StatementSyntax>> VisitRaiseEventStatement(VBSyntax.RaiseEventStatementSyntax node) => AddLocalVariablesAsync(node);
    public override Task<SyntaxList<StatementSyntax>> VisitReDimStatement(VBSyntax.ReDimStatementSyntax node) => AddLocalVariablesAsync(node);
    public override Task<SyntaxList<StatementSyntax>> VisitResumeStatement(VBSyntax.ResumeStatementSyntax node) => AddLocalVariablesAsync(node);
    public override Task<SyntaxList<StatementSyntax>> VisitReturnStatement(VBSyntax.ReturnStatementSyntax node) => AddLocalVariablesAsync(node);
    public override Task<SyntaxList<StatementSyntax>> VisitSelectBlock(VBSyntax.SelectBlockSyntax node) => AddLocalVariablesAsync(node, VBasic.SyntaxKind.SelectKeyword, true);
    public override Task<SyntaxList<StatementSyntax>> VisitSingleLineIfStatement(VBSyntax.SingleLineIfStatementSyntax node) => AddLocalVariablesAsync(node);
    public override Task<SyntaxList<StatementSyntax>> VisitStopOrEndStatement(VBSyntax.StopOrEndStatementSyntax node) => AddLocalVariablesAsync(node);
    public override Task<SyntaxList<StatementSyntax>> VisitSyncLockBlock(VBSyntax.SyncLockBlockSyntax node) => AddLocalVariablesAsync(node);
    public override Task<SyntaxList<StatementSyntax>> VisitThrowStatement(VBSyntax.ThrowStatementSyntax node) => AddLocalVariablesAsync(node);
    public override Task<SyntaxList<StatementSyntax>> VisitTryBlock(VBSyntax.TryBlockSyntax node)
    {
        var isExited = node.DescendantNodes(n => n == node || n is not VBSyntax.TryBlockSyntax).OfType<VBSyntax.ExitStatementSyntax>().Any(e => VBasic.VisualBasicExtensions.Kind(e.BlockKeyword) == VBasic.SyntaxKind.TryKeyword);
        return AddLocalVariablesAsync(node, VBasic.SyntaxKind.TryKeyword, isExited);
    }

    public override Task<SyntaxList<StatementSyntax>> VisitUsingBlock(VBSyntax.UsingBlockSyntax node) => AddLocalVariablesAsync(node);
    public override Task<SyntaxList<StatementSyntax>> VisitWhileBlock(VBSyntax.WhileBlockSyntax node) => AddLocalVariablesAsync(node, VBasic.SyntaxKind.WhileKeyword, true);
    public override Task<SyntaxList<StatementSyntax>> VisitWithBlock(VBSyntax.WithBlockSyntax node) => AddLocalVariablesAsync(node);
    public override Task<SyntaxList<StatementSyntax>> VisitYieldStatement(VBSyntax.YieldStatementSyntax node) => AddLocalVariablesAsync(node);
}