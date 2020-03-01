using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using CS = Microsoft.CodeAnalysis.CSharp;
using CSS = Microsoft.CodeAnalysis.CSharp.Syntax;
using ExpressionSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax;
using IdentifierNameSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.IdentifierNameSyntax;
using LiteralExpressionSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.LiteralExpressionSyntax;
using QualifiedNameSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.QualifiedNameSyntax;
using StatementSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.StatementSyntax;
using SyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory;
using SyntaxKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;
using TypeSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeSyntax;
using UsingStatementSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.UsingStatementSyntax;
using VariableDeclaratorSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.VariableDeclaratorSyntax;

namespace ICSharpCode.CodeConverter.VB
{
    internal class MethodBodyExecutableStatementVisitor : CS.CSharpSyntaxVisitor<SyntaxList<StatementSyntax>>
    {
        private SemanticModel _semanticModel;
        private readonly CommentConvertingVisitorWrapper<VisualBasicSyntaxNode> _nodesVisitor;
        private readonly CommonConversions _commonConversions;
        private readonly Stack<BlockInfo> _blockInfo = new Stack<BlockInfo>(); // currently only works with switch blocks
        private int _switchCount = 0;
        public bool IsIterator { get; private set; }

        private class BlockInfo
        {
            public readonly List<VisualBasicSyntaxNode> GotoCaseExpressions = new List<VisualBasicSyntaxNode>();
        }
        public CommentConvertingMethodBodyVisitor CommentConvertingVisitor { get; }

        public MethodBodyExecutableStatementVisitor(SemanticModel semanticModel,
            CommentConvertingVisitorWrapper<VisualBasicSyntaxNode> nodesVisitor, CommonConversions commonConversions)
        {
            this._semanticModel = semanticModel;
            this._nodesVisitor = nodesVisitor;
            _commonConversions = commonConversions;
            CommentConvertingVisitor = new CommentConvertingMethodBodyVisitor(this);
        }

        public override SyntaxList<StatementSyntax> DefaultVisit(SyntaxNode node)
        {
            throw new NotImplementedException($"Conversion for {CS.CSharpExtensions.Kind(node)} not implemented, please report this issue")
                .WithNodeInformation(node);
        }

        public override SyntaxList<StatementSyntax> VisitLocalDeclarationStatement(CSS.LocalDeclarationStatementSyntax node)
        {
            var modifiers = CommonConversions.ConvertModifiers(node.Modifiers, TokenContext.Local);
            if (modifiers.Count == 0)
                modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.DimKeyword));
            return SyntaxFactory.SingletonList<StatementSyntax>(
                SyntaxFactory.LocalDeclarationStatement(
                    modifiers, _commonConversions.RemodelVariableDeclaration(node.Declaration)
                )
            );
        }

        private StatementSyntax ConvertSingleExpression(CSS.ExpressionSyntax node)
        {
            var exprNode = node.Accept(_nodesVisitor);
            if (!(exprNode is StatementSyntax))
                exprNode = SyntaxFactory.ExpressionStatement((ExpressionSyntax)exprNode);

            return (StatementSyntax)exprNode;
        }

        public override SyntaxList<StatementSyntax> VisitExpressionStatement(CSS.ExpressionStatementSyntax node)
        {
            return SyntaxFactory.SingletonList(ConvertSingleExpression(node.Expression));
        }

        public override SyntaxList<StatementSyntax> VisitIfStatement(CSS.IfStatementSyntax node)
        {
            if (node.Else == null && TryConvertIfNotNullRaiseEvent(node, out var stmt)) {
                return SyntaxFactory.SingletonList(stmt);
            }
            var elseIfBlocks = new List<ElseIfBlockSyntax>();
            ElseBlockSyntax elseBlock = null;
            CollectElseBlocks(node, elseIfBlocks, ref elseBlock);

            if (node.Statement is CSS.BlockSyntax) {
                stmt = SyntaxFactory.MultiLineIfBlock(
                    SyntaxFactory.IfStatement((ExpressionSyntax)node.Condition.Accept(_nodesVisitor)).WithThenKeyword(SyntaxFactory.Token(SyntaxKind.ThenKeyword)),
                    ConvertBlock(node.Statement),
                    SyntaxFactory.List(elseIfBlocks),
                    elseBlock
                );
            } else {
                if (elseIfBlocks.Any() || elseBlock != null || !IsSimpleStatement(node.Statement)) {
                    stmt = SyntaxFactory.MultiLineIfBlock(
                         SyntaxFactory.IfStatement((ExpressionSyntax)node.Condition.Accept(_nodesVisitor)).WithThenKeyword(SyntaxFactory.Token(SyntaxKind.ThenKeyword)),
                         ConvertBlock(node.Statement),
                         SyntaxFactory.List(elseIfBlocks),
                         elseBlock
                     );
                } else {
                    stmt = SyntaxFactory.SingleLineIfStatement(
                        (ExpressionSyntax)node.Condition.Accept(_nodesVisitor),
                        ConvertBlock(node.Statement),
                        null
                    ).WithThenKeyword(SyntaxFactory.Token(SyntaxKind.ThenKeyword));
                }
            }
            return SyntaxFactory.SingletonList(stmt);
        }

        private bool IsSimpleStatement(CSS.StatementSyntax statement)
        {
            return statement is CSS.ExpressionStatementSyntax
                || statement is CSS.BreakStatementSyntax
                || statement is CSS.ContinueStatementSyntax
                || statement is CSS.ReturnStatementSyntax
                || statement is CSS.YieldStatementSyntax
                || statement is CSS.ThrowStatementSyntax;
        }

        private bool TryConvertIfNotNullRaiseEvent(CSS.IfStatementSyntax node, out StatementSyntax raiseEventStatement)
        {
            raiseEventStatement = null;
            return TryGetBinaryExpression(node, out var comparisonExpression, CS.SyntaxKind.NotEqualsExpression, CS.SyntaxKind.NullLiteralExpression)
                   && TryConvertRaiseEvent(node.Statement, comparisonExpression, ref raiseEventStatement);
        }

        private static bool TryGetBinaryExpression(CSS.IfStatementSyntax node, out CSS.BinaryExpressionSyntax binaryExpressionSyntax, CS.SyntaxKind notEqualsExpression, CS.SyntaxKind operand)
        {
            binaryExpressionSyntax = TrimParenthesis(node) as CSS.BinaryExpressionSyntax;
            return binaryExpressionSyntax != null
                   && binaryExpressionSyntax.IsKind(notEqualsExpression)
                   && (binaryExpressionSyntax.Left.IsKind(operand) ||
                       binaryExpressionSyntax.Right.IsKind(operand));
        }

        private static CSS.ExpressionSyntax TrimParenthesis(CSS.IfStatementSyntax node)
        {
            var condition = node.Condition;
            while (condition is CSS.ParenthesizedExpressionSyntax pExp) condition = pExp.Expression;
            return condition;
        }

        private bool TryConvertRaiseEvent(CSS.StatementSyntax resultStatement,
            CSS.BinaryExpressionSyntax be, ref StatementSyntax raiseEventStatement)
        {
            CSS.ExpressionStatementSyntax singleStatement;
            if (resultStatement is CSS.BlockSyntax block)
            {
                if (block.Statements.Count != 1)
                    return false;
                singleStatement = block.Statements[0] as CSS.ExpressionStatementSyntax;
            }
            else
            {
                singleStatement = resultStatement as CSS.ExpressionStatementSyntax;
            }

            if (!(singleStatement?.Expression is CSS.InvocationExpressionSyntax singleInvocationExpression))
                return false;

            raiseEventStatement = singleInvocationExpression.Accept(_nodesVisitor) as RaiseEventStatementSyntax;
            return raiseEventStatement != null;
        }

        private void CollectElseBlocks(CSS.IfStatementSyntax node, List<ElseIfBlockSyntax> elseIfBlocks, ref ElseBlockSyntax elseBlock)
        {
            if (node.Else == null) return;
            if (node.Else.Statement is CSS.IfStatementSyntax) {
                var elseIf = (CSS.IfStatementSyntax)node.Else.Statement;
                elseIfBlocks.Add(
                    SyntaxFactory.ElseIfBlock(
                        SyntaxFactory.ElseIfStatement((ExpressionSyntax)elseIf.Condition.Accept(_nodesVisitor)).WithThenKeyword(SyntaxFactory.Token(SyntaxKind.ThenKeyword)),
                        ConvertBlock(elseIf.Statement)
                    )
                );
                CollectElseBlocks(elseIf, elseIfBlocks, ref elseBlock);
            } else {
                SyntaxList<StatementSyntax> statements = ConvertBlock(node.Else.Statement);
                elseBlock = SyntaxFactory.ElseBlock(statements);
            }
        }

        public override SyntaxList<StatementSyntax> VisitSwitchStatement(CSS.SwitchStatementSyntax node)
        {
            StatementSyntax stmt;
            _blockInfo.Push(new BlockInfo());
            try {
                var blocks = node.Sections.OrderBy(IsDefaultSwitchStatement).Select(ConvertSwitchSection).ToArray();
                stmt = SyntaxFactory.SelectBlock(
                    SyntaxFactory.SelectStatement((ExpressionSyntax)node.Expression.Accept(_nodesVisitor)).WithCaseKeyword(SyntaxFactory.Token(SyntaxKind.CaseKeyword)),
                    SyntaxFactory.List(AddLabels(blocks, _blockInfo.Peek().GotoCaseExpressions))
                );
                _switchCount++;
            } finally {
                _blockInfo.Pop();
            }
            return SyntaxFactory.SingletonList(stmt);
        }

        private IEnumerable<CaseBlockSyntax> AddLabels(CaseBlockSyntax[] blocks, List<VisualBasicSyntaxNode> gotoLabels)
        {
            foreach (var block in blocks) {
                var modifiedBlock = block;
                foreach (var caseClause in block.CaseStatement.Cases) {
                    var expression = caseClause is ElseCaseClauseSyntax ? (VisualBasicSyntaxNode)caseClause : caseClause is SimpleCaseClauseSyntax sccs ? sccs.Value : ((RelationalCaseClauseSyntax) caseClause)?.Value;
                    if (gotoLabels.Any(label => label.IsEquivalentTo(expression)))
                        modifiedBlock = modifiedBlock.WithStatements(block.Statements.Insert(0, SyntaxFactory.LabelStatement(MakeGotoSwitchLabel(expression))));
                }
                yield return modifiedBlock;
            }
        }

        private CaseBlockSyntax ConvertSwitchSection(CSS.SwitchSectionSyntax section)
        {
            if (IsDefaultSwitchStatement(section))
                return SyntaxFactory.CaseElseBlock(SyntaxFactory.CaseElseStatement(SyntaxFactory.ElseCaseClause()), ConvertSwitchSectionBlock(section));
            var caseClauseSyntaxes = section.Labels.Select(l => l.Accept(_nodesVisitor));
            var caseStatementSyntax = SyntaxFactory.CaseStatement(SyntaxFactory.SeparatedList(caseClauseSyntaxes.Cast<CaseClauseSyntax>()));
            return SyntaxFactory.CaseBlock(caseStatementSyntax, ConvertSwitchSectionBlock(section));
        }

        private static bool IsDefaultSwitchStatement(CSS.SwitchSectionSyntax c)
        {
            return c.Labels.OfType<CSS.DefaultSwitchLabelSyntax>().Any();
        }

        private SyntaxList<StatementSyntax> ConvertSwitchSectionBlock(CSS.SwitchSectionSyntax section)
        {
            List<StatementSyntax> statements = new List<StatementSyntax>();
            var lastStatement = section.Statements.LastOrDefault();
            foreach (var s in section.Statements) {
                if (s == lastStatement && s is CSS.BreakStatementSyntax)
                    continue;
                statements.AddRange(ConvertBlock(s));
            }
            return SyntaxFactory.List(statements);
        }

        public override SyntaxList<StatementSyntax> VisitDoStatement(CSS.DoStatementSyntax node)
        {
            var condition = (ExpressionSyntax)node.Condition.Accept(_nodesVisitor);
            var stmt = ConvertBlock(node.Statement);
            var block = SyntaxFactory.DoLoopWhileBlock(
                SyntaxFactory.DoStatement(SyntaxKind.SimpleDoStatement),
                stmt,
                SyntaxFactory.LoopStatement(SyntaxKind.LoopWhileStatement, SyntaxFactory.WhileClause(condition))
            );

            return SyntaxFactory.SingletonList<StatementSyntax>(block);
        }

        public override SyntaxList<StatementSyntax> VisitWhileStatement(CSS.WhileStatementSyntax node)
        {
            var condition = (ExpressionSyntax)node.Condition.Accept(_nodesVisitor);
            var stmt = ConvertBlock(node.Statement);
            var block = SyntaxFactory.WhileBlock(
                SyntaxFactory.WhileStatement(condition),
                stmt
            );

            return SyntaxFactory.SingletonList<StatementSyntax>(block);
        }

        public override SyntaxList<StatementSyntax> VisitForStatement(CSS.ForStatementSyntax node)
        {
            StatementSyntax block;
            var convertedStatements = ConvertBlock(node.Statement);
            if (ConvertForToSimpleForNextWithoutStatements(node, out var forBlock)) {
                block = forBlock.WithStatements(convertedStatements);
            } else {
                var stmts = SyntaxFactory.List(convertedStatements)
                    .AddRange(node.Incrementors.Select(ConvertSingleExpression));
                var condition = node.Condition == null ? _commonConversions.Literal(true) : (ExpressionSyntax)node.Condition.Accept(_nodesVisitor);
                block = SyntaxFactory.WhileBlock(
                    SyntaxFactory.WhileStatement(condition),
                    stmts
                );

                var declarations = new List<StatementSyntax>();
                if (node.Declaration != null) {
                    var syntaxTokenList = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.DimKeyword));
                    declarations.Add(SyntaxFactory.LocalDeclarationStatement(syntaxTokenList, _commonConversions.RemodelVariableDeclaration(node.Declaration)));
                }

                return SyntaxFactory.List(declarations.Concat(node.Initializers.Select(ConvertSingleExpression))).Add(block);
            }
            return SyntaxFactory.SingletonList(block);
        }

        private bool ConvertForToSimpleForNextWithoutStatements(CSS.ForStatementSyntax node, out ForBlockSyntax blockWithoutStatements)
        {
            //   ForStatement -> ForNextStatement when for-loop is simple

            // only the following forms of the for-statement are allowed:
            // for (TypeReference name = start; name < oneAfterEnd; name += step)
            // for (name = start; name < oneAfterEnd; name += step)
            // for (TypeReference name = start; name <= end; name += step)
            // for (name = start; name <= end; name += step)
            // for (TypeReference name = start; name > oneAfterEnd; name -= step)
            // for (name = start; name > oneAfterEnd; name -= step)
            // for (TypeReference name = start; name >= end; name -= step)
            // for (name = start; name >= end; name -= step)

            blockWithoutStatements = null;

            // check if the form is valid and collect TypeReference, name, start, end and step
            bool hasVariable = node.Declaration != null && node.Declaration.Variables.Count == 1;
            if (!hasVariable && node.Initializers.Count != 1)
                return false;
            if (node.Incrementors.Count != 1)
                return false;
            var iterator = node.Incrementors.FirstOrDefault()?.Accept(_nodesVisitor) as AssignmentStatementSyntax;
            if (iterator == null || !iterator.IsKind(SyntaxKind.AddAssignmentStatement, SyntaxKind.SubtractAssignmentStatement))
                return false;
            var iteratorIdentifier = iterator.Left as IdentifierNameSyntax;
            if (iteratorIdentifier == null)
                return false;
            var stepExpression = iterator.Right as LiteralExpressionSyntax;
            if (stepExpression == null || !(stepExpression.Token.Value is int))
                return false;
            int step = (int)stepExpression.Token.Value;
            if (SyntaxTokenExtensions.IsKind(iterator.OperatorToken, SyntaxKind.MinusEqualsToken))
                step = -step;

            var condition = node.Condition as CSS.BinaryExpressionSyntax;
            if (condition == null || !(condition.Left is CSS.IdentifierNameSyntax))
                return false;
            if (((CSS.IdentifierNameSyntax)condition.Left).Identifier.IsEquivalentTo(iteratorIdentifier.Identifier))
                return false;

            ExpressionSyntax end;
            if (iterator.IsKind(SyntaxKind.SubtractAssignmentStatement)) {
                if (condition.IsKind(CS.SyntaxKind.GreaterThanOrEqualExpression))
                    end = (ExpressionSyntax)condition.Right.Accept(_nodesVisitor);
                else if (condition.IsKind(CS.SyntaxKind.GreaterThanExpression))
                    end = SyntaxFactory.BinaryExpression(SyntaxKind.AddExpression, (ExpressionSyntax)condition.Right.Accept(_nodesVisitor), SyntaxFactory.Token(SyntaxKind.PlusToken), _commonConversions.Literal(1));
                else return false;
            } else {
                if (condition.IsKind(CS.SyntaxKind.LessThanOrEqualExpression))
                    end = (ExpressionSyntax)condition.Right.Accept(_nodesVisitor);
                else if (condition.IsKind(CS.SyntaxKind.LessThanExpression))
                    end = SyntaxFactory.BinaryExpression(SyntaxKind.SubtractExpression, (ExpressionSyntax)condition.Right.Accept(_nodesVisitor), SyntaxFactory.Token(SyntaxKind.MinusToken), _commonConversions.Literal(1));
                else return false;
            }

            VisualBasicSyntaxNode variable;
            ExpressionSyntax start;
            if (hasVariable) {
                var v = node.Declaration.Variables[0];
                start = (ExpressionSyntax)v.Initializer?.Value.Accept(_nodesVisitor);
                if (start == null)
                    return false;
                variable = SyntaxFactory.VariableDeclarator(
                    SyntaxFactory.SingletonSeparatedList(SyntaxFactory.ModifiedIdentifier(_commonConversions.ConvertIdentifier(v.Identifier))),
                    node.Declaration.Type.IsVar ? null : SyntaxFactory.SimpleAsClause((TypeSyntax)node.Declaration.Type.Accept(_nodesVisitor)),
                    null
                );
            } else {
                var initializer = node.Initializers.FirstOrDefault() as CSS.AssignmentExpressionSyntax;
                if (initializer == null || !initializer.IsKind(CS.SyntaxKind.SimpleAssignmentExpression))
                    return false;
                if (!(initializer.Left is CSS.IdentifierNameSyntax))
                    return false;
                if (((CSS.IdentifierNameSyntax)initializer.Left).Identifier.IsEquivalentTo(iteratorIdentifier.Identifier))
                    return false;
                variable = initializer.Left.Accept(_nodesVisitor);
                start = (ExpressionSyntax)initializer.Right.Accept(_nodesVisitor);
            }

            blockWithoutStatements = SyntaxFactory.ForBlock(
                SyntaxFactory.ForStatement(variable, start, end, step == 1 ? null : SyntaxFactory.ForStepClause(_commonConversions.Literal(step))),
                SyntaxFactory.List<StatementSyntax>(),
                SyntaxFactory.NextStatement()
            );
            return true;
        }

        public override SyntaxList<StatementSyntax> VisitForEachVariableStatement(CSS.ForEachVariableStatementSyntax node)
        {
            var loopVar = node.Variable.Accept(_nodesVisitor);
            var extraStatements = new List<StatementSyntax>();
            if (node.Variable is CSS.DeclarationExpressionSyntax des && des.Designation is CSS.ParenthesizedVariableDesignationSyntax pv) {
                var tupleName = CommonConversions.GetTupleName(pv);
                extraStatements.AddRange(pv.Variables.Select((v, i) => {
                    var initializer = SyntaxFactory.EqualsValue(SyntaxFactory.SimpleMemberAccessExpression(
                        SyntaxFactory.IdentifierName(tupleName),
                        SyntaxFactory.IdentifierName("Item" + (i + 1).ToString())));
                    var variableDeclaratorSyntax = SyntaxFactory.VariableDeclarator(
                        SyntaxFactory.ModifiedIdentifier(SyntaxFactory.Identifier(v.ToString())))
                        .WithInitializer(initializer);
                    return CommonConversions.CreateLocalDeclarationStatement(variableDeclaratorSyntax);
                }));
            }
            return CreateForEachStatement(loopVar, node.Expression, node.Statement, extraStatements.ToArray());
        }

        public override SyntaxList<StatementSyntax> VisitForEachStatement(CSS.ForEachStatementSyntax node)
        {
            VisualBasicSyntaxNode variable;
            if (node.Type.IsVar)
            {
                variable = SyntaxFactory.IdentifierName(_commonConversions.ConvertIdentifier(node.Identifier));
            }
            else
            {
                variable = SyntaxFactory.VariableDeclarator(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.ModifiedIdentifier(_commonConversions.ConvertIdentifier(node.Identifier))),
                    SyntaxFactory.SimpleAsClause((TypeSyntax) node.Type.Accept(_nodesVisitor)),
                    null
                );
            }

            return CreateForEachStatement(variable, node.Expression, node.Statement);
        }

        private SyntaxList<StatementSyntax> CreateForEachStatement(VisualBasicSyntaxNode vbVariable,
            CSS.ExpressionSyntax csExpression, CSS.StatementSyntax csStatement, params StatementSyntax[] prefixExtraVbStatements)
        {
            var expression = (ExpressionSyntax) csExpression.Accept(_nodesVisitor);
            var stmt = ConvertBlock(csStatement, prefixExtraVbStatements);
            var block = SyntaxFactory.ForEachBlock(
                SyntaxFactory.ForEachStatement(vbVariable, expression),
                stmt,
                SyntaxFactory.NextStatement()
            );
            return SyntaxFactory.SingletonList<StatementSyntax>(block);
        }

        public override SyntaxList<StatementSyntax> VisitTryStatement(CSS.TryStatementSyntax node)
        {
            var block = SyntaxFactory.TryBlock(
                ConvertBlock(node.Block),
                SyntaxFactory.List(node.Catches.IndexedSelect(ConvertCatchClause)),
                node.Finally == null ? null : SyntaxFactory.FinallyBlock(ConvertBlock(node.Finally.Block))
            );

            return SyntaxFactory.SingletonList<StatementSyntax>(block);
        }

        private CatchBlockSyntax ConvertCatchClause(int index, CSS.CatchClauseSyntax catchClause)
        {
            var statements = ConvertBlock(catchClause.Block);
            if (catchClause.Declaration == null)
                return SyntaxFactory.CatchBlock(SyntaxFactory.CatchStatement(), statements);
            var type = (TypeSyntax)catchClause.Declaration.Type.Accept(_nodesVisitor);
            string simpleTypeName;
            if (type is QualifiedNameSyntax)
                simpleTypeName = ((QualifiedNameSyntax)type).Right.ToString();
            else
                simpleTypeName = type.ToString();
            return SyntaxFactory.CatchBlock(
                SyntaxFactory.CatchStatement(
                    SyntaxFactory.IdentifierName(SyntaxTokenExtensions.IsKind(catchClause.Declaration.Identifier, CS.SyntaxKind.None) ? SyntaxFactory.Identifier($"__unused{simpleTypeName}{index + 1}__") : _commonConversions.ConvertIdentifier(catchClause.Declaration.Identifier)),
                    SyntaxFactory.SimpleAsClause(type),
                    catchClause.Filter == null ? null : SyntaxFactory.CatchFilterClause((ExpressionSyntax)catchClause.Filter.FilterExpression.Accept(_nodesVisitor))
                ), statements
            );
        }

        public override SyntaxList<StatementSyntax> VisitUsingStatement(CSS.UsingStatementSyntax node)
        {
            UsingStatementSyntax stmt;
            if (node.Declaration == null) {
                stmt = SyntaxFactory.UsingStatement(
                    (ExpressionSyntax)node.Expression?.Accept(_nodesVisitor),
                    SyntaxFactory.SeparatedList<VariableDeclaratorSyntax>()
                );
            } else {
                stmt = SyntaxFactory.UsingStatement(null, _commonConversions.RemodelVariableDeclaration(node.Declaration));
            }
            return SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.UsingBlock(stmt, ConvertBlock(node.Statement)));
        }

        public override SyntaxList<StatementSyntax> VisitLockStatement(CSS.LockStatementSyntax node)
        {
            var stmt = SyntaxFactory.SyncLockStatement(
                (ExpressionSyntax)node.Expression?.Accept(_nodesVisitor)
            );
            return SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.SyncLockBlock(stmt, ConvertBlock(node.Statement)));
        }

        public override SyntaxList<StatementSyntax> VisitLabeledStatement(CSS.LabeledStatementSyntax node)
        {
            return SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.LabelStatement(_commonConversions.ConvertIdentifier(node.Identifier)))
                .AddRange(ConvertBlock(node.Statement));
        }

        private string MakeGotoSwitchLabel(VisualBasicSyntaxNode expression)
        {
            string expressionText;
            if (expression is ElseCaseClauseSyntax)
                expressionText = "Default";
            else
                expressionText = expression.ToString().Replace('.', '_');
            return $"_Select{_switchCount}_Case{expressionText}";
        }

        public override SyntaxList<StatementSyntax> VisitGotoStatement(CSS.GotoStatementSyntax node)
        {
            LabelSyntax label;
            if (node.IsKind(CS.SyntaxKind.GotoCaseStatement, CS.SyntaxKind.GotoDefaultStatement)) {
                if (_blockInfo.Count == 0)
                    throw new InvalidOperationException("goto case/goto default outside switch is illegal!");
                var labelExpression = node.Expression?.Accept(_nodesVisitor) ?? SyntaxFactory.ElseCaseClause();
                _blockInfo.Peek().GotoCaseExpressions.Add(labelExpression);
                label = SyntaxFactory.Label(SyntaxKind.IdentifierLabel, MakeGotoSwitchLabel(labelExpression));
            } else {
                label = SyntaxFactory.Label(SyntaxKind.IdentifierLabel, _commonConversions.ConvertIdentifier(((CSS.IdentifierNameSyntax)node.Expression).Identifier));
            }
            return SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.GoToStatement(label));
        }

        /// <summary>
        /// VB doesn't have a block construct, but all of its other constructs (e.g. if statements) create a block.
        /// So we can use an always-true if statement to isolate the variables.
        /// </summary>
        public override SyntaxList<StatementSyntax> VisitBlock(CSS.BlockSyntax node)
        {
            var statements = ConvertBlock(node);
            var ifBlock = CreateBlock(statements);

            return SyntaxFactory.SingletonList<StatementSyntax>(ifBlock);
        }

        public static MultiLineIfBlockSyntax CreateBlock(SyntaxList<StatementSyntax> statements)
        {
            var ifStatement = SyntaxFactory.IfStatement(SyntaxFactory.Token(
                    SyntaxKind.IfKeyword),
                SyntaxFactory.TrueLiteralExpression(SyntaxFactory.Token(SyntaxKind.TrueKeyword)),
                SyntaxFactory.Token(SyntaxKind.ThenKeyword)
            );

            var ifBlock =
                SyntaxFactory.MultiLineIfBlock(ifStatement, statements, SyntaxFactory.List<ElseIfBlockSyntax>(), null);
            return ifBlock;
        }

        private SyntaxList<StatementSyntax> ConvertBlock(CSS.StatementSyntax node, params StatementSyntax[] prefixExtraVbStatements)
        {
            if (node is CSS.BlockSyntax b) {
                return SyntaxFactory.List(prefixExtraVbStatements.Concat(b.Statements.Where(s => !(s is CSS.EmptyStatementSyntax))
                    .SelectMany(s => s.Accept(CommentConvertingVisitor))));
            }
            if (node is CSS.EmptyStatementSyntax) {
                return SyntaxFactory.List(prefixExtraVbStatements);
            }
            return node.Accept(CommentConvertingVisitor);
        }

        public override SyntaxList<StatementSyntax> VisitReturnStatement(CSS.ReturnStatementSyntax node)
        {
            var vbExpression = node.Expression?.Accept(_nodesVisitor);
            return SyntaxFactory.SingletonList(ReturnStatement(vbExpression));
        }

        private static StatementSyntax ReturnStatement(VisualBasicSyntaxNode vbExpression)
        {
            return vbExpression == null
                ? SyntaxFactory.ReturnStatement()
                : vbExpression.IsKind(SyntaxKind.EmptyStatement)
                    ? SyntaxFactory.ReturnStatement()
                    : SyntaxFactory.ReturnStatement((ExpressionSyntax)vbExpression);
        }

        public override SyntaxList<StatementSyntax> VisitYieldStatement(CSS.YieldStatementSyntax node)
        {
            IsIterator = true;
            StatementSyntax stmt;
            if (node.Expression == null)
                stmt = SyntaxFactory.ReturnStatement();
            else
                stmt = SyntaxFactory.YieldStatement((ExpressionSyntax)node.Expression.Accept(_nodesVisitor));
            return SyntaxFactory.SingletonList(stmt);
        }

        public override SyntaxList<StatementSyntax> VisitThrowStatement(CSS.ThrowStatementSyntax node)
        {
            StatementSyntax stmt;
            if (node.Expression == null)
                stmt = SyntaxFactory.ThrowStatement();
            else
                stmt = SyntaxFactory.ThrowStatement((ExpressionSyntax)node.Expression.Accept(_nodesVisitor));
            return SyntaxFactory.SingletonList(stmt);
        }

        public override SyntaxList<StatementSyntax> VisitContinueStatement(CSS.ContinueStatementSyntax node)
        {
            var statementKind = SyntaxKind.None;
            var keywordKind = SyntaxKind.None;
            foreach (var stmt in node.GetAncestors<CSS.StatementSyntax>()) {
                if (stmt is CSS.DoStatementSyntax) {
                    statementKind = SyntaxKind.ContinueDoStatement;
                    keywordKind = SyntaxKind.DoKeyword;
                    break;
                }
                if (stmt is CSS.WhileStatementSyntax) {
                    statementKind = SyntaxKind.ContinueWhileStatement;
                    keywordKind = SyntaxKind.WhileKeyword;
                    break;
                }
                if (stmt is CSS.ForEachStatementSyntax) {
                    statementKind = SyntaxKind.ContinueForStatement;
                    keywordKind = SyntaxKind.ForKeyword;
                    break;
                }
                if (stmt is CSS.ForStatementSyntax fs) {
                    bool isFor = ConvertForToSimpleForNextWithoutStatements(fs, out _);
                    statementKind = isFor ? SyntaxKind.ContinueForStatement : SyntaxKind.ContinueWhileStatement;
                    keywordKind = isFor ? SyntaxKind.ForKeyword : SyntaxKind.WhileKeyword;
                    break;
                }
            }
            return SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.ContinueStatement(statementKind, SyntaxFactory.Token(keywordKind)));
        }

        public override SyntaxList<StatementSyntax> VisitBreakStatement(CSS.BreakStatementSyntax node)
        {
            var statementKind = SyntaxKind.None;
            var keywordKind = SyntaxKind.None;
            foreach (var stmt in node.GetAncestors<CSS.StatementSyntax>()) {
                if (stmt is CSS.DoStatementSyntax) {
                    statementKind = SyntaxKind.ExitDoStatement;
                    keywordKind = SyntaxKind.DoKeyword;
                    break;
                }
                if (stmt is CSS.WhileStatementSyntax) {
                    statementKind = SyntaxKind.ExitWhileStatement;
                    keywordKind = SyntaxKind.WhileKeyword;
                    break;
                }
                if (stmt is CSS.ForEachStatementSyntax) {
                    statementKind = SyntaxKind.ExitForStatement;
                    keywordKind = SyntaxKind.ForKeyword;
                    break;
                }
                if (stmt is CSS.ForStatementSyntax fs) {
                    bool isFor = ConvertForToSimpleForNextWithoutStatements(fs, out _);
                    statementKind = isFor ? SyntaxKind.ExitForStatement : SyntaxKind.ExitWhileStatement;
                    keywordKind = isFor ? SyntaxKind.ForKeyword : SyntaxKind.WhileKeyword;
                    break;
                }
                if (stmt is CSS.SwitchStatementSyntax) {
                    statementKind = SyntaxKind.ExitSelectStatement;
                    keywordKind = SyntaxKind.SelectKeyword;
                    break;
                }
            }
            return SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.ExitStatement(statementKind, SyntaxFactory.Token(keywordKind)));
        }
        public override SyntaxList<StatementSyntax> VisitEmptyStatement(CSS.EmptyStatementSyntax node)
        {
            return SyntaxFactory.List<StatementSyntax>();
        }

        public override SyntaxList<StatementSyntax> VisitCheckedStatement(CSS.CheckedStatementSyntax node)
        {
            return WrapInComment(ConvertBlock(node.Block), "Visual Basic does not support checked statements!");
        }

        private SyntaxList<StatementSyntax> WrapInComment(SyntaxList<StatementSyntax> nodes, string comment)
        {
            if (nodes.Count > 0) {
                nodes = nodes.Replace(nodes[0], nodes[0].WithPrependedLeadingTrivia(SyntaxFactory.CommentTrivia("BEGIN TODO : " + comment)));
                nodes = nodes.Replace(nodes.Last(), nodes.Last().WithAppendedTrailingTrivia(SyntaxFactory.CommentTrivia("END TODO : " + comment)));
            }

            return nodes;
        }
    }
}
