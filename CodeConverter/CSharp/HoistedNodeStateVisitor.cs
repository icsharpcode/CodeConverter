using System;
using System.Collections.Generic;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;

namespace ICSharpCode.CodeConverter.CSharp
{
    /// <summary>
    /// Stores state to allow adding a syntax node to the surrounding scope (by sharing an instance of AdditionalLocals)
    /// e.g. Add a local variable declaration in the scope immediately before the expression currently being visited.
    /// e.g. Add a member declaration in the scope immediately before the member currently being visited.
    /// The current implementation uses a guid variable name, then replaces it later with a unique name by tracking the annotation added to it.
    /// </summary>
    internal class HoistedNodeStateVisitor : VBasic.VisualBasicSyntaxVisitor<Task<SyntaxList<StatementSyntax>>>
    {
        private readonly VBasic.VisualBasicSyntaxVisitor<Task<SyntaxList<StatementSyntax>>> _wrappedVisitor;
        private readonly HoistedNodeState _additionalLocals;
        private readonly SemanticModel _semanticModel;
        private readonly HashSet<string> _generatedNames;

        public HoistedNodeStateVisitor(VBasic.VisualBasicSyntaxVisitor<Task<SyntaxList<StatementSyntax>>> wrappedVisitor, HoistedNodeState additionalLocals,
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

        private async Task<SyntaxList<StatementSyntax>> AddLocalVariablesAsync(VBasic.VisualBasicSyntaxNode node)
        {
            _additionalLocals.PushScope();
            try {
                var csNodes = await _wrappedVisitor.Visit(node);
                var statements = await _additionalLocals.CreateLocalsAsync(node, csNodes, _generatedNames, _semanticModel);
                return _additionalLocals.CreateStatements(node, statements, _generatedNames, _semanticModel);
            } finally {
                _additionalLocals.PopScope();
            }
        }

        public override Task<SyntaxList<StatementSyntax>> VisitAddRemoveHandlerStatement(VBSyntax.AddRemoveHandlerStatementSyntax node) => AddLocalVariablesAsync(node);
        public override Task<SyntaxList<StatementSyntax>> VisitAssignmentStatement(VBSyntax.AssignmentStatementSyntax node) => AddLocalVariablesAsync(node);
        public override Task<SyntaxList<StatementSyntax>> VisitCallStatement(VBSyntax.CallStatementSyntax node) => AddLocalVariablesAsync(node);
        public override Task<SyntaxList<StatementSyntax>> VisitContinueStatement(VBSyntax.ContinueStatementSyntax node) => AddLocalVariablesAsync(node);
        public override Task<SyntaxList<StatementSyntax>> VisitDoLoopBlock(VBSyntax.DoLoopBlockSyntax node) => AddLocalVariablesAsync(node);
        public override Task<SyntaxList<StatementSyntax>> VisitEraseStatement(VBSyntax.EraseStatementSyntax node) => AddLocalVariablesAsync(node);
        public override Task<SyntaxList<StatementSyntax>> VisitErrorStatement(VBSyntax.ErrorStatementSyntax node) => AddLocalVariablesAsync(node);
        public override Task<SyntaxList<StatementSyntax>> VisitExitStatement(VBSyntax.ExitStatementSyntax node) => AddLocalVariablesAsync(node);
        public override Task<SyntaxList<StatementSyntax>> VisitExpressionStatement(VBSyntax.ExpressionStatementSyntax node) => AddLocalVariablesAsync(node);
        public override Task<SyntaxList<StatementSyntax>> VisitForBlock(VBSyntax.ForBlockSyntax node) => AddLocalVariablesAsync(node);
        public override Task<SyntaxList<StatementSyntax>> VisitForEachBlock(VBSyntax.ForEachBlockSyntax node) => AddLocalVariablesAsync(node);
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
        public override Task<SyntaxList<StatementSyntax>> VisitSelectBlock(VBSyntax.SelectBlockSyntax node) => AddLocalVariablesAsync(node);
        public override Task<SyntaxList<StatementSyntax>> VisitSingleLineIfStatement(VBSyntax.SingleLineIfStatementSyntax node) => AddLocalVariablesAsync(node);
        public override Task<SyntaxList<StatementSyntax>> VisitStopOrEndStatement(VBSyntax.StopOrEndStatementSyntax node) => AddLocalVariablesAsync(node);
        public override Task<SyntaxList<StatementSyntax>> VisitSyncLockBlock(VBSyntax.SyncLockBlockSyntax node) => AddLocalVariablesAsync(node);
        public override Task<SyntaxList<StatementSyntax>> VisitThrowStatement(VBSyntax.ThrowStatementSyntax node) => AddLocalVariablesAsync(node);
        public override Task<SyntaxList<StatementSyntax>> VisitTryBlock(VBSyntax.TryBlockSyntax node) => AddLocalVariablesAsync(node);
        public override Task<SyntaxList<StatementSyntax>> VisitUsingBlock(VBSyntax.UsingBlockSyntax node) => AddLocalVariablesAsync(node);
        public override Task<SyntaxList<StatementSyntax>> VisitWhileBlock(VBSyntax.WhileBlockSyntax node) => AddLocalVariablesAsync(node);
        public override Task<SyntaxList<StatementSyntax>> VisitWithBlock(VBSyntax.WithBlockSyntax node) => AddLocalVariablesAsync(node);
        public override Task<SyntaxList<StatementSyntax>> VisitYieldStatement(VBSyntax.YieldStatementSyntax node) => AddLocalVariablesAsync(node);
    }
}
