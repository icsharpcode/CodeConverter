using Microsoft.CodeAnalysis.Operations;

namespace ICSharpCode.CodeConverter.CSharp.Queries;

/// <summary>
/// The VB compiler's translation of a query shows whether its lambdas become expression trees (e.g. for IQueryable) or delegates
/// </summary>
internal class ExpressionTreeDetector
{
    private readonly SemanticModel _semanticModel;
    private readonly INamedTypeSymbol _expressionType;

    public ExpressionTreeDetector(SemanticModel semanticModel, INamedTypeSymbol expressionType)
    {
        _semanticModel = semanticModel;
        _expressionType = expressionType;
    }

    /// <summary>
    /// An Aggregate clause after the first is a separate query over its own source
    /// </summary>
    public bool IsExpressionTree(VBSyntax.QueryExpressionSyntax query) =>
        HasExpressionTreeArgument(_semanticModel.GetOperation(query), syntax => !IsWithinLaterAggregateClause(syntax, query));

    public bool IsExpressionTree(VBSyntax.AggregateClauseSyntax aggregate) =>
        HasExpressionTreeArgument(_semanticModel.GetOperation(aggregate.FirstAncestorOrSelf<VBSyntax.QueryExpressionSyntax>()), syntax => aggregate.Span.Contains(syntax.Span));

    /// <remarks>
    /// Nested queries and lambdas the user wrote within the query aren't relevant
    /// </remarks>
    private bool HasExpressionTreeArgument(IOperation operation, Func<SyntaxNode, bool> isRelevant) =>
        operation is IArgumentOperation { IsImplicit: true, Parent: IInvocationOperation { IsImplicit: true }, Parameter.Type: var type } &&
        SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, _expressionType) && isRelevant(operation.Syntax) ||
        operation?.ChildOperations.Where(c => c is not ITranslatedQueryOperation).Any(c => HasExpressionTreeArgument(c, isRelevant)) == true;

    private static bool IsWithinLaterAggregateClause(SyntaxNode syntax, VBSyntax.QueryExpressionSyntax query) =>
        syntax.AncestorsAndSelf().OfType<VBSyntax.AggregateClauseSyntax>().Any(a => a.Parent == query && a != query.Clauses.First());
}
