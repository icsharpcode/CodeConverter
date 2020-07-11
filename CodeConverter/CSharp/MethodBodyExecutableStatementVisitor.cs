using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.CodeAnalysis.Text;
using ICSharpCode.CodeConverter.Util.FromRoslyn;

namespace ICSharpCode.CodeConverter.CSharp
{
    /// <summary>
    /// Executable statements - which includes executable blocks such as if statements
    /// Maintains state relevant to the called method-like object. A fresh one must be used for each method, and the same one must be reused for statements in the same method
    /// </summary>
    internal class MethodBodyExecutableStatementVisitor : VBasic.VisualBasicSyntaxVisitor<Task<SyntaxList<StatementSyntax>>>
    {
        private readonly VBasic.VisualBasicSyntaxNode _methodNode;
        private readonly SemanticModel _semanticModel;
        private readonly CommentConvertingVisitorWrapper _expressionVisitor;
        private readonly Stack<ExpressionSyntax> _withBlockLhs;
        private readonly HashSet<string> _extraUsingDirectives;
        private readonly MethodsWithHandles _methodsWithHandles;
        private readonly HashSet<string> _generatedNames = new HashSet<string>();
        private readonly HashSet<VBSyntax.StatementSyntax> _redundantSourceStatements = new HashSet<VBSyntax.StatementSyntax>();
        private INamedTypeSymbol _vbBooleanTypeSymbol;
        private readonly HashSet<ILocalSymbol> _localsToInlineInLoop;

        public bool IsIterator { get; set; }
        public IdentifierNameSyntax ReturnVariable { get; set; }
        public bool HasReturnVariable => ReturnVariable != null;
        public VBasic.VisualBasicSyntaxVisitor<Task<SyntaxList<StatementSyntax>>> CommentConvertingVisitor { get; }

        private CommonConversions CommonConversions { get; }

        public static async Task<MethodBodyExecutableStatementVisitor> CreateAsync(VBasic.VisualBasicSyntaxNode node, SemanticModel semanticModel, CommentConvertingVisitorWrapper triviaConvertingExpressionVisitor, CommonConversions commonConversions, Stack<ExpressionSyntax> withBlockLhs, HashSet<string> extraUsingDirectives, ITypeContext typeContext, bool isIterator, IdentifierNameSyntax csReturnVariable)
        {
            var solution = commonConversions.Document.Project.Solution;
            var declarationsToInlineInLoop = await solution.GetDescendantsToInlineInLoopAsync(semanticModel, node);
            return new MethodBodyExecutableStatementVisitor(node, semanticModel, triviaConvertingExpressionVisitor, commonConversions, withBlockLhs, extraUsingDirectives, typeContext, declarationsToInlineInLoop) {
                IsIterator = isIterator,
                ReturnVariable = csReturnVariable,
            };
        }

        private MethodBodyExecutableStatementVisitor(VBasic.VisualBasicSyntaxNode methodNode, SemanticModel semanticModel,
            CommentConvertingVisitorWrapper expressionVisitor, CommonConversions commonConversions,
            Stack<ExpressionSyntax> withBlockLhs, HashSet<string> extraUsingDirectives,
            ITypeContext typeContext, HashSet<ILocalSymbol> localsToInlineInLoop)
        {
            _methodNode = methodNode;
            _semanticModel = semanticModel;
            _expressionVisitor = expressionVisitor;
            CommonConversions = commonConversions;
            _withBlockLhs = withBlockLhs;
            _extraUsingDirectives = extraUsingDirectives;
            _methodsWithHandles = typeContext.MethodsWithHandles;
            var byRefParameterVisitor = new HoistedNodeStateVisitor(this, typeContext.HoistedState, semanticModel, _generatedNames);
            CommentConvertingVisitor = new CommentConvertingMethodBodyVisitor(byRefParameterVisitor);
            _vbBooleanTypeSymbol = _semanticModel.Compilation.GetTypeByMetadataName("System.Boolean");
            _localsToInlineInLoop = localsToInlineInLoop;
        }

        public override async Task<SyntaxList<StatementSyntax>> DefaultVisit(SyntaxNode node)
        {
            throw new NotImplementedException($"Conversion for {VBasic.VisualBasicExtensions.Kind(node)} not implemented, please report this issue")
                .WithNodeInformation(node);
        }

        public override async Task<SyntaxList<StatementSyntax>> VisitStopOrEndStatement(VBSyntax.StopOrEndStatementSyntax node)
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

        public override async Task<SyntaxList<StatementSyntax>> VisitLocalDeclarationStatement(VBSyntax.LocalDeclarationStatementSyntax node)
        {
            var modifiers = CommonConversions.ConvertModifiers(node.Declarators[0].Names[0], node.Modifiers, TokenContext.Local);
            var isConst = modifiers.Any(a => a.Kind() == SyntaxKind.ConstKeyword);

            var declarations = new List<StatementSyntax>();

            foreach (var declarator in node.Declarators) {
                var splitVariableDeclarations = await SplitVariableDeclarationsAsync(declarator, preferExplicitType: isConst);
                var localDeclarationStatementSyntaxs = splitVariableDeclarations.Variables.Select(declAndType => SyntaxFactory.LocalDeclarationStatement(modifiers, declAndType.Decl));
                declarations.AddRange(localDeclarationStatementSyntaxs);
                var localFunctions = splitVariableDeclarations.Methods.Cast<LocalFunctionStatementSyntax>();
                declarations.AddRange(localFunctions);
            }

            return SyntaxFactory.List(declarations);
        }

        public override async Task<SyntaxList<StatementSyntax>> VisitAddRemoveHandlerStatement(VBSyntax.AddRemoveHandlerStatementSyntax node)
        {
            var syntaxKind = ConvertAddRemoveHandlerToCSharpSyntaxKind(node);
            return SingleStatement(SyntaxFactory.AssignmentExpression(syntaxKind,
                (ExpressionSyntax) await node.EventExpression.AcceptAsync(_expressionVisitor),
                (ExpressionSyntax) await node.DelegateExpression.AcceptAsync(_expressionVisitor)));
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

        public override async Task<SyntaxList<StatementSyntax>> VisitExpressionStatement(VBSyntax.ExpressionStatementSyntax node)
        {
            if (node.Expression is VBSyntax.InvocationExpressionSyntax invoke && invoke.Expression is VBSyntax.MemberAccessExpressionSyntax access && access.Expression is VBSyntax.MyBaseExpressionSyntax && access.Name.Identifier.ValueText.Equals("Finalize", StringComparison.OrdinalIgnoreCase)) {
                return new SyntaxList<StatementSyntax>();
            }

            return SingleStatement((ExpressionSyntax) await node.Expression.AcceptAsync(_expressionVisitor));
        }

        public override async Task<SyntaxList<StatementSyntax>> VisitAssignmentStatement(VBSyntax.AssignmentStatementSyntax node)
        {
            if (node.IsKind(VBasic.SyntaxKind.MidAssignmentStatement) && node.Left is VBSyntax.MidExpressionSyntax mes) {
                return await ConvertMidAssignmentAsync(node, mes);
            }

            var lhs = (ExpressionSyntax) await node.Left.AcceptAsync(_expressionVisitor);
            var lOperation = _semanticModel.GetOperation(node.Left);

            //Already dealt with by call to the same method in VisitInvocationExpression
            var (parameterizedPropertyAccessMethod, _) = await CommonConversions.GetParameterizedPropertyAccessMethodAsync(lOperation);
            if (parameterizedPropertyAccessMethod != null) return SingleStatement(lhs);
            var rhs = (ExpressionSyntax) await node.Right.AcceptAsync(_expressionVisitor);

            if (node.Left is VBSyntax.IdentifierNameSyntax id &&
                _methodNode is VBSyntax.MethodBlockSyntax mb &&
                HasReturnVariable &&
                id.Identifier.ValueText.Equals(mb.SubOrFunctionStatement.Identifier.ValueText, StringComparison.OrdinalIgnoreCase)) {
                lhs = ReturnVariable;
            }

            if (node.IsKind(VBasic.SyntaxKind.ExponentiateAssignmentStatement)) {
                rhs = SyntaxFactory.InvocationExpression(
                    ValidSyntaxFactory.MemberAccess(nameof(Math), nameof(Math.Pow)),
                    ExpressionSyntaxExtensions.CreateArgList(lhs, rhs));
            }
            var kind = node.Kind().ConvertToken(TokenContext.Local);

            rhs = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Right, rhs);
            var assignment = SyntaxFactory.AssignmentExpression(kind, lhs, rhs);

            var postAssignment = GetPostAssignmentStatements(node);
            return postAssignment.Insert(0, SyntaxFactory.ExpressionStatement(assignment));
        }

        private async Task<SyntaxList<StatementSyntax>> ConvertMidAssignmentAsync(VBSyntax.AssignmentStatementSyntax node, VBSyntax.MidExpressionSyntax mes)
        {
            _extraUsingDirectives.Add("Microsoft.VisualBasic.CompilerServices");
            var midFunction = ValidSyntaxFactory.MemberAccess("StringType", "MidStmtStr");
            var midArgList = (ArgumentListSyntax)await mes.ArgumentList.AcceptAsync(_expressionVisitor);
            var (reusable, statements, _) = await GetExpressionWithoutSideEffectsAsync(node.Right, "midTmp");
            if (midArgList.Arguments.Count == 2) {
                var length = ValidSyntaxFactory.MemberAccess(reusable, "Length");
                midArgList = midArgList.AddArguments(SyntaxFactory.Argument(length));
            }
            midArgList = midArgList.AddArguments(SyntaxFactory.Argument(reusable));
            var invokeMid = SyntaxFactory.InvocationExpression(midFunction, midArgList);
            return statements.Add(SyntaxFactory.ExpressionStatement(invokeMid));
        }

        /// <remarks>
        /// <see cref="CommonConversions.ConvertIdentifier"/> ensures we convert the property access to a field access
        /// </remarks>
        private SyntaxList<StatementSyntax> GetPostAssignmentStatements(VBSyntax.AssignmentStatementSyntax node)
        {
            var potentialPropertySymbol = _semanticModel.GetSymbolInfo(node.Left).ExtractBestMatch<ISymbol>();
            return _methodsWithHandles.GetPostAssignmentStatements(node, potentialPropertySymbol);
        }

        public override async Task<SyntaxList<StatementSyntax>> VisitEraseStatement(VBSyntax.EraseStatementSyntax node)
        {
            var eraseStatements = await node.Expressions.SelectAsync<VBSyntax.ExpressionSyntax, StatementSyntax>(async arrayExpression => {
                var lhs = await arrayExpression.AcceptAsync(_expressionVisitor);
                var rhs = SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
                var assignmentExpressionSyntax =
                    SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, (ExpressionSyntax)lhs,
                        rhs);
                return SyntaxFactory.ExpressionStatement(assignmentExpressionSyntax);
            });
            return SyntaxFactory.List(eraseStatements);
        }

        public override async Task<SyntaxList<StatementSyntax>> VisitReDimStatement(VBSyntax.ReDimStatementSyntax node)
        {
            return SyntaxFactory.List(await node.Clauses.SelectManyAsync(async arrayExpression => (IEnumerable<StatementSyntax>) await ConvertRedimClauseAsync(arrayExpression)));
        }

        /// <remarks>
        /// RedimClauseSyntax isn't an executable statement, therefore this isn't a "Visit" method.
        /// Since it returns multiple statements it's easiest for it to be here in the current architecture.
        /// </remarks>
        private async Task<SyntaxList<StatementSyntax>> ConvertRedimClauseAsync(VBSyntax.RedimClauseSyntax node)
        {
            bool preserve = node.Parent is VBSyntax.ReDimStatementSyntax rdss && rdss.PreserveKeyword.IsKind(VBasic.SyntaxKind.PreserveKeyword);

            var csTargetArrayExpression = (ExpressionSyntax) await node.Expression.AcceptAsync(_expressionVisitor);
            var convertedBounds = (await CommonConversions.ConvertArrayBoundsAsync(node.ArrayBounds)).Sizes.ToList();
            if (preserve && convertedBounds.Count == 1) {
                var argumentList = new[] { csTargetArrayExpression, convertedBounds.Single() }.CreateCsArgList(SyntaxKind.RefKeyword);
                var arrayResize = SyntaxFactory.InvocationExpression(ValidSyntaxFactory.MemberAccess(nameof(Array), nameof(Array.Resize)), argumentList);
                return SingleStatement(arrayResize);
            }
            var newArrayAssignment = CreateNewArrayAssignment(node.Expression, csTargetArrayExpression, convertedBounds, node.SpanStart);
            if (!preserve) return SingleStatement(newArrayAssignment);

            var lastIdentifierText = node.Expression.DescendantNodesAndSelf().OfType<VBSyntax.IdentifierNameSyntax>().Last().Identifier.Text;
            var (oldTargetExpression, stmts, _) = await GetExpressionWithoutSideEffectsAsync(node.Expression, "old" + lastIdentifierText.ToPascalCase(), true);
            var arrayCopyIfNotNull = CreateConditionalArrayCopy(node, (IdentifierNameSyntax) oldTargetExpression, csTargetArrayExpression, convertedBounds);

            return stmts.AddRange(new StatementSyntax[] {newArrayAssignment, arrayCopyIfNotNull});
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

            var oldTargetNotEqualToNull = CommonConversions.NotNothingComparison(sourceArrayExpression, true);
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
            var convertedType = (IArrayTypeSymbol) _semanticModel.GetTypeInfo(vbArrayExpression).ConvertedType;
            var arrayRankSpecifierSyntax = SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SeparatedList(convertedBounds));
            var rankSpecifiers = SyntaxFactory.SingletonList(arrayRankSpecifierSyntax);
            while (convertedType.ElementType is IArrayTypeSymbol ats) {
                convertedType = ats;
                rankSpecifiers = rankSpecifiers.Add(SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(SyntaxFactory.OmittedArraySizeExpression())));
            };
            var typeSyntax = CommonConversions.GetTypeSyntax(convertedType.ElementType);
            var arrayCreation =
                SyntaxFactory.ArrayCreationExpression(SyntaxFactory.ArrayType(typeSyntax, rankSpecifiers));
            var assignmentExpressionSyntax =
                SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, csArrayExpression, arrayCreation);
            var newArrayAssignment = SyntaxFactory.ExpressionStatement(assignmentExpressionSyntax);
            return newArrayAssignment;
        }

        public override async Task<SyntaxList<StatementSyntax>> VisitThrowStatement(VBSyntax.ThrowStatementSyntax node)
        {
            return SingleStatement(SyntaxFactory.ThrowStatement((ExpressionSyntax) await node.Expression.AcceptAsync(_expressionVisitor)));
        }

        public override async Task<SyntaxList<StatementSyntax>> VisitReturnStatement(VBSyntax.ReturnStatementSyntax node)
        {
            if (IsIterator)
                return SingleStatement(SyntaxFactory.YieldStatement(SyntaxKind.YieldBreakStatement));

            var csExpression = (ExpressionSyntax)await node.Expression.AcceptAsync(_expressionVisitor);
            csExpression = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Expression, csExpression);
            return SingleStatement(SyntaxFactory.ReturnStatement(csExpression));
        }

        public override async Task<SyntaxList<StatementSyntax>> VisitContinueStatement(VBSyntax.ContinueStatementSyntax node)
        {
            return SingleStatement(SyntaxFactory.ContinueStatement());
        }

        public override async Task<SyntaxList<StatementSyntax>> VisitYieldStatement(VBSyntax.YieldStatementSyntax node)
        {
            return SingleStatement(SyntaxFactory.YieldStatement(SyntaxKind.YieldReturnStatement, (ExpressionSyntax) await node.Expression.AcceptAsync(_expressionVisitor)));
        }

        public override async Task<SyntaxList<StatementSyntax>> VisitExitStatement(VBSyntax.ExitStatementSyntax node)
        {
            switch (VBasic.VisualBasicExtensions.Kind(node.BlockKeyword)) {
                case VBasic.SyntaxKind.SubKeyword:
                case VBasic.SyntaxKind.PropertyKeyword when node.GetAncestor<VBSyntax.AccessorBlockSyntax>()?.IsKind(VBasic.SyntaxKind.GetAccessorBlock) != true:
                    return SingleStatement(SyntaxFactory.ReturnStatement());
                case VBasic.SyntaxKind.FunctionKeyword:
                case VBasic.SyntaxKind.PropertyKeyword when node.GetAncestor<VBSyntax.AccessorBlockSyntax>()?.IsKind(VBasic.SyntaxKind.GetAccessorBlock) == true:
                    VBasic.VisualBasicSyntaxNode typeContainer = node.GetAncestor<VBSyntax.LambdaExpressionSyntax>()
                                                                 ?? (VBasic.VisualBasicSyntaxNode)node.GetAncestor<VBSyntax.MethodBlockSyntax>()
                                                                 ?? node.GetAncestor<VBSyntax.AccessorBlockSyntax>();
                    var enclosingMethodInfo = await typeContainer.TypeSwitch(
                        async (VBSyntax.LambdaExpressionSyntax e) => _semanticModel.GetSymbolInfo(e).Symbol,
                        async (VBSyntax.MethodBlockSyntax e) => _semanticModel.GetDeclaredSymbol(e),
                        async (VBSyntax.AccessorBlockSyntax e) => _semanticModel.GetDeclaredSymbol(e)) as IMethodSymbol;

                    if (IsIterator) return SingleStatement(SyntaxFactory.YieldStatement(SyntaxKind.YieldBreakStatement));

                    if (!enclosingMethodInfo.ReturnsVoidOrAsyncTask()) {
                        ExpressionSyntax expr = HasReturnVariable ? (ExpressionSyntax)ReturnVariable : SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression);
                        return SingleStatement(SyntaxFactory.ReturnStatement(expr));
                    }

                    return SingleStatement(SyntaxFactory.ReturnStatement());
                default:
                    return SingleStatement(SyntaxFactory.BreakStatement());
            }
        }

        public override async Task<SyntaxList<StatementSyntax>> VisitRaiseEventStatement(VBSyntax.RaiseEventStatementSyntax node)
        {
            var argumentListSyntax = (ArgumentListSyntax) await node.ArgumentList.AcceptAsync(_expressionVisitor) ?? SyntaxFactory.ArgumentList();

            var symbolInfo = _semanticModel.GetSymbolInfo(node.Name).ExtractBestMatch<IEventSymbol>();
            if (symbolInfo?.RaiseMethod != null) {
                return SingleStatement(SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName($"On{symbolInfo.Name}"),
                    argumentListSyntax));
            }

            var memberBindingExpressionSyntax = SyntaxFactory.MemberBindingExpression(SyntaxFactory.IdentifierName("Invoke"));
            var conditionalAccessExpressionSyntax = SyntaxFactory.ConditionalAccessExpression(
                (NameSyntax) await node.Name.AcceptAsync(_expressionVisitor),
                SyntaxFactory.InvocationExpression(memberBindingExpressionSyntax, argumentListSyntax)
            );
            return SingleStatement(
                conditionalAccessExpressionSyntax
            );
        }

        public override async Task<SyntaxList<StatementSyntax>> VisitSingleLineIfStatement(VBSyntax.SingleLineIfStatementSyntax node)
        {
            var condition = (ExpressionSyntax) await node.Condition.AcceptAsync(_expressionVisitor);
            condition = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.Condition, condition, forceTargetType: _vbBooleanTypeSymbol);
            var block = SyntaxFactory.Block(await ConvertStatementsAsync(node.Statements));
            ElseClauseSyntax elseClause = null;

            if (node.ElseClause != null) {
                var elseBlock = SyntaxFactory.Block(await ConvertStatementsAsync(node.ElseClause.Statements));
                elseClause = SyntaxFactory.ElseClause(elseBlock.UnpackNonNestedBlock());
            }
            return SingleStatement(SyntaxFactory.IfStatement(condition, block.UnpackNonNestedBlock(), elseClause));
        }

        public override async Task<SyntaxList<StatementSyntax>> VisitMultiLineIfBlock(VBSyntax.MultiLineIfBlockSyntax node)
        {
            var condition = (ExpressionSyntax) await node.IfStatement.Condition.AcceptAsync(_expressionVisitor);
            condition = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(node.IfStatement.Condition, condition, forceTargetType: _vbBooleanTypeSymbol);
            var block = SyntaxFactory.Block(await ConvertStatementsAsync(node.Statements));

            var elseClause = await ConvertElseClauseAsync(node.ElseBlock);
            elseClause = elseClause.WithVbSourceMappingFrom(node.ElseBlock); //Special case where explicit mapping is needed since block becomes clause so cannot be easily visited

            var elseIfBlocks = await node.ElseIfBlocks.SelectAsync(async elseIf => await ConvertElseIfAsync(elseIf));
            foreach (var elseIf in elseIfBlocks.Reverse()) {
                var ifStmt = SyntaxFactory.IfStatement(elseIf.ElseIfCondition, elseIf.ElseBlock, elseClause);
                elseClause = SyntaxFactory.ElseClause(ifStmt);
            }

            return SingleStatement(SyntaxFactory.IfStatement(condition, block, elseClause));
        }

        private async Task<(ExpressionSyntax ElseIfCondition, BlockSyntax ElseBlock)> ConvertElseIfAsync(VBSyntax.ElseIfBlockSyntax elseIf)
        {
            var elseBlock = SyntaxFactory.Block(await ConvertStatementsAsync(elseIf.Statements));
            var elseIfCondition = (ExpressionSyntax)await elseIf.ElseIfStatement.Condition.AcceptAsync(_expressionVisitor);
            elseIfCondition = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(elseIf.ElseIfStatement.Condition, elseIfCondition, forceTargetType: _vbBooleanTypeSymbol);
            return (elseIfCondition, elseBlock);
        }

        private async Task<ElseClauseSyntax> ConvertElseClauseAsync(VBSyntax.ElseBlockSyntax elseBlock)
        {
            if (elseBlock == null) return null;

            var csStatements = await ConvertStatementsAsync(elseBlock.Statements);
            if (csStatements.TryUnpackSingleStatement(out var stmt) && stmt.IsKind(SyntaxKind.IfStatement)) {
                // so that you get a neat "else if" at the end
                return SyntaxFactory.ElseClause(stmt);
            } else {
                return SyntaxFactory.ElseClause(SyntaxFactory.Block(csStatements));
            }
        }

        private async Task<StatementSyntax[]> ConvertStatementsAsync(SyntaxList<VBSyntax.StatementSyntax> statementSyntaxs)
        {
            return await statementSyntaxs.SelectManyAsync(async s => (IEnumerable<StatementSyntax>)await s.Accept(CommentConvertingVisitor));
        }

        /// <summary>
        /// See https://docs.microsoft.com/en-us/dotnet/visual-basic/language-reference/statements/for-next-statement#BKMK_Counter
        /// </summary>
        public override async Task<SyntaxList<StatementSyntax>> VisitForBlock(VBSyntax.ForBlockSyntax node)
        {
            var stmt = node.ForStatement;
            var startValue = (ExpressionSyntax) await stmt.FromValue.AcceptAsync(_expressionVisitor);
            VariableDeclarationSyntax declaration = null;
            ExpressionSyntax id;
            var controlVarOp = _semanticModel.GetOperation(stmt.ControlVariable) as IVariableDeclaratorOperation;
            var controlVarSymbol = controlVarOp?.Symbol;
            var controlVarType = controlVarSymbol?.Type;
            var initializers = new List<ExpressionSyntax>();
            if (stmt.ControlVariable is VBSyntax.VariableDeclaratorSyntax) {
                var v = (VBSyntax.VariableDeclaratorSyntax)stmt.ControlVariable;
                declaration = (await SplitVariableDeclarationsAsync(v)).Variables.Single().Decl;
                declaration = declaration.WithVariables(SyntaxFactory.SingletonSeparatedList(declaration.Variables[0].WithInitializer(SyntaxFactory.EqualsValueClause(startValue))));
                id = SyntaxFactory.IdentifierName(declaration.Variables[0].Identifier);
            } else {
                id = (ExpressionSyntax) await stmt.ControlVariable.AcceptAsync(_expressionVisitor);

                // If missing semantic info, the compiler just guesses object. In this branch there was no explicit type, so let's try to improve on that guess:
                controlVarType = controlVarType.Yield()
                    .Concat(new[] { stmt.FromValue, stmt.ToValue, stmt.StepClause?.StepValue }.Select(exp => _semanticModel.GetTypeInfo(exp).Type))
                    .FirstOrDefault(t => t != null && t.SpecialType != SpecialType.System_Object);

                if (controlVarSymbol != null && controlVarSymbol.DeclaringSyntaxReferences.Any(r => r.Span.OverlapsWith(stmt.ControlVariable.Span))) {
                    declaration = CommonConversions.CreateVariableDeclarationAndAssignment(controlVarSymbol.Name, startValue, CommonConversions.GetTypeSyntax(controlVarType));
                } else {
                    startValue = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, id, startValue);
                    initializers.Add(startValue);
                }
            }

            // In Visual Basic, the To expression is only evaluated once, but in C# will be evaluated every loop.
            // If it could evaluate differently or has side effects, it must be extracted as a variable
            var preLoopStatements = new List<SyntaxNode>();
            var csToValue = (ExpressionSyntax) await stmt.ToValue.AcceptAsync(_expressionVisitor);
            if (!_semanticModel.GetConstantValue(stmt.ToValue).HasValue) {
                var loopToVariableName = GetUniqueVariableNameInScope(node, "loopTo");
                var toValueType = _semanticModel.GetTypeInfo(stmt.ToValue).ConvertedType;
                var toVariableId = SyntaxFactory.IdentifierName(loopToVariableName);
                if (controlVarType?.Equals(toValueType) == true && declaration != null) {
                    var loopToAssignment = CommonConversions.CreateVariableDeclarator(loopToVariableName, csToValue);
                    declaration = declaration.AddVariables(loopToAssignment);
                } else {
                    csToValue = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(stmt.ToValue, csToValue, forceTargetType: controlVarType);
                    var loopEndDeclaration = SyntaxFactory.LocalDeclarationStatement(
                        CommonConversions.CreateVariableDeclarationAndAssignment(loopToVariableName, csToValue));
                    // Does not do anything about porting newline trivia upwards to maintain spacing above the loop
                    preLoopStatements.Add(loopEndDeclaration);
                }

                csToValue = toVariableId;
            };

            var (csCondition, csStep) = await ConvertConditionAndStepClauseAsync(stmt, id, csToValue, controlVarType);

            var block = SyntaxFactory.Block(await ConvertStatementsAsync(node.Statements));
            var forStatementSyntax = SyntaxFactory.ForStatement(
                declaration,
                SyntaxFactory.SeparatedList(initializers),
                csCondition,
                SyntaxFactory.SingletonSeparatedList(csStep),
                block.UnpackNonNestedBlock());
            return SyntaxFactory.List(preLoopStatements.Concat(new[] { forStatementSyntax }));
        }

        private async Task<(IReadOnlyCollection<(VariableDeclarationSyntax Decl, ITypeSymbol Type)> Variables, IReadOnlyCollection<CSharpSyntaxNode> Methods)> SplitVariableDeclarationsAsync(VBSyntax.VariableDeclaratorSyntax v, bool preferExplicitType = false)
        {
            return await CommonConversions.SplitVariableDeclarationsAsync(v, _localsToInlineInLoop, preferExplicitType);
        }

        private async Task<(ExpressionSyntax, ExpressionSyntax)> ConvertConditionAndStepClauseAsync(VBSyntax.ForStatementSyntax stmt, ExpressionSyntax id, ExpressionSyntax csToValue, ITypeSymbol controlVarType)
        {
            var vbStepValue = stmt.StepClause?.StepValue;
            var csStepValue = (ExpressionSyntax)await (stmt.StepClause?.StepValue).AcceptAsync(_expressionVisitor);
            // For an enum, you need to add on an integer for example:
            var forceStepType = controlVarType is INamedTypeSymbol nt && nt.IsEnumType() ? nt.EnumUnderlyingType : controlVarType;
            csStepValue = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(vbStepValue, csStepValue?.SkipIntoParens(), forceTargetType: forceStepType);
            csToValue = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(stmt.ToValue, csToValue?.SkipIntoParens(), forceTargetType: controlVarType);

            var nonNegativeCondition = SyntaxFactory.BinaryExpression(SyntaxKind.LessThanOrEqualExpression, id, csToValue);
            var negativeCondition = SyntaxFactory.BinaryExpression(SyntaxKind.GreaterThanOrEqualExpression, id, csToValue);
            if (csStepValue == null) {
                return (nonNegativeCondition, SyntaxFactory.PostfixUnaryExpression(SyntaxKind.PostIncrementExpression, id));
            }

            ExpressionSyntax csCondition;
            ExpressionSyntax csStep = SyntaxFactory.AssignmentExpression(SyntaxKind.AddAssignmentExpression, id, csStepValue);
            var vbStepConstValue = _semanticModel.GetConstantValue(vbStepValue);
            var constValue = !vbStepConstValue.HasValue ? null : (dynamic)vbStepConstValue.Value;
            if (constValue == null) {
                var ifStepNonNegative = SyntaxFactory.BinaryExpression(SyntaxKind.GreaterThanOrEqualExpression, csStepValue, CommonConversions.Literal(0));
                csCondition = SyntaxFactory.ConditionalExpression(ifStepNonNegative, nonNegativeCondition, negativeCondition);
            } else if (constValue < 0) {
                csCondition = negativeCondition;
                if (csStepValue is PrefixUnaryExpressionSyntax pues && pues.OperatorToken.IsKind(SyntaxKind.MinusToken)) {
                    csStep = SyntaxFactory.AssignmentExpression(SyntaxKind.SubtractAssignmentExpression, id, pues.Operand);
                }
            } else {
                csCondition = nonNegativeCondition;
            }

            return (csCondition, csStep);
        }

        public override async Task<SyntaxList<StatementSyntax>> VisitForEachBlock(VBSyntax.ForEachBlockSyntax node)
        {
            var stmt = node.ForEachStatement;

            TypeSyntax type;
            SyntaxToken id;
            List<StatementSyntax> statements = new List<StatementSyntax>();
            if (stmt.ControlVariable is VBSyntax.VariableDeclaratorSyntax vds) {
                var declaration = (await SplitVariableDeclarationsAsync(vds)).Variables.Single().Decl;
                type = declaration.Type;
                id = declaration.Variables.Single().Identifier;
            } else if (_semanticModel.GetSymbolInfo(stmt.ControlVariable).Symbol is { } varSymbol) {
                var variableType = varSymbol.GetSymbolType();
                var explicitCastWouldHaveNoEffect = variableType?.SpecialType == SpecialType.System_Object || _semanticModel.GetTypeInfo(stmt.Expression).ConvertedType.IsEnumerableOfExactType(variableType);
                type = CommonConversions.GetTypeSyntax(varSymbol.GetSymbolType(), explicitCastWouldHaveNoEffect);
                var v = (IdentifierNameSyntax)await stmt.ControlVariable.AcceptAsync(_expressionVisitor);
                if (_localsToInlineInLoop.Contains(varSymbol)) {
                    id = v.Identifier;
                } else {
                    id = CommonConversions.CsEscapedIdentifier(GetUniqueVariableNameInScope(node, "current" + varSymbol.Name.ToPascalCase()));
                    statements.Add(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, v, SyntaxFactory.IdentifierName(id))));
                }
            } else {
                var v = (IdentifierNameSyntax) await stmt.ControlVariable.AcceptAsync(_expressionVisitor);
                id = v.Identifier;
                type = ValidSyntaxFactory.VarType;
            }

            var block = SyntaxFactory.Block(statements.Concat(await ConvertStatementsAsync(node.Statements)));
            var csExpression = (ExpressionSyntax)await stmt.Expression.AcceptAsync(_expressionVisitor);
            return SingleStatement(SyntaxFactory.ForEachStatement(
                type,
                id,
                CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(stmt.Expression, csExpression),
                block.UnpackNonNestedBlock()
            ));
        }

        public override async Task<SyntaxList<StatementSyntax>> VisitLabelStatement(VBSyntax.LabelStatementSyntax node)
        {
            return SingleStatement(SyntaxFactory.LabeledStatement(node.LabelToken.Text, SyntaxFactory.EmptyStatement()));
        }

        public override async Task<SyntaxList<StatementSyntax>> VisitGoToStatement(VBSyntax.GoToStatementSyntax node)
        {
            return SingleStatement(SyntaxFactory.GotoStatement(SyntaxKind.GotoStatement,
                SyntaxFactory.IdentifierName(node.Label.LabelToken.Text)));
        }

        public override async Task<SyntaxList<StatementSyntax>> VisitSelectBlock(VBSyntax.SelectBlockSyntax node)
        {
            var vbExpr = node.SelectStatement.Expression;
            var vbEquality = CommonConversions.VisualBasicEqualityComparison;

            var csSwitchExpr = (ExpressionSyntax)await vbExpr.AcceptAsync(_expressionVisitor);
            csSwitchExpr = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(vbExpr, csSwitchExpr);
            var switchExprTypeInfo = _semanticModel.GetTypeInfo(vbExpr);
            var isStringComparison = switchExprTypeInfo.ConvertedType?.SpecialType == SpecialType.System_String || switchExprTypeInfo.ConvertedType?.IsArrayOf(SpecialType.System_Char) == true;
            var caseInsensitiveStringComparison = vbEquality.OptionCompareTextCaseInsensitive &&
                isStringComparison;
            if (isStringComparison) {
                csSwitchExpr = vbEquality.VbCoerceToNonNullString(vbExpr, csSwitchExpr, switchExprTypeInfo);
            }

            var usedConstantValues = new HashSet<object>();
            var sections = new List<SwitchSectionSyntax>();
            foreach (var block in node.CaseBlocks) {
                var labels = new List<SwitchLabelSyntax>();
                foreach (var c in block.CaseStatement.Cases) {
                    if (c is VBSyntax.SimpleCaseClauseSyntax s) {
                        var originalExpressionSyntax = (ExpressionSyntax)await s.Value.AcceptAsync(_expressionVisitor);
                        var caseTypeInfo = _semanticModel.GetTypeInfo(s.Value);
                        var typeConversionKind = CommonConversions.TypeConversionAnalyzer.AnalyzeConversion(s.Value);
                        var correctTypeExpressionSyntax = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(s.Value, originalExpressionSyntax, typeConversionKind, true, true);
                        var constantValue = _semanticModel.GetConstantValue(s.Value);
                        var notAlreadyUsed = !constantValue.HasValue || usedConstantValues.Add(constantValue.Value);

                        // Pass both halves in case we can optimize away the check based on the switch expr
                        var wrapForStringComparison = isStringComparison && (caseInsensitiveStringComparison ||
                            vbEquality.VbCoerceToNonNullString(vbExpr, csSwitchExpr, switchExprTypeInfo, true, s.Value, originalExpressionSyntax, caseTypeInfo, false).rhs != originalExpressionSyntax);

                        // CSharp requires an explicit cast from the base type (e.g. int) in most cases switching on an enum
                        var csExpressionToUse = switchExprTypeInfo.ConvertedType?.IsEnumType() == true ^ caseTypeInfo.Type?.IsEnumType() == true ? correctTypeExpressionSyntax.Expr : originalExpressionSyntax;

                        var caseSwitchLabelSyntax = !wrapForStringComparison && correctTypeExpressionSyntax.IsConst && notAlreadyUsed
                            ? (SwitchLabelSyntax)SyntaxFactory.CaseSwitchLabel(csExpressionToUse)
                            : WrapInCasePatternSwitchLabelSyntax(node, s.Value, csExpressionToUse);
                        labels.Add(caseSwitchLabelSyntax);
                    } else if (c is VBSyntax.ElseCaseClauseSyntax) {
                        labels.Add(SyntaxFactory.DefaultSwitchLabel());
                    } else if (c is VBSyntax.RelationalCaseClauseSyntax relational) {

                        var varName = CommonConversions.CsEscapedIdentifier(GetUniqueVariableNameInScope(node, "case"));
                        ExpressionSyntax csLeft = SyntaxFactory.IdentifierName(varName);
                        var operatorKind = VBasic.VisualBasicExtensions.Kind(relational);
                        var relationalValue = (ExpressionSyntax)await relational.Value.AcceptAsync(_expressionVisitor);
                        var binaryExp = SyntaxFactory.BinaryExpression(operatorKind.ConvertToken(TokenContext.Local), csLeft, relationalValue);
                        labels.Add(VarWhen(varName, binaryExp));
                    } else if (c is VBSyntax.RangeCaseClauseSyntax range) {
                        var varName = CommonConversions.CsEscapedIdentifier(GetUniqueVariableNameInScope(node, "case"));
                        ExpressionSyntax csLeft = SyntaxFactory.IdentifierName(varName);
                        var lowerBoundCheck = SyntaxFactory.BinaryExpression(SyntaxKind.LessThanOrEqualExpression, (ExpressionSyntax)await range.LowerBound.AcceptAsync(_expressionVisitor), csLeft);
                        var upperBoundCheck = SyntaxFactory.BinaryExpression(SyntaxKind.LessThanOrEqualExpression, csLeft, (ExpressionSyntax)await range.UpperBound.AcceptAsync(_expressionVisitor));
                        var withinBounds = SyntaxFactory.BinaryExpression(SyntaxKind.LogicalAndExpression, lowerBoundCheck, upperBoundCheck);
                        labels.Add(VarWhen(varName, withinBounds));
                    } else throw new NotSupportedException(c.Kind().ToString());
                }

                var csBlockStatements = (await ConvertStatementsAsync(block.Statements)).ToList();
                if (csBlockStatements.LastOrDefault()
                        ?.IsKind(SyntaxKind.ReturnStatement, SyntaxKind.BreakStatement) != true) {
                    csBlockStatements.Add(SyntaxFactory.BreakStatement());
                }
                var list = SingleStatement(SyntaxFactory.Block(csBlockStatements));
                sections.Add(SyntaxFactory.SwitchSection(SyntaxFactory.List(labels), list));
            }

            var switchStatementSyntax = ValidSyntaxFactory.SwitchStatement(csSwitchExpr, sections);
            return SingleStatement(switchStatementSyntax);
        }

        private static CasePatternSwitchLabelSyntax VarWhen(SyntaxToken varName, ExpressionSyntax binaryExp)
        {
            var patternMatch = ValidSyntaxFactory.VarPattern(varName);
            return SyntaxFactory.CasePatternSwitchLabel(patternMatch,
                SyntaxFactory.WhenClause(binaryExp), SyntaxFactory.Token(SyntaxKind.ColonToken));
        }

        private async Task<(ExpressionSyntax Reusable, SyntaxList<StatementSyntax> Statements, ExpressionSyntax SingleUse)> GetExpressionWithoutSideEffectsAsync(VBSyntax.ExpressionSyntax vbExpr, string variableNameBase, bool forceVariable = false)
        {
            var expr = (ExpressionSyntax)await vbExpr.AcceptAsync(_expressionVisitor);
            expr = CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(vbExpr, expr);
            SyntaxList<StatementSyntax> stmts = SyntaxFactory.List<StatementSyntax>();
            ExpressionSyntax exprWithoutSideEffects;
            ExpressionSyntax reusableExprWithoutSideEffects;
            if (forceVariable || !await CanEvaluateMultipleTimesAsync(vbExpr)) {
                var contextNode = vbExpr.GetAncestor<VBSyntax.MethodBlockBaseSyntax>() ?? (VBasic.VisualBasicSyntaxNode) vbExpr.Parent;
                var varName = GetUniqueVariableNameInScope(contextNode, variableNameBase);
                var stmt = CommonConversions.CreateLocalVariableDeclarationAndAssignment(varName, expr);
                stmts = stmts.Add(stmt);
                exprWithoutSideEffects = SyntaxFactory.IdentifierName(varName);
                reusableExprWithoutSideEffects = exprWithoutSideEffects;
            } else {
                exprWithoutSideEffects = expr;
                reusableExprWithoutSideEffects = expr.WithoutSourceMapping();
            }

            return (reusableExprWithoutSideEffects, stmts, exprWithoutSideEffects);
        }

        private async Task<bool> CanEvaluateMultipleTimesAsync(VBSyntax.ExpressionSyntax vbExpr)
        {
            return _semanticModel.GetConstantValue(vbExpr).HasValue || vbExpr.SkipIntoParens() is VBSyntax.NameSyntax ns && await IsNeverMutatedAsync(ns);
        }

        private async Task<bool> IsNeverMutatedAsync(VBSyntax.NameSyntax ns)
        {
            var allowedLocation = Location.Create(ns.SyntaxTree, TextSpan.FromBounds(ns.GetAncestor<VBSyntax.MethodBlockBaseSyntax>().SpanStart, ns.Span.End));
            var symbol = _semanticModel.GetSymbolInfo(ns).Symbol;
            //Perf optimization: Looking across the whole solution is expensive, so assume non-local symbols are written somewhere
            return symbol.MatchesKind(SymbolKind.Parameter, SymbolKind.Local) && await CommonConversions.Document.Project.Solution.IsNeverWrittenAsync(symbol, allowedLocation);
        }

        private CasePatternSwitchLabelSyntax WrapInCasePatternSwitchLabelSyntax(VBSyntax.SelectBlockSyntax node, VBSyntax.ExpressionSyntax vbCase, ExpressionSyntax expression, bool treatAsBoolean = false)
        {
            var typeInfo = _semanticModel.GetTypeInfo(node.SelectStatement.Expression);

            DeclarationPatternSyntax patternMatch;
            if (typeInfo.ConvertedType.SpecialType == SpecialType.System_Boolean || treatAsBoolean) {
                patternMatch = SyntaxFactory.DeclarationPattern(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
                    SyntaxFactory.DiscardDesignation());
            } else {
                var varName = CommonConversions.CsEscapedIdentifier(GetUniqueVariableNameInScope(node, "case"));
                patternMatch = ValidSyntaxFactory.VarPattern(varName);
                ExpressionSyntax csLeft = SyntaxFactory.IdentifierName(varName), csRight = expression;
                var caseTypeInfo = _semanticModel.GetTypeInfo(vbCase);
                expression = EqualsAdjustedForStringComparison(node, vbCase, typeInfo, csLeft, csRight, caseTypeInfo);
            }

            var colonToken = SyntaxFactory.Token(SyntaxKind.ColonToken);
            return SyntaxFactory.CasePatternSwitchLabel(patternMatch, SyntaxFactory.WhenClause(expression), colonToken);
        }

        private ExpressionSyntax EqualsAdjustedForStringComparison(VBSyntax.SelectBlockSyntax node, VBSyntax.ExpressionSyntax vbCase, TypeInfo lhsTypeInfo, ExpressionSyntax csLeft, ExpressionSyntax csRight, TypeInfo rhsTypeInfo)
        {
            var vbEquality = CommonConversions.VisualBasicEqualityComparison;
            switch (vbEquality.GetObjectEqualityType(lhsTypeInfo, rhsTypeInfo)) {
                case VisualBasicEqualityComparison.RequiredType.Object:
                    return vbEquality.GetFullExpressionForVbObjectComparison(csLeft, csRight);
                case VisualBasicEqualityComparison.RequiredType.StringOnly:
                    // We know lhs isn't null, because we always coalesce it in the switch expression
                    (csLeft, csRight) = vbEquality
                        .AdjustForVbStringComparison(node.SelectStatement.Expression, csLeft, lhsTypeInfo, true, vbCase, csRight, rhsTypeInfo, false);
                    break;
            }
            return SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, csLeft, csRight);
        }

        public override async Task<SyntaxList<StatementSyntax>> VisitWithBlock(VBSyntax.WithBlockSyntax node)
        {
            var (lhsExpression, prefixDeclarations, _) = await GetExpressionWithoutSideEffectsAsync(node.WithStatement.Expression, "withBlock");

            _withBlockLhs.Push(lhsExpression);
            try {
                var statements = await ConvertStatementsAsync(node.Statements);

                var statementSyntaxs = SyntaxFactory.List(prefixDeclarations.Concat(statements));
                return prefixDeclarations.Any()
                    ? SingleStatement(SyntaxFactory.Block(statementSyntaxs))
                    : statementSyntaxs;
            } finally {
                _withBlockLhs.Pop();
            }
        }

        private string GetUniqueVariableNameInScope(VBasic.VisualBasicSyntaxNode node, string variableNameBase)
        {
            return NameGenerator.GetUniqueVariableNameInScope(_semanticModel, _generatedNames, node, variableNameBase);
        }

        public override async Task<SyntaxList<StatementSyntax>> VisitTryBlock(VBSyntax.TryBlockSyntax node)
        {
            var block = SyntaxFactory.Block(await ConvertStatementsAsync(node.Statements));
            return SingleStatement(
                SyntaxFactory.TryStatement(
                    block,
                    SyntaxFactory.List(await node.CatchBlocks.SelectAsync(async c => (CatchClauseSyntax) await c.AcceptAsync(_expressionVisitor))),
                    (FinallyClauseSyntax) await node.FinallyBlock.AcceptAsync(_expressionVisitor)
                )
            );
        }

        public override async Task<SyntaxList<StatementSyntax>> VisitSyncLockBlock(VBSyntax.SyncLockBlockSyntax node)
        {
            return SingleStatement(SyntaxFactory.LockStatement(
                (ExpressionSyntax) await node.SyncLockStatement.Expression.AcceptAsync(_expressionVisitor),
                SyntaxFactory.Block(await ConvertStatementsAsync(node.Statements)).UnpackNonNestedBlock()
            ));
        }

        public override async Task<SyntaxList<StatementSyntax>> VisitUsingBlock(VBSyntax.UsingBlockSyntax node)
        {
            var statementSyntax = SyntaxFactory.Block(await ConvertStatementsAsync(node.Statements));
            if (node.UsingStatement.Expression == null) {
                StatementSyntax stmt = statementSyntax;
                foreach (var v in node.UsingStatement.Variables.Reverse())
                foreach (var declaration in (await SplitVariableDeclarationsAsync(v)).Variables.Reverse())
                    stmt = SyntaxFactory.UsingStatement(declaration.Decl, null, stmt);
                return SingleStatement(stmt);
            }

            var expr = (ExpressionSyntax) await node.UsingStatement.Expression.AcceptAsync(_expressionVisitor);
            var unpackPossiblyNestedBlock = statementSyntax.UnpackPossiblyNestedBlock(); // Allow reduced indentation for multiple usings in a row
            return SingleStatement(SyntaxFactory.UsingStatement(null, expr, unpackPossiblyNestedBlock));
        }

        public override async Task<SyntaxList<StatementSyntax>> VisitWhileBlock(VBSyntax.WhileBlockSyntax node)
        {
            return SingleStatement(SyntaxFactory.WhileStatement(
                (ExpressionSyntax) await node.WhileStatement.Condition.AcceptAsync(_expressionVisitor),
                SyntaxFactory.Block(await ConvertStatementsAsync(node.Statements)).UnpackNonNestedBlock()
            ));
        }

        public override async Task<SyntaxList<StatementSyntax>> VisitDoLoopBlock(VBSyntax.DoLoopBlockSyntax node)
        {
            var statements = SyntaxFactory.Block(await ConvertStatementsAsync(node.Statements)).UnpackNonNestedBlock();

            if (node.DoStatement.WhileOrUntilClause != null) {
                var stmt = node.DoStatement.WhileOrUntilClause;
                if (SyntaxTokenExtensions.IsKind(stmt.WhileOrUntilKeyword, VBasic.SyntaxKind.WhileKeyword))
                    return SingleStatement(SyntaxFactory.WhileStatement(
                        (ExpressionSyntax) await stmt.Condition.AcceptAsync(_expressionVisitor),
                        statements
                    ));
                return SingleStatement(SyntaxFactory.WhileStatement(
                    ((ExpressionSyntax) await stmt.Condition.AcceptAsync(_expressionVisitor)).InvertCondition(),
                    statements
                ));
            }

            var whileOrUntilStmt = node.LoopStatement.WhileOrUntilClause;
            ExpressionSyntax conditionExpression;
            bool isUntilStmt;
            if (whileOrUntilStmt != null) {
                conditionExpression = (ExpressionSyntax) await whileOrUntilStmt.Condition.AcceptAsync(_expressionVisitor);
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

        public override async Task<SyntaxList<StatementSyntax>> VisitCallStatement(VBSyntax.CallStatementSyntax node)
        {
            return SingleStatement((ExpressionSyntax) await node.Invocation.AcceptAsync(_expressionVisitor));
        }

        private SyntaxList<StatementSyntax> SingleStatement(StatementSyntax statement)
        {
            return SyntaxFactory.SingletonList(statement);
        }

        private SyntaxList<StatementSyntax> SingleStatement(ExpressionSyntax expression)
        {
            return SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.ExpressionStatement(expression));
        }
    }
}
