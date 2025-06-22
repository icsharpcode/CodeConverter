using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

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
        var originalIsWithinQuery = CommonConversions.TriviaConvertingExpressionVisitor.IsWithinQuery;
        CommonConversions.TriviaConvertingExpressionVisitor.IsWithinQuery = CommonConversions.IsLinqDelegateExpression(node);
        try {
            return await ConvertInnerAsync();
        } finally {
            CommonConversions.TriviaConvertingExpressionVisitor.IsWithinQuery = originalIsWithinQuery;
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
        var originalIsWithinQuery = CommonConversions.TriviaConvertingExpressionVisitor.IsWithinQuery;
        CommonConversions.TriviaConvertingExpressionVisitor.IsWithinQuery = CommonConversions.IsLinqDelegateExpression(node);
        try {
            return await ConvertInnerAsync();
        } finally {
            CommonConversions.TriviaConvertingExpressionVisitor.IsWithinQuery = originalIsWithinQuery;
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
        if (!convertedStatements.TryUnpackSingleStatement(out StatementSyntax singleStatement)) {
            convertedStatements = convertedStatements.Select(l => l.WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)).ToList();
            block = SyntaxFactory.Block(convertedStatements);
        } else if (singleStatement.TryUnpackSingleExpressionFromStatement(out expressionBody)) {
            arrow = SyntaxFactory.ArrowExpressionClause(expressionBody);
        } else {
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
        return await GetWithConvertedGotosOrNull(statements) ?? await ConvertStatements(statements);

        async Task<List<StatementSyntax>> ConvertStatements(IEnumerable<VBSyntax.StatementSyntax> readOnlyCollection)
        {
            return (await readOnlyCollection.SelectManyAsync(async s => (IEnumerable<StatementSyntax>)await s.Accept(innerMethodBodyVisitor.CommentConvertingVisitor))).ToList();
        }

        async Task<IReadOnlyCollection<StatementSyntax>> GetWithConvertedGotosOrNull(IReadOnlyCollection<Microsoft.CodeAnalysis.VisualBasic.Syntax.StatementSyntax> statements)
        {
            var onlyIdentifierLabel = statements.OnlyOrDefault(s => s.IsKind(VBasic.SyntaxKind.LabelStatement));
            var onlyOnErrorGotoStatement = statements.OnlyOrDefault(s => s.IsKind(VBasic.SyntaxKind.OnErrorGoToLabelStatement));

            // See https://learn.microsoft.com/en-us/dotnet/visual-basic/language-reference/statements/on-error-statement
            if (onlyIdentifierLabel != null && onlyOnErrorGotoStatement != null) {
                var statementsList = statements.ToList();
                var onlyIdentifierLabelIndex = statementsList.IndexOf(onlyIdentifierLabel);
                var onlyOnErrorGotoStatementIndex = statementsList.IndexOf(onlyOnErrorGotoStatement);

                // Even this very simple case can generate compile errors if the error handling uses statements declared in the scope of the try block
                // For now, the user will have to fix these manually, in future it'd be possible to hoist any used declarations out of the try block
                if (onlyOnErrorGotoStatementIndex < onlyIdentifierLabelIndex) {
                    var beforeStatements = await ConvertStatements(statements.Take(onlyOnErrorGotoStatementIndex));
                    var tryBlockStatements = await ConvertStatements(statements.Take(onlyIdentifierLabelIndex).Skip(onlyOnErrorGotoStatementIndex + 1));
                    var tryBlock = SyntaxFactory.Block(tryBlockStatements);
                    var afterStatements = await ConvertStatements(statements.Skip(onlyIdentifierLabelIndex + 1));
                    
                    var catchClauseSyntax = SyntaxFactory.CatchClause();

                    // Default to putting the statements after the catch block in case logic falls through, but if the last statement is a return, put them inside the catch block for neatness.
                    if (tryBlockStatements.LastOrDefault().IsKind(SyntaxKind.ReturnStatement)) {
                        catchClauseSyntax = catchClauseSyntax.WithBlock(SyntaxFactory.Block(afterStatements));
                        afterStatements = new List<StatementSyntax>();
                    }

                    var tryStatement = SyntaxFactory.TryStatement(SyntaxFactory.SingletonList(catchClauseSyntax)).WithBlock(tryBlock);
                    return beforeStatements.Append(tryStatement).Concat(afterStatements).ToList();
                }
            }

            return null;
        }
    }
}