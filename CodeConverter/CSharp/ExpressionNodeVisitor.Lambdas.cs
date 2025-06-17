using System.Collections.Immutable;
//using System.Data; // Not directly used by lambda methods
using System.Globalization;
//using System.Linq.Expressions; // Not directly used by lambda methods
//using System.Runtime.CompilerServices;
//using System.Xml.Linq; // Not used
//using ICSharpCode.CodeConverter.CSharp.Replacements; // Not directly used
using ICSharpCode.CodeConverter.Util.FromRoslyn; // For .Yield()
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
//using Microsoft.CodeAnalysis.Operations; // Not directly used
//using Microsoft.CodeAnalysis.Simplification; // Not directly used
//using Microsoft.VisualBasic; // Not directly used
//using Microsoft.VisualBasic.CompilerServices; // Not directly used
//using ComparisonKind = ICSharpCode.CodeConverter.CSharp.VisualBasicEqualityComparison.ComparisonKind; // Not used
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.CSharp;

internal partial class ExpressionNodeVisitor // Must be partial
{
    private readonly LambdaConverter _lambdaConverter;

    public override async Task<CSharpSyntaxNode> VisitSingleLineLambdaExpression(VBasic.Syntax.SingleLineLambdaExpressionSyntax node)
    {
        var originalIsWithinQuery = TriviaConvertingExpressionVisitor.IsWithinQuery;
        TriviaConvertingExpressionVisitor.IsWithinQuery = CommonConversions.IsLinqDelegateExpression(node);
        try {
            return await ConvertInnerAsync();
        } finally {
            TriviaConvertingExpressionVisitor.IsWithinQuery = originalIsWithinQuery;
        }

        async Task<CSharpSyntaxNode> ConvertInnerAsync()
        {
            IReadOnlyCollection<StatementSyntax> convertedStatements;
            if (node.Body is VBasic.Syntax.StatementSyntax statement)
            {
                convertedStatements = await ConvertMethodBodyStatementsAsync(statement, statement.Yield().ToArray());
            }
            else
            {
                var csNode = await node.Body.AcceptAsync<ExpressionSyntax>(TriviaConvertingExpressionVisitor);
                convertedStatements = new[] {SyntaxFactory.ExpressionStatement(csNode)};
            }

            var param = await node.SubOrFunctionHeader.ParameterList.AcceptAsync<ParameterListSyntax>(TriviaConvertingExpressionVisitor);
            return await _lambdaConverter.ConvertAsync(node, param, convertedStatements);
        }
    }

    public override async Task<CSharpSyntaxNode> VisitMultiLineLambdaExpression(VBasic.Syntax.MultiLineLambdaExpressionSyntax node)
    {
        var originalIsWithinQuery = TriviaConvertingExpressionVisitor.IsWithinQuery;
        TriviaConvertingExpressionVisitor.IsWithinQuery = CommonConversions.IsLinqDelegateExpression(node);
        try {
            return await ConvertInnerAsync();
        } finally {
            TriviaConvertingExpressionVisitor.IsWithinQuery = originalIsWithinQuery;
        }

        async Task<CSharpSyntaxNode> ConvertInnerAsync()
        {
            var body = await ConvertMethodBodyStatementsAsync(node, node.Statements);
            var param = await node.SubOrFunctionHeader.ParameterList.AcceptAsync<ParameterListSyntax>(TriviaConvertingExpressionVisitor);
            return await _lambdaConverter.ConvertAsync(node, param, body.ToList());
        }
    }

    public async Task<IReadOnlyCollection<StatementSyntax>> ConvertMethodBodyStatementsAsync(VBasic.VisualBasicSyntaxNode node, IReadOnlyCollection<VBSyntax.StatementSyntax> statements, bool isIterator = false, IdentifierNameSyntax csReturnVariable = null)
    {
        // _visualBasicEqualityComparison and _withBlockLhs are accessed via `this` from other partial classes
        var innerMethodBodyVisitor = await MethodBodyExecutableStatementVisitor.CreateAsync(node, _semanticModel, TriviaConvertingExpressionVisitor, CommonConversions, _visualBasicEqualityComparison, new Stack<ExpressionSyntax>(), _extraUsingDirectives, _typeContext, isIterator, csReturnVariable);
        return await GetWithConvertedGotosOrNull(statements) ?? await ConvertStatements(statements);

        async Task<List<StatementSyntax>> ConvertStatements(IEnumerable<VBSyntax.StatementSyntax> readOnlyCollection)
        {
            return (await readOnlyCollection.SelectManyAsync(async s => (IEnumerable<StatementSyntax>)await s.Accept(innerMethodBodyVisitor.CommentConvertingVisitor))).ToList();
        }

        async Task<IReadOnlyCollection<StatementSyntax>> GetWithConvertedGotosOrNull(IReadOnlyCollection<VBSyntax.StatementSyntax> stmts)
        {
            var onlyIdentifierLabel = stmts.OnlyOrDefault(s => s.IsKind(VBasic.SyntaxKind.LabelStatement));
            var onlyOnErrorGotoStatement = stmts.OnlyOrDefault(s => s.IsKind(VBasic.SyntaxKind.OnErrorGoToLabelStatement));

            // See https://learn.microsoft.com/en-us/dotnet/visual-basic/language-reference/statements/on-error-statement
            if (onlyIdentifierLabel != null && onlyOnErrorGotoStatement != null) {
                var statementsList = stmts.ToList();
                var onlyIdentifierLabelIndex = statementsList.IndexOf(onlyIdentifierLabel);
                var onlyOnErrorGotoStatementIndex = statementsList.IndexOf(onlyOnErrorGotoStatement);

                if (onlyOnErrorGotoStatementIndex < onlyIdentifierLabelIndex) {
                    var beforeStatements = await ConvertStatements(stmts.Take(onlyOnErrorGotoStatementIndex));
                    var tryBlockStatements = await ConvertStatements(stmts.Take(onlyIdentifierLabelIndex).Skip(onlyOnErrorGotoStatementIndex + 1));
                    var tryBlock = SyntaxFactory.Block(tryBlockStatements);
                    var afterStatements = await ConvertStatements(stmts.Skip(onlyIdentifierLabelIndex + 1));

                    var catchClauseSyntax = SyntaxFactory.CatchClause();

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
