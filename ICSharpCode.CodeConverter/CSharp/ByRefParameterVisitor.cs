using System;
using System.Collections.Generic;
using System.Text;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;

namespace ICSharpCode.CodeConverter.CSharp
{
    public class ByRefParameterVisitor : VBasic.VisualBasicSyntaxVisitor<SyntaxList<StatementSyntax>>
    {
        private readonly VBasic.VisualBasicSyntaxVisitor<SyntaxList<StatementSyntax>> _wrappedVisitor;
        private readonly AdditionalLocals _additionalLocals;
        private readonly SemanticModel _semanticModel;
        private readonly HashSet<string> _generatedNames;

        public ByRefParameterVisitor(VBasic.VisualBasicSyntaxVisitor<SyntaxList<StatementSyntax>> wrappedVisitor, AdditionalLocals additionalLocals,
            SemanticModel semanticModel, HashSet<string> generatedNames)
        {
            _wrappedVisitor = wrappedVisitor;
            _additionalLocals = additionalLocals;
            _semanticModel = semanticModel;
            _generatedNames = generatedNames;
        }

        public override SyntaxList<StatementSyntax> DefaultVisit(SyntaxNode node)
        {
            // If we don't insert the new variables in the right place, don't insert them at all
            _additionalLocals.PopScope();

            throw new NotImplementedException($"Conversion for {VBasic.VisualBasicExtensions.Kind(node)} not implemented, please report this issue")
                .WithNodeInformation(node);
        }

        private SyntaxList<StatementSyntax> AddLocalVariables(VBasic.VisualBasicSyntaxNode node)
        {
            _additionalLocals.PushScope();
            IEnumerable<SyntaxNode> csNodes = _wrappedVisitor.Visit(node);
                
            var additionalDeclarations = new List<StatementSyntax>();

            if (_additionalLocals.Count() > 0) {
                var newNames = new Dictionary<string, string>();
                csNodes = csNodes.Select(csNode => csNode.ReplaceNodes(csNode.GetAnnotatedNodes(AdditionalLocals.Annotation), (an, _) => {
                    var id = (an as IdentifierNameSyntax).Identifier.ValueText;
                    newNames[id] = NameGenerator.GetUniqueVariableNameInScope(_semanticModel, _generatedNames, node, _additionalLocals[id].Prefix);
                    return SyntaxFactory.IdentifierName(newNames[id]);
                })).ToList();

                foreach (var additionalLocal in _additionalLocals) {
                    var decl = CommonConversions.CreateVariableDeclarationAndAssignment(newNames[additionalLocal.Key], additionalLocal.Value.Initializer, additionalLocal.Value.Type);
                    additionalDeclarations.Add(SyntaxFactory.LocalDeclarationStatement(decl));
                }
            }
            _additionalLocals.PopScope();

            return SyntaxFactory.List(additionalDeclarations.Concat(csNodes));
        }

        public override SyntaxList<StatementSyntax> VisitAddRemoveHandlerStatement(VBSyntax.AddRemoveHandlerStatementSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitAssignmentStatement(VBSyntax.AssignmentStatementSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitCallStatement(VBSyntax.CallStatementSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitContinueStatement(VBSyntax.ContinueStatementSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitDoLoopBlock(VBSyntax.DoLoopBlockSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitEraseStatement(VBSyntax.EraseStatementSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitErrorStatement(VBSyntax.ErrorStatementSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitExitStatement(VBSyntax.ExitStatementSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitExpressionStatement(VBSyntax.ExpressionStatementSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitForBlock(VBSyntax.ForBlockSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitForEachBlock(VBSyntax.ForEachBlockSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitGoToStatement(VBSyntax.GoToStatementSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitLabelStatement(VBSyntax.LabelStatementSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitLocalDeclarationStatement(VBSyntax.LocalDeclarationStatementSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitMultiLineIfBlock(VBSyntax.MultiLineIfBlockSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitOnErrorGoToStatement(VBSyntax.OnErrorGoToStatementSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitOnErrorResumeNextStatement(VBSyntax.OnErrorResumeNextStatementSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitPrintStatement(VBSyntax.PrintStatementSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitRaiseEventStatement(VBSyntax.RaiseEventStatementSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitReDimStatement(VBSyntax.ReDimStatementSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitResumeStatement(VBSyntax.ResumeStatementSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitReturnStatement(VBSyntax.ReturnStatementSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitSelectBlock(VBSyntax.SelectBlockSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitSingleLineIfStatement(VBSyntax.SingleLineIfStatementSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitStopOrEndStatement(VBSyntax.StopOrEndStatementSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitSyncLockBlock(VBSyntax.SyncLockBlockSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitThrowStatement(VBSyntax.ThrowStatementSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitTryBlock(VBSyntax.TryBlockSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitUsingBlock(VBSyntax.UsingBlockSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitWhileBlock(VBSyntax.WhileBlockSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitWithBlock(VBSyntax.WithBlockSyntax node) => AddLocalVariables(node);
        public override SyntaxList<StatementSyntax> VisitYieldStatement(VBSyntax.YieldStatementSyntax node) => AddLocalVariables(node);
    }
}
