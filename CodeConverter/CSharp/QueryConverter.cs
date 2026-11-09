using ICSharpCode.CodeConverter.CSharp.Queries;
using Microsoft.CodeAnalysis.Operations;

namespace ICSharpCode.CodeConverter.CSharp;

/// <summary>
/// Converts VB query expressions to C#. See <see cref="QueryClauseConverter"/> and Queries/QueryConversion.md
/// </summary>
internal class QueryConverter
{
    private readonly CommonConversions _commonConversions;
    private readonly SemanticModel _semanticModel;
    private readonly CommentConvertingVisitorWrapper _triviaConvertingVisitor;
    private readonly RangeVariableReplacements _rangeVariableReplacements;
    private readonly HashSet<string> _generatedNames;
    private readonly Stack<CsQueryBuilder> _activeBuilders = new();

    public QueryConverter(CommonConversions commonConversions, SemanticModel semanticModel, CommentConvertingVisitorWrapper triviaConvertingExpressionVisitor,
        RangeVariableReplacements rangeVariableReplacements, HashSet<string> generatedNames)
    {
        _commonConversions = commonConversions;
        _semanticModel = semanticModel;
        _triviaConvertingVisitor = triviaConvertingExpressionVisitor;
        _rangeVariableReplacements = rangeVariableReplacements;
        _generatedNames = generatedNames;
    }

    public async Task<CSharpSyntaxNode> ConvertQueryAsync(VBSyntax.QueryExpressionSyntax query)
    {
        var tracker = _triviaConvertingVisitor;
        var (originalIsWithinQuery, originalIsWithinExpressionTree) = (tracker.IsWithinQuery, tracker.IsWithinExpressionTree);
        tracker.IsWithinQuery = true;
        tracker.IsWithinExpressionTree = IsExpressionTree(_semanticModel.GetOperation(query));
        try {
            var converter = new QueryClauseConverter(query, _semanticModel, _commonConversions, _triviaConvertingVisitor, _rangeVariableReplacements, _generatedNames, _activeBuilders);
            return await converter.ConvertAsync();
        } finally {
            (tracker.IsWithinQuery, tracker.IsWithinExpressionTree) = (originalIsWithinQuery, originalIsWithinExpressionTree);
        }
    }

    /// <summary>
    /// The VB compiler's translation of the query shows whether its lambdas are passed as expression trees, e.g. to IQueryable
    /// </summary>
    private bool IsExpressionTree(IOperation operation) =>
        operation is IArgumentOperation { Parameter.Type: var type } && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, _commonConversions.KnownTypes.System_Linq_Expressions_Expression_T)
        || operation?.ChildOperations.Where(c => c is not ITranslatedQueryOperation).Any(IsExpressionTree) == true;
}
