using ICSharpCode.CodeConverter.CSharp.Queries;

namespace ICSharpCode.CodeConverter.CSharp;

/// <summary>
/// See Queries/README.md
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
        var originalIsWithinExpressionTree = tracker.IsWithinExpressionTree;
        tracker.IsWithinExpressionTree |= new ExpressionTreeDetector(_semanticModel, _commonConversions.KnownTypes.System_Linq_Expressions_Expression_T).IsExpressionTree(query);
        try {
            var converter = new QueryClauseConverter(query, _semanticModel, _commonConversions, _triviaConvertingVisitor, _rangeVariableReplacements, _generatedNames, _activeBuilders, originalIsWithinExpressionTree);
            return await converter.ConvertAsync();
        } finally {
            tracker.IsWithinExpressionTree = originalIsWithinExpressionTree;
        }
    }
}