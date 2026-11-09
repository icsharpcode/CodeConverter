using ICSharpCode.CodeConverter.CSharp.Queries;

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
        bool originalIsWithinQuery = _triviaConvertingVisitor.IsWithinQuery;
        _triviaConvertingVisitor.IsWithinQuery = true;
        try {
            var converter = new QueryClauseConverter(query, _semanticModel, _commonConversions, _triviaConvertingVisitor, _rangeVariableReplacements, _generatedNames, _activeBuilders);
            return await converter.ConvertAsync();
        } finally {
            _triviaConvertingVisitor.IsWithinQuery = originalIsWithinQuery;
        }
    }
}
