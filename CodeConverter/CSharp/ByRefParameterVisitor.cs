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
    public class ByRefParameterVisitor : VBasic.VisualBasicSyntaxVisitor<Task<SyntaxList<StatementSyntax>>>
    {
        private readonly VBasic.VisualBasicSyntaxVisitor<Task<SyntaxList<StatementSyntax>>> _wrappedVisitor;
        private readonly AdditionalLocals _additionalLocals;
        private readonly SemanticModel _semanticModel;
        private readonly HashSet<string> _generatedNames;

        public ByRefParameterVisitor(VBasic.VisualBasicSyntaxVisitor<Task<SyntaxList<StatementSyntax>>> wrappedVisitor, AdditionalLocals additionalLocals,
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
            IEnumerable<SyntaxNode> csNodes;
            List<StatementSyntax> additionalDeclarations;
            try {
                (csNodes, additionalDeclarations) = await CreateLocals(node);
            } finally {
                _additionalLocals.PopScope();
            }

            return SyntaxFactory.List(additionalDeclarations.Concat(csNodes));
        }

        private async Task<(IEnumerable<SyntaxNode> csNodes, List<StatementSyntax> additionalDeclarations)> CreateLocals(VBasic.VisualBasicSyntaxNode node)
        {
            IEnumerable<SyntaxNode> csNodes = await _wrappedVisitor.Visit(node);

            var additionalDeclarations = new List<StatementSyntax>();

            if (_additionalLocals.Count() > 0)
            {
                var newNames = new Dictionary<string, string>();
                csNodes = csNodes.Select(csNode => csNode.ReplaceNodes(csNode.GetAnnotatedNodes(AdditionalLocals.Annotation),
                    (an, _) =>
                    {
                        var id = ((IdentifierNameSyntax) an).Identifier.ValueText;
                        newNames[id] = NameGenerator.GetUniqueVariableNameInScope(_semanticModel, _generatedNames, node,
                            _additionalLocals[id].Prefix);
                        return SyntaxFactory.IdentifierName(newNames[id]);
                    })).ToList();

                foreach (var additionalLocal in _additionalLocals)
                {
                    var decl = CommonConversions.CreateVariableDeclarationAndAssignment(newNames[additionalLocal.Key],
                        additionalLocal.Value.Initializer, additionalLocal.Value.Type);
                    additionalDeclarations.Add(SyntaxFactory.LocalDeclarationStatement(decl));
                }
            }

            return (csNodes, additionalDeclarations);
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
