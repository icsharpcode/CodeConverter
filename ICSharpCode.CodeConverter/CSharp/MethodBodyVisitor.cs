using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using static ICSharpCode.CodeConverter.CSharp.SyntaxKindExtensions;

namespace ICSharpCode.CodeConverter.CSharp
{
    public partial class VisualBasicConverter
    {
        class MethodBodyVisitor : VBasic.VisualBasicSyntaxVisitor<SyntaxList<StatementSyntax>>
        {
            private readonly SemanticModel _semanticModel;
            private readonly VBasic.VisualBasicSyntaxVisitor<CSharpSyntaxNode> _nodesVisitor;
            private readonly Stack<string> _withBlockTempVariableNames;

            public bool IsIterator { get; set; }
            public VBasic.VisualBasicSyntaxVisitor<SyntaxList<StatementSyntax>> CommentConvertingVisitor { get; }

            private CommonConversions CommonConversions { get; }

            public MethodBodyVisitor(SemanticModel semanticModel, VBasic.VisualBasicSyntaxVisitor<CSharpSyntaxNode> nodesVisitor, Stack<string> withBlockTempVariableNames, TriviaConverter triviaConverter)
            {
                this._semanticModel = semanticModel;
                this._nodesVisitor = nodesVisitor;
                this._withBlockTempVariableNames = withBlockTempVariableNames;
                CommentConvertingVisitor = new CommentConvertingMethodBodyVisitor(this, triviaConverter);
                CommonConversions = new CommonConversions(semanticModel, _nodesVisitor);
            }

            public override SyntaxList<StatementSyntax> DefaultVisit(SyntaxNode node)
            {
                throw new NotImplementedException($"Conversion for {VBasic.VisualBasicExtensions.Kind(node)} not implemented, please report this issue")
                    .WithNodeInformation(node);
            }

            public override SyntaxList<StatementSyntax> VisitStopOrEndStatement(VBSyntax.StopOrEndStatementSyntax node)
            {
                return SingleStatement(SyntaxFactory.ParseStatement(ConvertStopOrEndToCSharpStatementText(node)));
            }

            private static string ConvertStopOrEndToCSharpStatementText(VBSyntax.StopOrEndStatementSyntax node)
            {
                switch (VBasic.VisualBasicExtensions.Kind(node.StopOrEndKeyword)) {
                    case VBasic.SyntaxKind.StopKeyword:
                        return "System.Diagnostics.Debugger.Break();";
                    case VBasic.SyntaxKind.EndKeyword:
                        return "System.Environment.Exit(0);";
                    default:
                        throw new NotImplementedException(node.StopOrEndKeyword.Kind() + " not implemented!");
                }
            }

            public override SyntaxList<StatementSyntax> VisitLocalDeclarationStatement(VBSyntax.LocalDeclarationStatementSyntax node)
            {
                var modifiers = CommonConversions.ConvertModifiers(node.Modifiers, TokenContext.Local);

                var declarations = new List<LocalDeclarationStatementSyntax>();

                foreach (var declarator in node.Declarators)
                    foreach (var decl in CommonConversions.SplitVariableDeclarations(declarator))
                        declarations.Add(SyntaxFactory.LocalDeclarationStatement(modifiers, decl.Value));

                return SyntaxFactory.List<StatementSyntax>(declarations);
            }

            public override SyntaxList<StatementSyntax> VisitAddRemoveHandlerStatement(VBSyntax.AddRemoveHandlerStatementSyntax node)
            {
                var syntaxKind = ConvertAddRemoveHandlerToCSharpSyntaxKind(node);
                return SingleStatement(SyntaxFactory.AssignmentExpression(syntaxKind,
                    (ExpressionSyntax)node.EventExpression.Accept(_nodesVisitor),
                    (ExpressionSyntax)node.DelegateExpression.Accept(_nodesVisitor)));
            }

            private static SyntaxKind ConvertAddRemoveHandlerToCSharpSyntaxKind(VBSyntax.AddRemoveHandlerStatementSyntax node)
            {
                switch (node.Kind()) {
                    case VBasic.SyntaxKind.AddHandlerStatement:
                        return SyntaxKind.AddAssignmentExpression;
                    case VBasic.SyntaxKind.RemoveHandlerStatement:
                        return SyntaxKind.SubtractAssignmentExpression;
                    default:
                        throw new NotImplementedException(node.Kind() + " not implemented!");
                }
            }

            public override SyntaxList<StatementSyntax> VisitExpressionStatement(VBSyntax.ExpressionStatementSyntax node)
            {
                return SingleStatement((ExpressionSyntax)node.Expression.Accept(_nodesVisitor));
            }

            public override SyntaxList<StatementSyntax> VisitAssignmentStatement(VBSyntax.AssignmentStatementSyntax node)
            {
                var lhs = (ExpressionSyntax)node.Left.Accept(_nodesVisitor);
                var rhs = (ExpressionSyntax)node.Right.Accept(_nodesVisitor);
                // e.g. VB DivideAssignmentExpression "/=" is always on doubles unless you use the "\=" IntegerDivideAssignmentExpression, so need to cast in C#
                // Need the unconverted type, since the whole point is that it gets converted to a double by the operator
                if (node.IsKind(VBasic.SyntaxKind.DivideAssignmentStatement) && !node.HasOperandOfUnconvertedType("System.Double", _semanticModel)) {
                    var doubleType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DoubleKeyword));
                    rhs = SyntaxFactory.CastExpression(doubleType, rhs);
                }

                if (node.IsKind(VBasic.SyntaxKind.ExponentiateAssignmentStatement)) {
                    rhs = SyntaxFactory.InvocationExpression(
                        SyntaxFactory.ParseExpression($"{nameof(Math)}.{nameof(Math.Pow)}"),
                        ExpressionSyntaxExtensions.CreateArgList(lhs, rhs));
                }
                var kind = node.Kind().ConvertToken(TokenContext.Local);
                return SingleStatement(SyntaxFactory.AssignmentExpression(kind, lhs, rhs));
            }

            public override SyntaxList<StatementSyntax> VisitEraseStatement(VBSyntax.EraseStatementSyntax node)
            {
                var eraseStatements = node.Expressions.Select<VBSyntax.ExpressionSyntax, StatementSyntax>(arrayExpression => {
                    var lhs = arrayExpression.Accept(_nodesVisitor);
                    var rhs = SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
                    var assignmentExpressionSyntax =
                        SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, (ExpressionSyntax)lhs,
                            rhs);
                    return SyntaxFactory.ExpressionStatement(assignmentExpressionSyntax);
                });
                return SyntaxFactory.List(eraseStatements);
            }

            public override SyntaxList<StatementSyntax> VisitReDimStatement(VBSyntax.ReDimStatementSyntax node)
            {
                return SyntaxFactory.List(node.Clauses.SelectMany(arrayExpression => arrayExpression.Accept(CommentConvertingVisitor)));
            }

            public override SyntaxList<StatementSyntax> VisitRedimClause(VBSyntax.RedimClauseSyntax node)
            {
                bool preserve = node.Parent is VBSyntax.ReDimStatementSyntax rdss && rdss.PreserveKeyword.IsKind(VBasic.SyntaxKind.PreserveKeyword);
                
                var csTargetArrayExpression = (ExpressionSyntax) node.Expression.Accept(_nodesVisitor);
                var convertedBounds = CommonConversions.ConvertArrayBounds(node.ArrayBounds).ToList();

                var newArrayAssignment = CreateNewArrayAssignment(node.Expression, csTargetArrayExpression, convertedBounds, node.SpanStart);
                if (!preserve) return SingleStatement(newArrayAssignment);
                
                var oldTargetName = GetUniqueVariableNameInScope(node, "old" + csTargetArrayExpression.ToString().ToPascalCase());
                var oldArrayAssignment = CreateLocalVariableDeclarationAndAssignment(oldTargetName, csTargetArrayExpression);

                var oldTargetExpression = SyntaxFactory.IdentifierName(oldTargetName);
                var arrayCopyIfNotNull = CreateConditionalArrayCopy(oldTargetExpression, csTargetArrayExpression, convertedBounds);

                return SyntaxFactory.List(new StatementSyntax[] {oldArrayAssignment, newArrayAssignment, arrayCopyIfNotNull});
            }

            /// <summary>
            /// Cut down version of Microsoft.VisualBasic.CompilerServices.Utils.CopyArray
            /// </summary>
            private IfStatementSyntax CreateConditionalArrayCopy(IdentifierNameSyntax sourceArrayExpression,
                ExpressionSyntax targetArrayExpression,
                List<ExpressionSyntax> convertedBounds)
            {
                var sourceLength = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, sourceArrayExpression, SyntaxFactory.IdentifierName("Length"));
                var arrayCopyStatement = convertedBounds.Count == 1 
                    ? CreateArrayCopyWithMinOfLengths(sourceArrayExpression, sourceLength, targetArrayExpression, convertedBounds.Single()) 
                    : CreateArrayCopy(sourceArrayExpression, sourceLength, targetArrayExpression, convertedBounds);

                var oldTargetNotEqualToNull = SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, sourceArrayExpression,
                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
                return SyntaxFactory.IfStatement(oldTargetNotEqualToNull, arrayCopyStatement);
            }

            /// <summary>
            /// Array copy for multiple array dimensions represented by <paramref name="convertedBounds"/>
            /// </summary>
            /// <remarks>
            /// Exception cases will sometimes silently succeed in the converted code, 
            ///  but existing VB code relying on the exception thrown from a multidimensional redim preserve on
            ///  different rank arrays is hopefully rare enough that it's worth saving a few lines of code
            /// </remarks>
            private StatementSyntax CreateArrayCopy(IdentifierNameSyntax sourceArrayExpression,
                MemberAccessExpressionSyntax sourceLength,
                ExpressionSyntax targetArrayExpression, ICollection convertedBounds)
            {
                var lastSourceLengthArgs = ExpressionSyntaxExtensions.CreateArgList(CommonConversions.Literal(convertedBounds.Count - 1));
                var sourceLastRankLength = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.ParseExpression($"{sourceArrayExpression.Identifier}.GetLength"), lastSourceLengthArgs);
                var targetLastRankLength =
                    SyntaxFactory.InvocationExpression(SyntaxFactory.ParseExpression($"{targetArrayExpression}.GetLength"),
                        lastSourceLengthArgs);
                var length = SyntaxFactory.InvocationExpression(SyntaxFactory.ParseExpression("Math.Min"), ExpressionSyntaxExtensions.CreateArgList(sourceLastRankLength, targetLastRankLength));

                var loopVariableName = GetUniqueVariableNameInScope(sourceArrayExpression, "i");
                var loopVariableIdentifier = SyntaxFactory.IdentifierName(loopVariableName);
                var sourceStartForThisIteration =
                    SyntaxFactory.BinaryExpression(SyntaxKind.MultiplyExpression, loopVariableIdentifier, sourceLastRankLength);
                var targetStartForThisIteration =
                    SyntaxFactory.BinaryExpression(SyntaxKind.MultiplyExpression, loopVariableIdentifier, targetLastRankLength);

                var arrayCopy = CreateArrayCopyWithStartingPoints(sourceArrayExpression, sourceStartForThisIteration, targetArrayExpression,
                    targetStartForThisIteration, length);

                var sourceArrayCount = SyntaxFactory.BinaryExpression(SyntaxKind.SubtractExpression,
                    SyntaxFactory.BinaryExpression(SyntaxKind.DivideExpression, sourceLength, sourceLastRankLength), CommonConversions.Literal(1));

                return CreateForZeroToValueLoop(loopVariableIdentifier, arrayCopy, sourceArrayCount);
            }

            private ForStatementSyntax CreateForZeroToValueLoop(SimpleNameSyntax loopVariableIdentifier, StatementSyntax loopStatement, ExpressionSyntax inclusiveLoopUpperBound)
            {
                var loopVariableAssignment = CreateVariableDeclarationAndAssignment(loopVariableIdentifier.Identifier.Text, CommonConversions.Literal(0));
                var lessThanSourceBounds = SyntaxFactory.BinaryExpression(SyntaxKind.LessThanOrEqualExpression,
                    loopVariableIdentifier, inclusiveLoopUpperBound);
                var incrementors = SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                    SyntaxFactory.PrefixUnaryExpression(SyntaxKind.PreIncrementExpression, loopVariableIdentifier));
                var forStatementSyntax = SyntaxFactory.ForStatement(loopVariableAssignment,
                    SyntaxFactory.SeparatedList<ExpressionSyntax>(),
                    lessThanSourceBounds, incrementors, loopStatement);
                return forStatementSyntax;
            }

            private static ExpressionStatementSyntax CreateArrayCopyWithMinOfLengths(
                IdentifierNameSyntax sourceExpression, ExpressionSyntax sourceLength,
                ExpressionSyntax targetExpression, ExpressionSyntax targetLength)
            {
                var minLength = SyntaxFactory.InvocationExpression(SyntaxFactory.ParseExpression("Math.Min"), ExpressionSyntaxExtensions.CreateArgList(targetLength, sourceLength));
                var copyArgList = ExpressionSyntaxExtensions.CreateArgList(sourceExpression, targetExpression, minLength);
                var arrayCopy = SyntaxFactory.InvocationExpression(SyntaxFactory.ParseExpression("Array.Copy"), copyArgList);
                return SyntaxFactory.ExpressionStatement(arrayCopy);
            }

            private static ExpressionStatementSyntax CreateArrayCopyWithStartingPoints(
                IdentifierNameSyntax sourceExpression, ExpressionSyntax sourceStart,
                ExpressionSyntax targetExpression, ExpressionSyntax targetStart, ExpressionSyntax length)
            {
                var copyArgList = ExpressionSyntaxExtensions.CreateArgList(sourceExpression, sourceStart, targetExpression, targetStart, length);
                var arrayCopy = SyntaxFactory.InvocationExpression(SyntaxFactory.ParseExpression("Array.Copy"), copyArgList);
                return SyntaxFactory.ExpressionStatement(arrayCopy);
            }

            private ExpressionStatementSyntax CreateNewArrayAssignment(VBSyntax.ExpressionSyntax vbArrayExpression,
                ExpressionSyntax csArrayExpression, List<ExpressionSyntax> convertedBounds,
                int nodeSpanStart)
            {
                var arrayRankSpecifierSyntax = SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SeparatedList(convertedBounds));
                var convertedType = (IArrayTypeSymbol) _semanticModel.GetTypeInfo(vbArrayExpression).ConvertedType;
                var typeSyntax = GetTypeSyntaxFromTypeSymbol(convertedType.ElementType, nodeSpanStart);
                var arrayCreation =
                    SyntaxFactory.ArrayCreationExpression(SyntaxFactory.ArrayType(typeSyntax,
                        SyntaxFactory.SingletonList(arrayRankSpecifierSyntax)));
                var assignmentExpressionSyntax =
                    SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, csArrayExpression, arrayCreation);
                var newArrayAssignment = SyntaxFactory.ExpressionStatement(assignmentExpressionSyntax);
                return newArrayAssignment;
            }

            private TypeSyntax GetTypeSyntaxFromTypeSymbol(ITypeSymbol convertedType, int nodeSpanStart)
            {
                var predefinedKeywordKind = convertedType.SpecialType.GetPredefinedKeywordKind();
                if (predefinedKeywordKind != SyntaxKind.None) return SyntaxFactory.PredefinedType(SyntaxFactory.Token(predefinedKeywordKind));
                return SyntaxFactory.ParseTypeName(convertedType.ToMinimalCSharpDisplayString(_semanticModel, nodeSpanStart));
            }

            public override SyntaxList<StatementSyntax> VisitThrowStatement(VBSyntax.ThrowStatementSyntax node)
            {
                return SingleStatement(SyntaxFactory.ThrowStatement((ExpressionSyntax)node.Expression?.Accept(_nodesVisitor)));
            }

            public override SyntaxList<StatementSyntax> VisitReturnStatement(VBSyntax.ReturnStatementSyntax node)
            {
                if (IsIterator)
                    return SingleStatement(SyntaxFactory.YieldStatement(SyntaxKind.YieldBreakStatement));
                return SingleStatement(SyntaxFactory.ReturnStatement((ExpressionSyntax)node.Expression?.Accept(_nodesVisitor)));
            }

            public override SyntaxList<StatementSyntax> VisitContinueStatement(VBSyntax.ContinueStatementSyntax node)
            {
                return SingleStatement(SyntaxFactory.ContinueStatement());
            }

            public override SyntaxList<StatementSyntax> VisitYieldStatement(VBSyntax.YieldStatementSyntax node)
            {
                return SingleStatement(SyntaxFactory.YieldStatement(SyntaxKind.YieldReturnStatement, (ExpressionSyntax)node.Expression?.Accept(_nodesVisitor)));
            }

            public override SyntaxList<StatementSyntax> VisitExitStatement(VBSyntax.ExitStatementSyntax node)
            {
                switch (VBasic.VisualBasicExtensions.Kind(node.BlockKeyword)) {
                    case VBasic.SyntaxKind.SubKeyword:
                        return SingleStatement(SyntaxFactory.ReturnStatement());
                    case VBasic.SyntaxKind.FunctionKeyword:
                        VBasic.VisualBasicSyntaxNode typeContainer = (VBasic.VisualBasicSyntaxNode)node.Ancestors().OfType<VBSyntax.LambdaExpressionSyntax>().FirstOrDefault()
                            ?? node.Ancestors().OfType<VBSyntax.MethodBlockSyntax>().FirstOrDefault();
                        var info = typeContainer.TypeSwitch(
                            (VBSyntax.LambdaExpressionSyntax e) => _semanticModel.GetTypeInfo(e).Type.GetReturnType(),
                            (VBSyntax.MethodBlockSyntax e) => {
                                var type = (TypeSyntax)e.SubOrFunctionStatement.AsClause?.Type.Accept(_nodesVisitor) ?? SyntaxFactory.ParseTypeName("object");
                                return _semanticModel.GetSymbolInfo(type).Symbol?.GetReturnType();
                            }
                        );
                        ExpressionSyntax expr;
                        if (info == null)
                            expr = null;
                        else if (info.IsReferenceType)
                            expr = SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
                        else if (info.CanBeReferencedByName)
                            expr = SyntaxFactory.DefaultExpression(SyntaxFactory.ParseTypeName(info.ToMinimalCSharpDisplayString(_semanticModel, node.SpanStart)));
                        else
                            throw new NotSupportedException();
                        return SingleStatement(SyntaxFactory.ReturnStatement(expr));
                    default:
                        return SingleStatement(SyntaxFactory.BreakStatement());
                }
            }

            public override SyntaxList<StatementSyntax> VisitRaiseEventStatement(VBSyntax.RaiseEventStatementSyntax node)
            {
                var argumentListSyntax = (ArgumentListSyntax)node.ArgumentList.Accept(_nodesVisitor);

                var symbolInfo = _semanticModel.GetSymbolInfo(node.Name).ExtractBestMatch() as IEventSymbol;
                if (symbolInfo?.RaiseMethod != null) {
                    return SingleStatement(SyntaxFactory.InvocationExpression(
                        SyntaxFactory.IdentifierName($"On{symbolInfo.Name}"),
                        argumentListSyntax));
                }

                var memberBindingExpressionSyntax = SyntaxFactory.MemberBindingExpression(SyntaxFactory.IdentifierName("Invoke"));
                var conditionalAccessExpressionSyntax = SyntaxFactory.ConditionalAccessExpression(
                    (NameSyntax)node.Name.Accept(_nodesVisitor),
                    SyntaxFactory.InvocationExpression(memberBindingExpressionSyntax, argumentListSyntax)
                );
                return SingleStatement(
                    conditionalAccessExpressionSyntax
                );
            }

            public override SyntaxList<StatementSyntax> VisitSingleLineIfStatement(VBSyntax.SingleLineIfStatementSyntax node)
            {
                var condition = (ExpressionSyntax)node.Condition.Accept(_nodesVisitor);
                var block = SyntaxFactory.Block(node.Statements.SelectMany(s => s.Accept(CommentConvertingVisitor)));
                ElseClauseSyntax elseClause = null;

                if (node.ElseClause != null) {
                    var elseBlock = SyntaxFactory.Block(node.ElseClause.Statements.SelectMany(s => s.Accept(CommentConvertingVisitor)));
                    elseClause = SyntaxFactory.ElseClause(elseBlock.UnpackNonNestedBlock());
                }
                return SingleStatement(SyntaxFactory.IfStatement(condition, block.UnpackNonNestedBlock(), elseClause));
            }

            public override SyntaxList<StatementSyntax> VisitMultiLineIfBlock(VBSyntax.MultiLineIfBlockSyntax node)
            {
                var condition = (ExpressionSyntax)node.IfStatement.Condition.Accept(_nodesVisitor);
                var block = SyntaxFactory.Block(node.Statements.SelectMany(s => s.Accept(CommentConvertingVisitor)));
                ElseClauseSyntax elseClause = null;

                if (node.ElseBlock != null) {
                    var elseBlock = SyntaxFactory.Block(node.ElseBlock.Statements.SelectMany(s => s.Accept(CommentConvertingVisitor)));
                    elseClause = SyntaxFactory.ElseClause(elseBlock.UnpackPossiblyNestedBlock());// so that you get a neat "else if" at the end
                }

                foreach (var elseIf in node.ElseIfBlocks.Reverse()) {
                    var elseBlock = SyntaxFactory.Block(elseIf.Statements.SelectMany(s => s.Accept(CommentConvertingVisitor)));
                    var ifStmt = SyntaxFactory.IfStatement((ExpressionSyntax)elseIf.ElseIfStatement.Condition.Accept(_nodesVisitor), elseBlock.UnpackNonNestedBlock(), elseClause);
                    elseClause = SyntaxFactory.ElseClause(ifStmt);
                }

                return SingleStatement(SyntaxFactory.IfStatement(condition, block.UnpackNonNestedBlock(), elseClause));
            }

            public override SyntaxList<StatementSyntax> VisitForBlock(VBSyntax.ForBlockSyntax node)
            {
                var stmt = node.ForStatement;
                ExpressionSyntax startValue = (ExpressionSyntax)stmt.FromValue.Accept(_nodesVisitor);
                VariableDeclarationSyntax declaration = null;
                ExpressionSyntax id;
                if (stmt.ControlVariable is VBSyntax.VariableDeclaratorSyntax) {
                    var v = (VBSyntax.VariableDeclaratorSyntax)stmt.ControlVariable;
                    declaration = CommonConversions.SplitVariableDeclarations(v).Values.Single();
                    declaration = declaration.WithVariables(SyntaxFactory.SingletonSeparatedList(declaration.Variables[0].WithInitializer(SyntaxFactory.EqualsValueClause(startValue))));
                    id = SyntaxFactory.IdentifierName(declaration.Variables[0].Identifier);
                } else {
                    id = (ExpressionSyntax)stmt.ControlVariable.Accept(_nodesVisitor);
                    var symbol = _semanticModel.GetSymbolInfo(stmt.ControlVariable).Symbol;
                    if (!_semanticModel.LookupSymbols(node.FullSpan.Start, name: symbol.Name).Any()) {
                        var variableDeclaratorSyntax = SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(symbol.Name), null,
                            SyntaxFactory.EqualsValueClause(startValue));
                        declaration = SyntaxFactory.VariableDeclaration(
                            SyntaxFactory.IdentifierName("var"),
                            SyntaxFactory.SingletonSeparatedList(variableDeclaratorSyntax));
                    } else {
                        startValue = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, id, startValue);
                    }
                }

                var step = (ExpressionSyntax)stmt.StepClause?.StepValue.Accept(_nodesVisitor);
                PrefixUnaryExpressionSyntax value = step.SkipParens() as PrefixUnaryExpressionSyntax;
                ExpressionSyntax condition;
                if (value == null) {
                    condition = SyntaxFactory.BinaryExpression(SyntaxKind.LessThanOrEqualExpression, id, (ExpressionSyntax)stmt.ToValue.Accept(_nodesVisitor));
                } else {
                    condition = SyntaxFactory.BinaryExpression(SyntaxKind.GreaterThanOrEqualExpression, id, (ExpressionSyntax)stmt.ToValue.Accept(_nodesVisitor));
                }
                if (step == null)
                    step = SyntaxFactory.PostfixUnaryExpression(SyntaxKind.PostIncrementExpression, id);
                else
                    step = SyntaxFactory.AssignmentExpression(SyntaxKind.AddAssignmentExpression, id, step);
                var block = SyntaxFactory.Block(node.Statements.SelectMany(s => s.Accept(CommentConvertingVisitor)));
                return SingleStatement(SyntaxFactory.ForStatement(
                    declaration,
                    declaration != null ? SyntaxFactory.SeparatedList<ExpressionSyntax>() : SyntaxFactory.SingletonSeparatedList(startValue),
                    condition,
                    SyntaxFactory.SingletonSeparatedList(step),
                    block.UnpackNonNestedBlock()));
            }

            public override SyntaxList<StatementSyntax> VisitForEachBlock(VBSyntax.ForEachBlockSyntax node)
            {
                var stmt = node.ForEachStatement;

                TypeSyntax type = null;
                SyntaxToken id;
                if (stmt.ControlVariable is VBSyntax.VariableDeclaratorSyntax) {
                    var v = (VBSyntax.VariableDeclaratorSyntax)stmt.ControlVariable;
                    var declaration = CommonConversions.SplitVariableDeclarations(v).Values.Single();
                    type = declaration.Type;
                    id = declaration.Variables[0].Identifier;
                } else {
                    var v = (IdentifierNameSyntax)stmt.ControlVariable.Accept(_nodesVisitor);
                    id = v.Identifier;
                    type = SyntaxFactory.ParseTypeName("var");
                }

                var block = SyntaxFactory.Block(node.Statements.SelectMany(s => s.Accept(CommentConvertingVisitor)));
                return SingleStatement(SyntaxFactory.ForEachStatement(
                        type,
                        id,
                        (ExpressionSyntax)stmt.Expression.Accept(_nodesVisitor),
                        block.UnpackNonNestedBlock()
                    ));
            }

            public override SyntaxList<StatementSyntax> VisitLabelStatement(VBSyntax.LabelStatementSyntax node)
            {
                return SingleStatement(SyntaxFactory.LabeledStatement(node.LabelToken.Text, SyntaxFactory.EmptyStatement()));
            }

            public override SyntaxList<StatementSyntax> VisitGoToStatement(VBSyntax.GoToStatementSyntax node)
            {
                return SingleStatement(SyntaxFactory.GotoStatement(SyntaxKind.GotoStatement,
                    SyntaxFactory.IdentifierName(node.Label.LabelToken.Text)));
            }

            public override SyntaxList<StatementSyntax> VisitSelectBlock(VBSyntax.SelectBlockSyntax node)
            {
                var expr = (ExpressionSyntax)node.SelectStatement.Expression.Accept(_nodesVisitor);
                var exprWithoutTrivia = expr.WithoutTrivia().WithoutAnnotations();
                var sections = new List<SwitchSectionSyntax>();
                foreach (var block in node.CaseBlocks) {
                    var labels = new List<SwitchLabelSyntax>();
                    foreach (var c in block.CaseStatement.Cases) {
                        if (c is VBSyntax.SimpleCaseClauseSyntax s) {
                            var expressionSyntax = (ExpressionSyntax)s.Value.Accept(_nodesVisitor);
                            SwitchLabelSyntax caseSwitchLabelSyntax = SyntaxFactory.CaseSwitchLabel(expressionSyntax);
                            if (!_semanticModel.GetConstantValue(s.Value).HasValue) {
                                caseSwitchLabelSyntax =
                                    WrapInCasePatternSwitchLabelSyntax(expressionSyntax);
                            }

                            labels.Add(caseSwitchLabelSyntax);
                        } else if (c is VBSyntax.ElseCaseClauseSyntax) {
                            labels.Add(SyntaxFactory.DefaultSwitchLabel());
                        } else if (c is VBSyntax.RelationalCaseClauseSyntax relational) {
                            var operatorKind = VBasic.VisualBasicExtensions.Kind(relational);
                            var cSharpSyntaxNode = SyntaxFactory.BinaryExpression(operatorKind.ConvertToken(TokenContext.Local), exprWithoutTrivia, (ExpressionSyntax) relational.Value.Accept(_nodesVisitor));
                            labels.Add(WrapInCasePatternSwitchLabelSyntax(cSharpSyntaxNode));
                        } else if (c is VBSyntax.RangeCaseClauseSyntax range) {
                            var lowerBoundCheck = SyntaxFactory.BinaryExpression(SyntaxKind.LessThanOrEqualExpression, (ExpressionSyntax) range.LowerBound.Accept(_nodesVisitor), exprWithoutTrivia);
                            var upperBoundCheck = SyntaxFactory.BinaryExpression(SyntaxKind.LessThanOrEqualExpression, exprWithoutTrivia, (ExpressionSyntax) range.UpperBound.Accept(_nodesVisitor));
                            var withinBounds = SyntaxFactory.BinaryExpression(SyntaxKind.LogicalAndExpression, lowerBoundCheck, upperBoundCheck);
                            labels.Add(WrapInCasePatternSwitchLabelSyntax(withinBounds));
                        } else throw new NotSupportedException(c.Kind().ToString());
                    }

                    var csBlockStatements = block.Statements.SelectMany(s => s.Accept(CommentConvertingVisitor)).ToList();
                    if (csBlockStatements.LastOrDefault()
                            ?.IsKind(SyntaxKind.ReturnStatement) != true) {
                        csBlockStatements.Add(SyntaxFactory.BreakStatement());
                    }
                    var list = SingleStatement(SyntaxFactory.Block(csBlockStatements));
                    sections.Add(SyntaxFactory.SwitchSection(SyntaxFactory.List(labels), list));
                }

                var switchStatementSyntax = SyntaxFactory.SwitchStatement(expr, SyntaxFactory.List(sections));
                return SingleStatement(switchStatementSyntax);
            }

            private static CasePatternSwitchLabelSyntax WrapInCasePatternSwitchLabelSyntax(ExpressionSyntax cSharpSyntaxNode)
            {
                var discardPatternMatch = SyntaxFactory.DeclarationPattern(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
                    SyntaxFactory.DiscardDesignation());
                var casePatternSwitchLabelSyntax = SyntaxFactory.CasePatternSwitchLabel(discardPatternMatch,
                    SyntaxFactory.WhenClause(cSharpSyntaxNode), SyntaxFactory.Token(SyntaxKind.ColonToken));
                return casePatternSwitchLabelSyntax;
            }

            public override SyntaxList<StatementSyntax> VisitWithBlock(VBSyntax.WithBlockSyntax node)
            {
                var withExpression = (ExpressionSyntax)node.WithStatement.Expression.Accept(_nodesVisitor);
                _withBlockTempVariableNames.Push(GetUniqueVariableNameInScope(node, "withBlock"));
                try {
                    var declaration = CreateLocalVariableDeclarationAndAssignment(_withBlockTempVariableNames.Peek(), withExpression);
                    var statements = node.Statements.SelectMany(s => s.Accept(CommentConvertingVisitor));

                    return SingleStatement(SyntaxFactory.Block(new[] { declaration }.Concat(statements).ToArray()));
                } finally {
                    _withBlockTempVariableNames.Pop();
                }
            }

            private LocalDeclarationStatementSyntax CreateLocalVariableDeclarationAndAssignment(string variableName, ExpressionSyntax initValue)
            {
                return SyntaxFactory.LocalDeclarationStatement(CreateVariableDeclarationAndAssignment(variableName, initValue));
            }

            private static VariableDeclarationSyntax CreateVariableDeclarationAndAssignment(string variableName,
                ExpressionSyntax initValue)
            {
                var variableDeclaratorSyntax = SyntaxFactory.VariableDeclarator(
                    SyntaxFactory.Identifier(variableName), null,
                    SyntaxFactory.EqualsValueClause(initValue));
                var variableDeclarationSyntax = SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(variableDeclaratorSyntax));
                return variableDeclarationSyntax;
            }

            private string GetUniqueVariableNameInScope(SyntaxNode node, string variableNameBase)
            {
                var reservedNames = _withBlockTempVariableNames.Concat(node.DescendantNodesAndSelf()
                    .SelectMany(syntaxNode => _semanticModel.LookupSymbols(syntaxNode.SpanStart).Select(s => s.Name)));
                return NameGenerator.EnsureUniqueness(variableNameBase, reservedNames, true);
            }

            public override SyntaxList<StatementSyntax> VisitTryBlock(VBSyntax.TryBlockSyntax node)
            {
                var block = SyntaxFactory.Block(node.Statements.SelectMany(s => s.Accept(CommentConvertingVisitor)));
                return SingleStatement(
                    SyntaxFactory.TryStatement(
                        block,
                        SyntaxFactory.List(node.CatchBlocks.Select(c => (CatchClauseSyntax)c.Accept(_nodesVisitor))),
                        (FinallyClauseSyntax)node.FinallyBlock?.Accept(_nodesVisitor)
                    )
                );
            }

            public override SyntaxList<StatementSyntax> VisitSyncLockBlock(VBSyntax.SyncLockBlockSyntax node)
            {
                return SingleStatement(SyntaxFactory.LockStatement(
                    (ExpressionSyntax)node.SyncLockStatement.Expression.Accept(_nodesVisitor),
                    SyntaxFactory.Block(node.Statements.SelectMany(s => s.Accept(CommentConvertingVisitor))).UnpackNonNestedBlock()
                ));
            }

            public override SyntaxList<StatementSyntax> VisitUsingBlock(VBSyntax.UsingBlockSyntax node)
            {
                var statementSyntax = SyntaxFactory.Block(node.Statements.SelectMany(s => s.Accept(CommentConvertingVisitor)));
                if (node.UsingStatement.Expression == null) {
                    StatementSyntax stmt = statementSyntax;
                    foreach (var v in node.UsingStatement.Variables.Reverse())
                        foreach (var declaration in CommonConversions.SplitVariableDeclarations(v).Values.Reverse())
                            stmt = SyntaxFactory.UsingStatement(declaration, null, stmt);
                    return SingleStatement(stmt);
                }

                var expr = (ExpressionSyntax)node.UsingStatement.Expression.Accept(_nodesVisitor);
                var unpackPossiblyNestedBlock = statementSyntax.UnpackPossiblyNestedBlock(); // Allow reduced indentation for multiple usings in a row
                return SingleStatement(SyntaxFactory.UsingStatement(null, expr, unpackPossiblyNestedBlock));
            }

            public override SyntaxList<StatementSyntax> VisitWhileBlock(VBSyntax.WhileBlockSyntax node)
            {
                return SingleStatement(SyntaxFactory.WhileStatement(
                    (ExpressionSyntax)node.WhileStatement.Condition.Accept(_nodesVisitor),
                    SyntaxFactory.Block(node.Statements.SelectMany(s => s.Accept(CommentConvertingVisitor))).UnpackNonNestedBlock()
                ));
            }

            public override SyntaxList<StatementSyntax> VisitDoLoopBlock(VBSyntax.DoLoopBlockSyntax node)
            {
                var statements = SyntaxFactory.Block(node.Statements.SelectMany(s => s.Accept(CommentConvertingVisitor))).UnpackNonNestedBlock();

                if (node.DoStatement.WhileOrUntilClause != null) {
                    var stmt = node.DoStatement.WhileOrUntilClause;
                    if (SyntaxTokenExtensions.IsKind(stmt.WhileOrUntilKeyword, VBasic.SyntaxKind.WhileKeyword))
                        return SingleStatement(SyntaxFactory.WhileStatement(
                            (ExpressionSyntax)stmt.Condition.Accept(_nodesVisitor),
                            statements
                        ));
                    return SingleStatement(SyntaxFactory.WhileStatement(
                        SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, (ExpressionSyntax)stmt.Condition.Accept(_nodesVisitor)),
                        statements
                    ));
                }
                
                var whileOrUntilStmt = node.LoopStatement.WhileOrUntilClause;
                ExpressionSyntax conditionExpression;
                bool isUntilStmt;
                if (whileOrUntilStmt != null) {
                    conditionExpression = (ExpressionSyntax)whileOrUntilStmt.Condition.Accept(_nodesVisitor);
                    isUntilStmt = SyntaxTokenExtensions.IsKind(whileOrUntilStmt.WhileOrUntilKeyword, VBasic.SyntaxKind.UntilKeyword);
                } else {
                    conditionExpression = SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);
                    isUntilStmt = false;
                }

                if (isUntilStmt) {
                    conditionExpression = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, conditionExpression);
                }

                return SingleStatement(SyntaxFactory.DoStatement(statements, conditionExpression));
            }

            public override SyntaxList<StatementSyntax> VisitCallStatement(VBSyntax.CallStatementSyntax node)
            {
                return SingleStatement((ExpressionSyntax) node.Invocation.Accept(_nodesVisitor));
            }

            SyntaxList<StatementSyntax> SingleStatement(StatementSyntax statement)
            {
                return SyntaxFactory.SingletonList(statement);
            }

            SyntaxList<StatementSyntax> SingleStatement(ExpressionSyntax expression)
            {
                return SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.ExpressionStatement(expression));
            }
        }
    }

    static class Extensions
    {
        /// <summary>
        /// Returns the single statement in a block if it has no nested statements.
        /// If it has nested statements, and the surrounding block was removed, it could be ambiguous, 
        /// e.g. if (...) { if (...) return null; } else return "";
        /// Unbundling the middle if statement would bind the else to it, rather than the outer if statement
        /// </summary>
        public static StatementSyntax UnpackNonNestedBlock(this BlockSyntax block)
        {
            return block.Statements.Count == 1 && !block.ContainsNestedStatements() ? block.Statements[0] : block;
        }

        /// <summary>
        /// Only use this over <see cref="UnpackNonNestedBlock"/> in special cases where it will display more neatly and where you're sure nested statements don't introduce ambiguity
        /// </summary>
        public static StatementSyntax UnpackPossiblyNestedBlock(this BlockSyntax block)
        {
            return block.Statements.Count == 1 ? block.Statements[0] : block;
        }

        private static bool ContainsNestedStatements(this BlockSyntax block)
        {
            return block.Statements.Any(HasDescendantCSharpStatement);
        }

        private static bool HasDescendantCSharpStatement(this StatementSyntax c)
        {
            return c.DescendantNodes().OfType<StatementSyntax>().Any();
        }
    }
}
