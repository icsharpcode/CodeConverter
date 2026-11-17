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
            var param = await node.SubOrFunctionHeader.ParameterList.AcceptAsync<ParameterListSyntax>(CommonConversions.TriviaConvertingExpressionVisitor);
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
                convertedStatements = new[] { SyntaxFactory.ExpressionStatement(csNode) };
            }

            var param = await node.SubOrFunctionHeader.ParameterList.AcceptAsync<ParameterListSyntax>(CommonConversions.TriviaConvertingExpressionVisitor);
            return await ConvertAsync(node, param, convertedStatements);
        }
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
        return await OnErrorMethodBodyConverter.ConvertOrNullAsync(statements, ConvertStatementsAsync) ?? await ConvertStatementsAsync(statements);

        async Task<List<StatementSyntax>> ConvertStatementsAsync(IEnumerable<VBSyntax.StatementSyntax> vbStatements)
        {
            return (await vbStatements.SelectManyAsync(async s => (IEnumerable<StatementSyntax>)await s.Accept(innerMethodBodyVisitor.CommentConvertingVisitor))).ToList();
        }
    }

    /// <summary>
    /// A delegate lambda within an expression tree (e.g. `o.Items.Any(Function(i) ...)` in an IQueryable query) is part of the expression tree too
    /// </summary>
    private bool IsWithinExpressionTree(VBasic.VisualBasicSyntaxNode node) =>
        CommonConversions.TriviaConvertingExpressionVisitor.IsWithinExpressionTree || CommonConversions.IsLinqDelegateExpression(node);
}