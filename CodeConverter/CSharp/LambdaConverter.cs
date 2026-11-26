using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using ICSharpCode.CodeConverter.CSharp.OnError;

namespace ICSharpCode.CodeConverter.CSharp;

internal class LambdaConverter
{
    private readonly SemanticModel _semanticModel;
    private readonly Stack<ExpressionSyntax> _withBlockLhs;
    private readonly HashSet<string> _extraUsingDirectives;
    private readonly ITypeContext _typeContext;
    private readonly Solution _solution;

    public LambdaConverter(CommonConversions commonConversions, SemanticModel semanticModel, Stack<ExpressionSyntax> withBlockLhs, HashSet<string> extraUsingDirectives, ITypeContext typeContext)
    {
        CommonConversions = commonConversions;
        _semanticModel = semanticModel;
        _withBlockLhs = withBlockLhs;
        _extraUsingDirectives = extraUsingDirectives;
        _typeContext = typeContext;
        _solution = CommonConversions.Document.Project.Solution;
    }

    public CommonConversions CommonConversions { get; }



    public async Task<CSharpSyntaxNode> ConvertMultiLineLambdaAsync(VBSyntax.MultiLineLambdaExpressionSyntax node)
    {
        var tracker = CommonConversions.TriviaConvertingExpressionVisitor;
        var originalIsWithinExpressionTree = tracker.IsWithinExpressionTree;
        tracker.IsWithinExpressionTree = IsWithinExpressionTree(node);
        try {
            return await ConvertInnerAsync();
        } finally {
            tracker.IsWithinExpressionTree = originalIsWithinExpressionTree;
        }

        async Task<CSharpSyntaxNode> ConvertInnerAsync()
        {
            var body = await ConvertMethodBodyStatementsAsync(node, node.Statements);
            var param = await node.SubOrFunctionHeader.ParameterList.AcceptParameterListAsync(CommonConversions.TriviaConvertingExpressionVisitor);
            return await ConvertAsync(node, param, body.ToList());
        }
    }

    public async Task<CSharpSyntaxNode> ConvertSingleLineLambdaAsync(VBSyntax.SingleLineLambdaExpressionSyntax node)
    {
        var tracker = CommonConversions.TriviaConvertingExpressionVisitor;
        var originalIsWithinExpressionTree = tracker.IsWithinExpressionTree;
        tracker.IsWithinExpressionTree = IsWithinExpressionTree(node);
        try {
            return await ConvertInnerAsync();
        } finally {
            tracker.IsWithinExpressionTree = originalIsWithinExpressionTree;
        }

        async Task<CSharpSyntaxNode> ConvertInnerAsync()
        {
            IReadOnlyCollection<StatementSyntax> convertedStatements;
            if (node.Body is VBasic.Syntax.StatementSyntax statement) {
                convertedStatements = await ConvertMethodBodyStatementsAsync(statement, statement.Yield().ToArray());
            } else {
                var csNode = await node.Body.AcceptAsync<ExpressionSyntax>(CommonConversions.TriviaConvertingExpressionVisitor);
                csNode = ConvertToReturnType((VBSyntax.ExpressionSyntax)node.Body, csNode, tracker.IsWithinExpressionTree);
                convertedStatements = new[] { SyntaxFactory.ExpressionStatement(csNode) };
            }

            var param = await node.SubOrFunctionHeader.ParameterList.AcceptParameterListAsync(CommonConversions.TriviaConvertingExpressionVisitor);
            return await ConvertAsync(node, param, convertedStatements);
        }
    }

    /// <summary>
    /// VB converts a single-line lambda's body to the return type as a Return statement would (e.g. Byte to Short), C# needs that conversion to be implicit.
    /// </summary>
    /// <remarks>
    /// Except where the C# body already has the return type, which the VB types don't show:
    /// <list type="bullet">
    /// <item>a nested lambda returned as a delegate: C# infers its type from the return type, so it needs no delegate creation</item>
    /// <item>an Object comparison in an expression tree: it becomes a C# comparison giving bool (see <see cref="VisualBasicEqualityComparison"/>), where VB gives Object</item>
    /// </list>
    /// </remarks>
    private ExpressionSyntax ConvertToReturnType(VBSyntax.ExpressionSyntax vbBody, ExpressionSyntax csBody, bool isWithinExpressionTree)
    {
        var typeInfo = _semanticModel.GetTypeInfo(vbBody);
        var isTargetTypedLambda = vbBody.SkipIntoParens() is VBSyntax.LambdaExpressionSyntax && typeInfo.ConvertedType?.TypeKind == TypeKind.Delegate;
        var isExpressionTreeObjectComparison = isWithinExpressionTree && typeInfo.Type?.SpecialType == SpecialType.System_Object &&
                                               vbBody.SkipIntoParens().IsKind(VBasic.SyntaxKind.EqualsExpression, VBasic.SyntaxKind.NotEqualsExpression,
                                                   VBasic.SyntaxKind.LessThanExpression, VBasic.SyntaxKind.LessThanOrEqualExpression,
                                                   VBasic.SyntaxKind.GreaterThanExpression, VBasic.SyntaxKind.GreaterThanOrEqualExpression);
        return isTargetTypedLambda || isExpressionTreeObjectComparison ? csBody : CommonConversions.TypeConversionAnalyzer.AddExplicitConversion(vbBody, csBody);
    }

    public async Task<CSharpSyntaxNode> ConvertAsync(VBSyntax.LambdaExpressionSyntax vbNode,
        ParameterListSyntax param, IReadOnlyCollection<StatementSyntax> convertedStatements)
    {
        BlockSyntax block = null;
        ExpressionSyntax expressionBody = null;
        ArrowExpressionClauseSyntax arrow = null;
        bool hasComments = vbNode.DescendantTrivia().Any(t => t.IsKind(VBasic.SyntaxKind.CommentTrivia));
        if (hasComments || !convertedStatements.TryUnpackSingleStatement(out StatementSyntax singleStatement)) {
            convertedStatements = convertedStatements.Select(l => l.WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)).ToList();
            block = SyntaxFactory.Block(convertedStatements);
        } else if (singleStatement.TryUnpackSingleExpressionFromStatement(out expressionBody)) {
            if (vbNode is VBasic.Syntax.MultiLineLambdaExpressionSyntax && singleStatement is not ExpressionStatementSyntax && singleStatement is not ReturnStatementSyntax) {
                singleStatement = singleStatement.WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);
                block = SyntaxFactory.Block(singleStatement);
                expressionBody = null;
            } else {
                arrow = SyntaxFactory.ArrowExpressionClause(expressionBody);
            }
        } else {
            if (vbNode is VBasic.Syntax.MultiLineLambdaExpressionSyntax) {
                singleStatement = singleStatement.WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);
            }
            block = SyntaxFactory.Block(singleStatement);
        }

        var functionStatement = await ConvertToFunctionDeclarationOrNullAsync(vbNode, param, block, arrow);
        if (functionStatement != null) {
            return functionStatement;
        }

        var body = (CSharpSyntaxNode)block ?? expressionBody;
        param = WithParametersOmittedByRelaxation(vbNode, param);
        var isAnonAsync = _semanticModel.GetOperation(vbNode) is IAnonymousFunctionOperation a && a.Symbol.IsAsync;

        var asyncKeyword = SyntaxFactory.Token(SyntaxKind.AsyncKeyword);
        LambdaExpressionSyntax lambda;
        if (param.Parameters.Count == 1 && param.Parameters.Single().Type == null) {
            var l = SyntaxFactory.SimpleLambdaExpression(param.Parameters[0], body);
            lambda = isAnonAsync ? l.WithAsyncKeyword(asyncKeyword) : l;
        } else {
            var l = SyntaxFactory.ParenthesizedLambdaExpression(param, body);
            lambda = isAnonAsync ? l.WithAsyncKeyword(asyncKeyword) : l;
        }

        return lambda;
    }


    /// <summary>
    /// VB's relaxed delegate conversion allows a lambda with no parameters to be converted to a delegate type that has parameters
    /// (e.g. <c>Sub() ...</c> to <c>Action(Of T)</c>), whereas C# requires a lambda to declare every parameter of its delegate type.
    /// </summary>
    /// <remarks>https://learn.microsoft.com/en-us/dotnet/visual-basic/programming-guide/language-features/delegates/relaxed-delegate-conversion</remarks>
    private ParameterListSyntax WithParametersOmittedByRelaxation(VBSyntax.LambdaExpressionSyntax vbNode, ParameterListSyntax param)
    {
        if (param.Parameters.Any()) return param;
        var delegateType = CommonConversions.UnwrapLinqDelegateExpression(_semanticModel.GetTypeInfo(vbNode).ConvertedType);
        var targetParameterCount = delegateType?.GetDelegateInvokeMethod()?.Parameters.Length ?? 0;
        return targetParameterCount > 0 ? CommonConversions.CreateThrowawayParameterList(targetParameterCount).WithTriviaFrom(param) : param;
    }

    private async Task<CSharpSyntaxNode> ConvertToFunctionDeclarationOrNullAsync(VBSyntax.LambdaExpressionSyntax vbNode,
        ParameterListSyntax param, BlockSyntax block,
        ArrowExpressionClauseSyntax arrow)
    {
        if (!(_semanticModel.GetOperation(vbNode) is IAnonymousFunctionOperation anonFuncOp) || anonFuncOp.GetParentIgnoringConversions() is IDelegateCreationOperation dco && !dco.IsImplicit) {
            return null;
        }

        var potentialAncestorDeclarationOperation = anonFuncOp.GetParentIgnoringConversions().GetParentIgnoringConversions();
        // Could do: See if we can improve upon returning "object" for pretty much everything (which is what the symbols say)
        // I believe that in general, special VB functions such as MultiplyObject are designed to work the same as integer when given two integers for example.
        // If all callers currently pass an integer, perhaps it'd be more idiomatic in C# to specify "int", than to have Operators
        var paramsWithTypes = anonFuncOp.Symbol.Parameters.Select(p => (ParameterSyntax) CommonConversions.CsSyntaxGenerator.ParameterDeclaration(p));

        var paramListWithTypes = param.WithParameters(SyntaxFactory.SeparatedList(paramsWithTypes));
        if (potentialAncestorDeclarationOperation is IFieldInitializerOperation fieldInit) {
            var fieldSymbol = fieldInit.InitializedFields.Single();
            if (fieldSymbol.GetResultantVisibility() != SymbolVisibility.Public && !fieldSymbol.Type.IsDelegateReferencableByName() && await _solution.IsNeverWrittenAsync(fieldSymbol)) {
                return CreateMethodDeclaration(anonFuncOp, fieldSymbol, block, arrow);
            }
        }

        if (potentialAncestorDeclarationOperation is IVariableInitializerOperation vio) {
            if (vio.GetParentIgnoringConversions().GetParentIgnoringConversions() is IVariableDeclarationGroupOperation go) {
                potentialAncestorDeclarationOperation = go.Declarations.First(d => d.Syntax.FullSpan.Contains(vbNode.FullSpan));
            } else {
                potentialAncestorDeclarationOperation = vio.Parent;
            }

            if (potentialAncestorDeclarationOperation is IVariableDeclarationOperation variableDeclaration) {
                var variableDeclaratorOperation = variableDeclaration.Declarators.Single();
                if (!variableDeclaratorOperation.Symbol.Type.IsDelegateReferencableByName() &&
                    await _solution.IsNeverWrittenAsync(variableDeclaratorOperation.Symbol)) {
                    //Should do: Check no (other) write usages exist: SymbolFinder.FindReferencesAsync + checking if they're an assignment LHS or out parameter
                    return CreateLocalFunction(anonFuncOp, variableDeclaratorOperation, paramListWithTypes, block,
                        arrow);
                }
            }
        }

        return null;
    }

    private MethodDeclarationSyntax CreateMethodDeclaration(IAnonymousFunctionOperation operation,
        IFieldSymbol fieldSymbol,
        BlockSyntax block, ArrowExpressionClauseSyntax arrow)
    {
        MethodDeclarationSyntax methodDecl =
            ((MethodDeclarationSyntax)CommonConversions.CsSyntaxGenerator.MethodDeclaration(operation.Symbol))
            .WithIdentifier(SyntaxFactory.Identifier(fieldSymbol.Name))
            .WithBody(block).WithExpressionBody(arrow);

        if (operation.Symbol.IsAsync) methodDecl = methodDecl.AddModifiers(SyntaxFactory.Token(SyntaxKind.AsyncKeyword));

        if (arrow != null) methodDecl = methodDecl.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

        return methodDecl;
    }

    private LocalFunctionStatementSyntax CreateLocalFunction(IAnonymousFunctionOperation operation,
        IVariableDeclaratorOperation variableDeclaratorOperation,
        ParameterListSyntax param, BlockSyntax block,
        ArrowExpressionClauseSyntax arrow)
    {
        string symbolName = variableDeclaratorOperation.Symbol.Name;
        LocalFunctionStatementSyntax localFunc = SyntaxFactory.LocalFunctionStatement(
            SyntaxFactory.TokenList(),
            CommonConversions.GetTypeSyntax(operation.Symbol.ReturnType),
            SyntaxFactory.Identifier(symbolName), null, param,
            SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(), block, arrow,
            SyntaxFactory.Token(SyntaxKind.SemicolonToken));


        if (operation.Symbol.IsAsync) localFunc = localFunc.AddModifiers(SyntaxFactory.Token(SyntaxKind.AsyncKeyword));

        return localFunc;
    }

    public async Task<IReadOnlyCollection<StatementSyntax>> ConvertMethodBodyStatementsAsync(VBasic.VisualBasicSyntaxNode node, IReadOnlyCollection<VBSyntax.StatementSyntax> statements, bool isIterator = false, IdentifierNameSyntax csReturnVariable = null)
    {

        var innerMethodBodyVisitor = await MethodBodyExecutableStatementVisitor.CreateAsync(node, _semanticModel, CommonConversions.TriviaConvertingExpressionVisitor, CommonConversions, CommonConversions.VisualBasicEqualityComparison, _withBlockLhs, _extraUsingDirectives, _typeContext, isIterator, csReturnVariable);
        var onErrorContext = new OnErrorConversionContext(node, _semanticModel, CommonConversions, innerMethodBodyVisitor, _extraUsingDirectives);
        return await OnErrorMethodBodyConverter.ConvertOrNullAsync(onErrorContext, statements) ?? await onErrorContext.ConvertAsync(statements);
    }

    /// <summary>
    /// A delegate lambda within an expression tree (e.g. `o.Items.Any(Function(i) ...)` in an IQueryable query) is part of the expression tree too
    /// </summary>
    private bool IsWithinExpressionTree(VBasic.VisualBasicSyntaxNode node) =>
        CommonConversions.TriviaConvertingExpressionVisitor.IsWithinExpressionTree || CommonConversions.IsLinqDelegateExpression(node);
}