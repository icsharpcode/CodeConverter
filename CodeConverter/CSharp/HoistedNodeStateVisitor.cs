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
        private readonly AdditionalLocals _additionalLocals;
        private readonly SemanticModel _semanticModel;
        private readonly HashSet<string> _generatedNames;

        public HoistedNodeStateVisitor(VBasic.VisualBasicSyntaxVisitor<Task<SyntaxList<StatementSyntax>>> wrappedVisitor, AdditionalLocals additionalLocals,
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

        private async Task<SyntaxList<StatementSyntax>> AddLocalVariables(VBasic.VisualBasicSyntaxNode node)
        {
            _additionalLocals.PushScope();
            try {
                return await CreateLocals(node);
            } finally {
                _additionalLocals.PopScope();
            }
        }

        private async Task<SyntaxList<StatementSyntax>> CreateLocals(VBasic.VisualBasicSyntaxNode node)
        {
            IEnumerable<StatementSyntax> csNodes = await _wrappedVisitor.Visit(node);

            var preDeclarations = new List<StatementSyntax>();
            var postAssignments = new List<StatementSyntax>();

            var additionalDeclarationInfo = _additionalLocals.GetDeclarations();
            var newNames = additionalDeclarationInfo.ToDictionary(l => l.Id, l =>
                NameGenerator.GetUniqueVariableNameInScope(_semanticModel, _generatedNames, node, l.Prefix)
            );
            if (additionalDeclarationInfo.Count() > 0) {
                foreach (var additionalLocal in additionalDeclarationInfo) {
                    var decl = CommonConversions.CreateVariableDeclarationAndAssignment(newNames[additionalLocal.Id],
                        additionalLocal.Initializer, additionalLocal.Type);
                    preDeclarations.Add(SyntaxFactory.LocalDeclarationStatement(decl));
                }
            }
            var additionalAssignmentInfo = _additionalLocals.GetPostAssignments();
            if (additionalAssignmentInfo.Count() > 0)
            {

                foreach (var additionalAssignment in additionalAssignmentInfo)
                {
                    var assign = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, additionalAssignment.Expression, additionalAssignment.IdentifierName);
                    postAssignments.Add(SyntaxFactory.ExpressionStatement(assign));
                }
            }

            var statementsWithUpdatedIds = AdditionalDeclaration.ReplaceNames(preDeclarations.Concat(csNodes).Concat(postAssignments), newNames);

            return SyntaxFactory.List(statementsWithUpdatedIds);
        }

        public override Task<SyntaxList<StatementSyntax>> VisitAddRemoveHandlerStatement(VBSyntax.AddRemoveHandlerStatementSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitAssignmentStatement(VBSyntax.AssignmentStatementSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitCallStatement(VBSyntax.CallStatementSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitContinueStatement(VBSyntax.ContinueStatementSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitDoLoopBlock(VBSyntax.DoLoopBlockSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitEraseStatement(VBSyntax.EraseStatementSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitErrorStatement(VBSyntax.ErrorStatementSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitExitStatement(VBSyntax.ExitStatementSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitExpressionStatement(VBSyntax.ExpressionStatementSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitForBlock(VBSyntax.ForBlockSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitForEachBlock(VBSyntax.ForEachBlockSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitGoToStatement(VBSyntax.GoToStatementSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitLabelStatement(VBSyntax.LabelStatementSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitLocalDeclarationStatement(VBSyntax.LocalDeclarationStatementSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitMultiLineIfBlock(VBSyntax.MultiLineIfBlockSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitOnErrorGoToStatement(VBSyntax.OnErrorGoToStatementSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitOnErrorResumeNextStatement(VBSyntax.OnErrorResumeNextStatementSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitPrintStatement(VBSyntax.PrintStatementSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitRaiseEventStatement(VBSyntax.RaiseEventStatementSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitReDimStatement(VBSyntax.ReDimStatementSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitResumeStatement(VBSyntax.ResumeStatementSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitReturnStatement(VBSyntax.ReturnStatementSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitSelectBlock(VBSyntax.SelectBlockSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitSingleLineIfStatement(VBSyntax.SingleLineIfStatementSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitStopOrEndStatement(VBSyntax.StopOrEndStatementSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitSyncLockBlock(VBSyntax.SyncLockBlockSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitThrowStatement(VBSyntax.ThrowStatementSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitTryBlock(VBSyntax.TryBlockSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitUsingBlock(VBSyntax.UsingBlockSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitWhileBlock(VBSyntax.WhileBlockSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitWithBlock(VBSyntax.WithBlockSyntax node) => AddLocalVariables(node);
        public override Task<SyntaxList<StatementSyntax>> VisitYieldStatement(VBSyntax.YieldStatementSyntax node) => AddLocalVariables(node);
    }
}
