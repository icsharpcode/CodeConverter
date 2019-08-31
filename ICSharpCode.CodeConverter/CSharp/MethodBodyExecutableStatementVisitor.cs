using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using static ICSharpCode.CodeConverter.CSharp.SyntaxKindExtensions;

namespace ICSharpCode.CodeConverter.CSharp
{
    /// <summary>
    /// Executable statements - which includes executable blocks such as if statements
    /// Maintains state relevant to the called method-like object. A fresh one must be used for each method, and the same one must be reused for statements in the same method
    /// </summary>
    internal class MethodBodyExecutableStatementVisitor : VBasic.VisualBasicSyntaxVisitor<SyntaxList<StatementSyntax>>
    {
        private readonly VBasic.VisualBasicSyntaxNode _methodNode;
        private readonly SemanticModel _semanticModel;
        private readonly CommentConvertingVisitorWrapper<CSharpSyntaxNode> _expressionVisitor;
        private readonly Stack<ExpressionSyntax> _withBlockLhs;
        private readonly HashSet<string> _extraUsingDirectives;
        private readonly MethodsWithHandles _methodsWithHandles;
        private readonly HashSet<string> _generatedNames = new HashSet<string>();

        public bool IsIterator { get; set; }
        public IdentifierNameSyntax ReturnVariable { get; set; }
        public bool HasReturnVariable => ReturnVariable != null;
        public VBasic.VisualBasicSyntaxVisitor<SyntaxList<StatementSyntax>> CommentConvertingVisitor { get; }

        private CommonConversions CommonConversions { get; }

        public MethodBodyExecutableStatementVisitor(VBasic.VisualBasicSyntaxNode methodNode, SemanticModel semanticModel,
            CommentConvertingVisitorWrapper<CSharpSyntaxNode> expressionVisitor, CommonConversions commonConversions,
            Stack<ExpressionSyntax> withBlockLhs, HashSet<string> extraUsingDirectives,
            AdditionalLocals additionalLocals, MethodsWithHandles methodsWithHandles,
            TriviaConverter triviaConverter)
        {
            _methodNode = methodNode;
            _semanticModel = semanticModel;
            _expressionVisitor = expressionVisitor;
            CommonConversions = commonConversions;
            _withBlockLhs = withBlockLhs;
            _extraUsingDirectives = extraUsingDirectives;
            _methodsWithHandles = methodsWithHandles;
            var byRefParameterVisitor = new ByRefParameterVisitor(this, additionalLocals, semanticModel, _generatedNames);
            CommentConvertingVisitor = new CommentConvertingMethodBodyVisitor(byRefParameterVisitor, triviaConverter);
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

        private string ConvertStopOrEndToCSharpStatementText(VBSyntax.StopOrEndStatementSyntax node)
        {
            switch (VBasic.VisualBasicExtensions.Kind(node.StopOrEndKeyword)) {
                case VBasic.SyntaxKind.StopKeyword:
                    _extraUsingDirectives.Add("System.Diagnostics");
                    return "Debugger.Break();";
                case VBasic.SyntaxKind.EndKeyword:
                    _extraUsingDirectives.Add("System");
                    return "Environment.Exit(0);";
                default:
                    throw new NotImplementedException(node.StopOrEndKeyword.Kind() + " not implemented!");
            }
        }

        public override SyntaxList<StatementSyntax> VisitLocalDeclarationStatement(VBSyntax.LocalDeclarationStatementSyntax node)
        {
            var modifiers = CommonConversions.ConvertModifiers(node.Declarators[0].Names[0], node.Modifiers, TokenContext.Local);
            var isConst = modifiers.Any(a => a.Kind() == SyntaxKind.ConstKeyword);

            var declarations = new List<LocalDeclarationStatementSyntax>();

            foreach (var declarator in node.Declarators)
            foreach (var decl in CommonConversions.SplitVariableDeclarations(declarator, preferExplicitType: isConst))
                declarations.Add(SyntaxFactory.LocalDeclarationStatement(modifiers, decl.Value));

            return SyntaxFactory.List<StatementSyntax>(declarations);
        }

        public override SyntaxList<StatementSyntax> VisitAddRemoveHandlerStatement(VBSyntax.AddRemoveHandlerStatementSyntax node)
        {
            var syntaxKind = ConvertAddRemoveHandlerToCSharpSyntaxKind(node);
            return SingleStatement(SyntaxFactory.AssignmentExpression(syntaxKind,
                (ExpressionSyntax)node.EventExpression.Accept(_expressionVisitor),
                (ExpressionSyntax)node.DelegateExpression.Accept(_expressionVisitor)));
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
            if (node.Expression is VBSyntax.InvocationExpressionSyntax invoke && invoke.Expression is VBSyntax.MemberAccessExpressionSyntax access) {
                if (access.Expression is VBSyntax.MyBaseExpressionSyntax && access.Name.Identifier.ValueText.Equals("Finalize", StringComparison.OrdinalIgnoreCase)) {
                    return new SyntaxList<StatementSyntax>();
                } else if (CommonConversions.InMethodCalledInitializeComponent(node) &&
                           access.Name.Identifier.ValueText.Equals("ResumeLayout",
                               StringComparison.OrdinalIgnoreCase)) {
                    var eventSubscriptionStatements = _methodsWithHandles.GetPreResumeLayoutEventHandlers();
                    if (eventSubscriptionStatements.Any()) {
                        return SyntaxFactory.List(eventSubscriptionStatements.Concat(SingleStatement((ExpressionSyntax)node.Expression.Accept(_expressionVisitor))));
                    }
                }
            }

            return SingleStatement((ExpressionSyntax)node.Expression.Accept(_expressionVisitor));
        }

        public override SyntaxList<StatementSyntax> VisitAssignmentStatement(VBSyntax.AssignmentStatementSyntax node)
        {
            var lhs = (ExpressionSyntax)node.Left.Accept(_expressionVisitor);
            var lOperation = _semanticModel.GetOperation(node.Left);

            //Already dealt with by call to the same method in VisitInvocationExpression
            if (CommonConversions.GetParameterizedPropertyAccessMethod(lOperation, out var _) != null) return SingleStatement(lhs);
            var rhs = (ExpressionSyntax)node.Right.Accept(_expressionVisitor);

            if (node.Left is VBSyntax.IdentifierNameSyntax id &&
                _methodNode is VBSyntax.MethodBlockSyntax mb &&
                HasReturnVariable &&
                id.Identifier.ValueText.Equals(mb.SubOrFunctionStatement.Identifier.ValueText, StringComparison.OrdinalIgnoreCase)) {
                lhs = ReturnVariable;
            }

            if (node.IsKind(VBasic.SyntaxKind.ExponentiateAssignmentStatement)) {
                rhs = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.ParseExpression($"{nameof(Math)}.{nameof(Math.Pow)}"),
                    ExpressionSyntaxExtensions.CreateArgList(lhs, rhs));
            }
            var kind = node.Kind().ConvertToken(TokenContext.Local);

            rhs = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Right, rhs);
            var assignment = SyntaxFactory.AssignmentExpression(kind, lhs, rhs);

            var postAssignment = GetPostAssignmentStatements(node);
            return postAssignment.Insert(0, SyntaxFactory.ExpressionStatement(assignment));
        }

        private SyntaxList<StatementSyntax> GetPostAssignmentStatements(VBSyntax.AssignmentStatementSyntax node)
        {
            var potentialPropertySymbol = _semanticModel.GetSymbolInfo(node.Left).ExtractBestMatch();
            return _methodsWithHandles.GetPostAssignmentStatements(node, potentialPropertySymbol);
        }

        public override SyntaxList<StatementSyntax> VisitEraseStatement(VBSyntax.EraseStatementSyntax node)
        {
            var eraseStatements = node.Expressions.Select<VBSyntax.ExpressionSyntax, StatementSyntax>(arrayExpression => {
                var lhs = arrayExpression.Accept(_expressionVisitor);
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
            return SyntaxFactory.List(node.Clauses.SelectMany(arrayExpression => ConvertRedimClause(arrayExpression)));
        }

        /// <remarks>
        /// RedimClauseSyntax isn't an executable statement, therefore this isn't a "Visit" method.
        /// Since it returns multiple statements it's easiest for it to be here in the current architecture.
        /// </remarks>
        private SyntaxList<StatementSyntax> ConvertRedimClause(VBSyntax.RedimClauseSyntax node)
        {
            bool preserve = node.Parent is VBSyntax.ReDimStatementSyntax rdss && rdss.PreserveKeyword.IsKind(VBasic.SyntaxKind.PreserveKeyword);
                
            var csTargetArrayExpression = (ExpressionSyntax) node.Expression.Accept(_expressionVisitor);
            var convertedBounds = CommonConversions.ConvertArrayBounds(node.ArrayBounds).ToList();

            var newArrayAssignment = CreateNewArrayAssignment(node.Expression, csTargetArrayExpression, convertedBounds, node.SpanStart);
            if (!preserve) return SingleStatement(newArrayAssignment);
                
            var oldTargetName = GetUniqueVariableNameInScope(node, "old" + csTargetArrayExpression.ToString().ToPascalCase());
            var oldArrayAssignment = CreateLocalVariableDeclarationAndAssignment(oldTargetName, csTargetArrayExpression);

            var oldTargetExpression = SyntaxFactory.IdentifierName(oldTargetName);
            var arrayCopyIfNotNull = CreateConditionalArrayCopy(node, oldTargetExpression, csTargetArrayExpression, convertedBounds);

            return SyntaxFactory.List(new StatementSyntax[] {oldArrayAssignment, newArrayAssignment, arrayCopyIfNotNull});
        }

        /// <summary>
        /// Cut down version of Microsoft.VisualBasic.CompilerServices.Utils.CopyArray
        /// </summary>
        private IfStatementSyntax CreateConditionalArrayCopy(VBasic.VisualBasicSyntaxNode originalVbNode,
            IdentifierNameSyntax sourceArrayExpression,
            ExpressionSyntax targetArrayExpression,
            List<ExpressionSyntax> convertedBounds)
        {
            var sourceLength = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, sourceArrayExpression, SyntaxFactory.IdentifierName("Length"));
            var arrayCopyStatement = convertedBounds.Count == 1 
                ? CreateArrayCopyWithMinOfLengths(sourceArrayExpression, sourceLength, targetArrayExpression, convertedBounds.Single()) 
                : CreateArrayCopy(originalVbNode, sourceArrayExpression, sourceLength, targetArrayExpression, convertedBounds);

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
        private StatementSyntax CreateArrayCopy(VBasic.VisualBasicSyntaxNode originalVbNode,
            IdentifierNameSyntax sourceArrayExpression,
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

            var loopVariableName = GetUniqueVariableNameInScope(originalVbNode, "i");
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
            var loopVariableAssignment = CommonConversions.CreateVariableDeclarationAndAssignment(loopVariableIdentifier.Identifier.Text, CommonConversions.Literal(0));
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
            return SingleStatement(SyntaxFactory.ThrowStatement((ExpressionSyntax)node.Expression?.Accept(_expressionVisitor)));
        }

        public override SyntaxList<StatementSyntax> VisitReturnStatement(VBSyntax.ReturnStatementSyntax node)
        {
            if (IsIterator)
                return SingleStatement(SyntaxFactory.YieldStatement(SyntaxKind.YieldBreakStatement));

            return SingleStatement(SyntaxFactory.ReturnStatement((ExpressionSyntax)node.Expression?.Accept(_expressionVisitor)));
        }

        public override SyntaxList<StatementSyntax> VisitContinueStatement(VBSyntax.ContinueStatementSyntax node)
        {
            return SingleStatement(SyntaxFactory.ContinueStatement());
        }

        public override SyntaxList<StatementSyntax> VisitYieldStatement(VBSyntax.YieldStatementSyntax node)
        {
            return SingleStatement(SyntaxFactory.YieldStatement(SyntaxKind.YieldReturnStatement, (ExpressionSyntax)node.Expression?.Accept(_expressionVisitor)));
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
                            var type = (TypeSyntax)e.SubOrFunctionStatement.AsClause?.Type.Accept(_expressionVisitor) ?? SyntaxFactory.ParseTypeName("object");
                            return _semanticModel.GetSymbolInfo(type).Symbol?.GetReturnType();
                        }
                    );
                    ExpressionSyntax expr;
                    if (HasReturnVariable)
                        expr = ReturnVariable;
                    else if (info == null)
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
            var argumentListSyntax = (ArgumentListSyntax)node.ArgumentList.Accept(_expressionVisitor);

            var symbolInfo = _semanticModel.GetSymbolInfo(node.Name).ExtractBestMatch() as IEventSymbol;
            if (symbolInfo?.RaiseMethod != null) {
                return SingleStatement(SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName($"On{symbolInfo.Name}"),
                    argumentListSyntax));
            }

            var memberBindingExpressionSyntax = SyntaxFactory.MemberBindingExpression(SyntaxFactory.IdentifierName("Invoke"));
            var conditionalAccessExpressionSyntax = SyntaxFactory.ConditionalAccessExpression(
                (NameSyntax)node.Name.Accept(_expressionVisitor),
                SyntaxFactory.InvocationExpression(memberBindingExpressionSyntax, argumentListSyntax)
            );
            return SingleStatement(
                conditionalAccessExpressionSyntax
            );
        }

        public override SyntaxList<StatementSyntax> VisitSingleLineIfStatement(VBSyntax.SingleLineIfStatementSyntax node)
        {
            var condition = (ExpressionSyntax)node.Condition.Accept(_expressionVisitor);
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
            var condition = (ExpressionSyntax)node.IfStatement.Condition.Accept(_expressionVisitor);
            var block = SyntaxFactory.Block(node.Statements.SelectMany(s => s.Accept(CommentConvertingVisitor)));
            ElseClauseSyntax elseClause = null;

            if (node.ElseBlock != null) {
                var elseBlock = SyntaxFactory.Block(node.ElseBlock.Statements.SelectMany(s => s.Accept(CommentConvertingVisitor)));
                elseClause = SyntaxFactory.ElseClause(elseBlock.UnpackPossiblyNestedBlock());// so that you get a neat "else if" at the end
            }

            foreach (var elseIf in node.ElseIfBlocks.Reverse()) {
                var elseBlock = SyntaxFactory.Block(elseIf.Statements.SelectMany(s => s.Accept(CommentConvertingVisitor)));
                var ifStmt = SyntaxFactory.IfStatement((ExpressionSyntax)elseIf.ElseIfStatement.Condition.Accept(_expressionVisitor), elseBlock.UnpackNonNestedBlock(), elseClause);
                elseClause = SyntaxFactory.ElseClause(ifStmt);
            }

            return SingleStatement(SyntaxFactory.IfStatement(condition, block.UnpackNonNestedBlock(), elseClause));
        }

        public override SyntaxList<StatementSyntax> VisitForBlock(VBSyntax.ForBlockSyntax node)
        {
            var stmt = node.ForStatement;
            ExpressionSyntax startValue = (ExpressionSyntax)stmt.FromValue.Accept(_expressionVisitor);
            VariableDeclarationSyntax declaration = null;
            ExpressionSyntax id;
            var controlVarOp = _semanticModel.GetOperation(stmt.ControlVariable) as IVariableDeclaratorOperation;
            var controlVarType = controlVarOp?.Symbol.Type;
            var initializers = new List<ExpressionSyntax>();
            if (stmt.ControlVariable is VBSyntax.VariableDeclaratorSyntax) {
                var v = (VBSyntax.VariableDeclaratorSyntax)stmt.ControlVariable;
                declaration = CommonConversions.SplitVariableDeclarations(v).Values.Single();
                declaration = declaration.WithVariables(SyntaxFactory.SingletonSeparatedList(declaration.Variables[0].WithInitializer(SyntaxFactory.EqualsValueClause(startValue))));
                id = SyntaxFactory.IdentifierName(declaration.Variables[0].Identifier);
            } else {
                id = (ExpressionSyntax)stmt.ControlVariable.Accept(_expressionVisitor);
                var controlVarSymbol = controlVarOp?.Symbol;
                if (controlVarSymbol != null && controlVarSymbol.DeclaringSyntaxReferences.Any(r => r.Span.OverlapsWith(stmt.ControlVariable.Span))) {
                    declaration = CommonConversions.CreateVariableDeclarationAndAssignment(controlVarSymbol.Name, startValue, CommonConversions.GetTypeSyntax(controlVarType));
                } else {
                    startValue = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, id, startValue);
                    initializers.Add(startValue);
                }
            }

            var step = (ExpressionSyntax)stmt.StepClause?.StepValue.Accept(_expressionVisitor);
            PrefixUnaryExpressionSyntax value = step.SkipParens() as PrefixUnaryExpressionSyntax;
            ExpressionSyntax condition;

            // In Visual Basic, the To expression is only evaluated once, but in C# will be evaluated every loop.
            // If it could evaluate differently or has side effects, it must be extracted as a variable
            var preLoopStatements = new List<SyntaxNode>();
            var csToValue = (ExpressionSyntax)stmt.ToValue.Accept(_expressionVisitor);
            if (!_semanticModel.GetConstantValue(stmt.ToValue).HasValue) {
                var loopToVariableName = GetUniqueVariableNameInScope(node, "loopTo");
                var toValueType = _semanticModel.GetTypeInfo(stmt.ToValue).ConvertedType;
                var toVariableId = SyntaxFactory.IdentifierName(loopToVariableName);
                if (controlVarType?.Equals(toValueType) == true && declaration != null) {
                    var loopToAssignment = CommonConversions.CreateVariableDeclarator(loopToVariableName, csToValue);
                    declaration = declaration.AddVariables(loopToAssignment);
                } else {
                    var loopEndDeclaration = SyntaxFactory.LocalDeclarationStatement(
                        CommonConversions.CreateVariableDeclarationAndAssignment(loopToVariableName, csToValue));
                    // Does not do anything about porting newline trivia upwards to maintain spacing above the loop
                    preLoopStatements.Add(loopEndDeclaration);
                }

                csToValue = toVariableId;
            };
                
            if (value == null) {
                condition = SyntaxFactory.BinaryExpression(SyntaxKind.LessThanOrEqualExpression, id, csToValue);
            } else {
                condition = SyntaxFactory.BinaryExpression(SyntaxKind.GreaterThanOrEqualExpression, id, csToValue);
            }
            if (step == null)
                step = SyntaxFactory.PostfixUnaryExpression(SyntaxKind.PostIncrementExpression, id);
            else
                step = SyntaxFactory.AssignmentExpression(SyntaxKind.AddAssignmentExpression, id, step);
            var block = SyntaxFactory.Block(node.Statements.SelectMany(s => s.Accept(CommentConvertingVisitor)));
            var forStatementSyntax = SyntaxFactory.ForStatement(
                declaration,
                SyntaxFactory.SeparatedList(initializers),
                condition,
                SyntaxFactory.SingletonSeparatedList(step),
                block.UnpackNonNestedBlock());
            return SyntaxFactory.List(preLoopStatements.Concat(new[] {forStatementSyntax}));
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
                var v = (IdentifierNameSyntax)stmt.ControlVariable.Accept(_expressionVisitor);
                id = v.Identifier;
                type = SyntaxFactory.ParseTypeName("var");
            }

            var block = SyntaxFactory.Block(node.Statements.SelectMany(s => s.Accept(CommentConvertingVisitor)));
            return SingleStatement(SyntaxFactory.ForEachStatement(
                type,
                id,
                (ExpressionSyntax)stmt.Expression.Accept(_expressionVisitor),
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
            var expr = (ExpressionSyntax)node.SelectStatement.Expression.Accept(_expressionVisitor);
            var exprWithoutTrivia = expr.WithoutTrivia().WithoutAnnotations();
            var sections = new List<SwitchSectionSyntax>();
            foreach (var block in node.CaseBlocks) {
                var labels = new List<SwitchLabelSyntax>();
                foreach (var c in block.CaseStatement.Cases) {
                    if (c is VBSyntax.SimpleCaseClauseSyntax s) {
                        var originalExpressionSyntax = (ExpressionSyntax)s.Value.Accept(_expressionVisitor);
                        // CSharp requires an explicit cast from the base type (e.g. int) in most cases switching on an enum
                        var typeConversionKind = CommonConversions.TypeConversionAnalyzer.AnalyzeConversion(s.Value);
                        var expressionSyntax = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(s.Value, originalExpressionSyntax, typeConversionKind, true);
                        SwitchLabelSyntax caseSwitchLabelSyntax = SyntaxFactory.CaseSwitchLabel(expressionSyntax);
                        if (!_semanticModel.GetConstantValue(s.Value).HasValue || (typeConversionKind != TypeConversionAnalyzer.TypeConversionKind.NonDestructiveCast && typeConversionKind != TypeConversionAnalyzer.TypeConversionKind.Identity)) {
                            caseSwitchLabelSyntax =
                                WrapInCasePatternSwitchLabelSyntax(node, expressionSyntax);
                        }

                        labels.Add(caseSwitchLabelSyntax);
                    } else if (c is VBSyntax.ElseCaseClauseSyntax) {
                        labels.Add(SyntaxFactory.DefaultSwitchLabel());
                    } else if (c is VBSyntax.RelationalCaseClauseSyntax relational) {
                        var operatorKind = VBasic.VisualBasicExtensions.Kind(relational);
                        var cSharpSyntaxNode = SyntaxFactory.BinaryExpression(operatorKind.ConvertToken(TokenContext.Local), exprWithoutTrivia, (ExpressionSyntax) relational.Value.Accept(_expressionVisitor));
                        labels.Add(WrapInCasePatternSwitchLabelSyntax(node, cSharpSyntaxNode, treatAsBoolean: true));
                    } else if (c is VBSyntax.RangeCaseClauseSyntax range) {
                        var lowerBoundCheck = SyntaxFactory.BinaryExpression(SyntaxKind.LessThanOrEqualExpression, (ExpressionSyntax) range.LowerBound.Accept(_expressionVisitor), exprWithoutTrivia);
                        var upperBoundCheck = SyntaxFactory.BinaryExpression(SyntaxKind.LessThanOrEqualExpression, exprWithoutTrivia, (ExpressionSyntax) range.UpperBound.Accept(_expressionVisitor));
                        var withinBounds = SyntaxFactory.BinaryExpression(SyntaxKind.LogicalAndExpression, lowerBoundCheck, upperBoundCheck);
                        labels.Add(WrapInCasePatternSwitchLabelSyntax(node, withinBounds, treatAsBoolean: true));
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

            var switchStatementSyntax = ValidSyntaxFactory.SwitchStatement(expr, sections);
            return SingleStatement(switchStatementSyntax);
        }

        private CasePatternSwitchLabelSyntax WrapInCasePatternSwitchLabelSyntax(VBSyntax.SelectBlockSyntax node, ExpressionSyntax cSharpSyntaxNode, bool treatAsBoolean = false)
        {
            var typeInfo = _semanticModel.GetTypeInfo(node.SelectStatement.Expression);

            DeclarationPatternSyntax patternMatch;
            if (typeInfo.ConvertedType.SpecialType == SpecialType.System_Boolean || treatAsBoolean) {
                patternMatch = SyntaxFactory.DeclarationPattern(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
                    SyntaxFactory.DiscardDesignation());
            } else {
                var varName = CommonConversions.ConvertIdentifier(SyntaxFactory.Identifier(GetUniqueVariableNameInScope(node, "case"))).ValueText;
                patternMatch = SyntaxFactory.DeclarationPattern(
                    SyntaxFactory.ParseTypeName("var"), SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier(varName)));
                cSharpSyntaxNode = SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, SyntaxFactory.IdentifierName(varName), cSharpSyntaxNode);
            }

            var casePatternSwitchLabelSyntax = SyntaxFactory.CasePatternSwitchLabel(patternMatch,
                SyntaxFactory.WhenClause(cSharpSyntaxNode), SyntaxFactory.Token(SyntaxKind.ColonToken));
            return casePatternSwitchLabelSyntax;
        }

        public override SyntaxList<StatementSyntax> VisitWithBlock(VBSyntax.WithBlockSyntax node)
        {
            var withExpression = (ExpressionSyntax)node.WithStatement.Expression.Accept(_expressionVisitor);
            var generateVariableName = !_semanticModel.GetTypeInfo(node.WithStatement.Expression).Type.IsValueType;

            ExpressionSyntax lhsExpression;
            List<StatementSyntax> prefixDeclarations = new List<StatementSyntax>();
            if (generateVariableName) {
                string uniqueVariableNameInScope = GetUniqueVariableNameInScope(node, "withBlock");
                lhsExpression =
                    SyntaxFactory.IdentifierName(uniqueVariableNameInScope);
                prefixDeclarations.Add(
                    CreateLocalVariableDeclarationAndAssignment(uniqueVariableNameInScope, withExpression));
            } else {
                lhsExpression = withExpression;
            }

            _withBlockLhs.Push(lhsExpression);
            try {
                var statements = node.Statements.SelectMany(s => s.Accept(CommentConvertingVisitor));

                IEnumerable<StatementSyntax> statementSyntaxs = prefixDeclarations.Concat(statements).ToArray();
                return generateVariableName
                    ? SingleStatement(SyntaxFactory.Block(statementSyntaxs))
                    : SyntaxFactory.List(statementSyntaxs);
            } finally {
                _withBlockLhs.Pop();
            }
        }

        private LocalDeclarationStatementSyntax CreateLocalVariableDeclarationAndAssignment(string variableName, ExpressionSyntax initValue)
        {
            return SyntaxFactory.LocalDeclarationStatement(CommonConversions.CreateVariableDeclarationAndAssignment(variableName, initValue));
        }

        private string GetUniqueVariableNameInScope(VBasic.VisualBasicSyntaxNode node, string variableNameBase)
        {
            return NameGenerator.GetUniqueVariableNameInScope(_semanticModel, _generatedNames, node, variableNameBase);
        }

        public override SyntaxList<StatementSyntax> VisitTryBlock(VBSyntax.TryBlockSyntax node)
        {
            var block = SyntaxFactory.Block(node.Statements.SelectMany(s => s.Accept(CommentConvertingVisitor)));
            return SingleStatement(
                SyntaxFactory.TryStatement(
                    block,
                    SyntaxFactory.List(node.CatchBlocks.Select(c => (CatchClauseSyntax)c.Accept(_expressionVisitor))),
                    (FinallyClauseSyntax)node.FinallyBlock?.Accept(_expressionVisitor)
                )
            );
        }

        public override SyntaxList<StatementSyntax> VisitSyncLockBlock(VBSyntax.SyncLockBlockSyntax node)
        {
            return SingleStatement(SyntaxFactory.LockStatement(
                (ExpressionSyntax)node.SyncLockStatement.Expression.Accept(_expressionVisitor),
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

            var expr = (ExpressionSyntax)node.UsingStatement.Expression.Accept(_expressionVisitor);
            var unpackPossiblyNestedBlock = statementSyntax.UnpackPossiblyNestedBlock(); // Allow reduced indentation for multiple usings in a row
            return SingleStatement(SyntaxFactory.UsingStatement(null, expr, unpackPossiblyNestedBlock));
        }

        public override SyntaxList<StatementSyntax> VisitWhileBlock(VBSyntax.WhileBlockSyntax node)
        {
            return SingleStatement(SyntaxFactory.WhileStatement(
                (ExpressionSyntax)node.WhileStatement.Condition.Accept(_expressionVisitor),
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
                        (ExpressionSyntax)stmt.Condition.Accept(_expressionVisitor),
                        statements
                    ));
                return SingleStatement(SyntaxFactory.WhileStatement(
                    ((ExpressionSyntax)stmt.Condition.Accept(_expressionVisitor)).InvertCondition(),
                    statements
                ));
            }
                
            var whileOrUntilStmt = node.LoopStatement.WhileOrUntilClause;
            ExpressionSyntax conditionExpression;
            bool isUntilStmt;
            if (whileOrUntilStmt != null) {
                conditionExpression = (ExpressionSyntax)whileOrUntilStmt.Condition.Accept(_expressionVisitor);
                isUntilStmt = SyntaxTokenExtensions.IsKind(whileOrUntilStmt.WhileOrUntilKeyword, VBasic.SyntaxKind.UntilKeyword);
            } else {
                conditionExpression = SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);
                isUntilStmt = false;
            }

            if (isUntilStmt) {
                conditionExpression = conditionExpression.InvertCondition();
            }

            return SingleStatement(SyntaxFactory.DoStatement(statements, conditionExpression));
        }

        public override SyntaxList<StatementSyntax> VisitCallStatement(VBSyntax.CallStatementSyntax node)
        {
            return SingleStatement((ExpressionSyntax) node.Invocation.Accept(_expressionVisitor));
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
