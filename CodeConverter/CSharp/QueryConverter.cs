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
    private readonly LegacyQueryConverter _legacyQueryConverter;

    public QueryConverter(CommonConversions commonConversions, SemanticModel semanticModel, CommentConvertingVisitorWrapper triviaConvertingExpressionVisitor,
        RangeVariableReplacements rangeVariableReplacements, HashSet<string> generatedNames)
    {
        _commonConversions = commonConversions;
        _semanticModel = semanticModel;
        _triviaConvertingVisitor = triviaConvertingExpressionVisitor;
        _rangeVariableReplacements = rangeVariableReplacements;
        _generatedNames = generatedNames;
        _legacyQueryConverter = new LegacyQueryConverter(commonConversions, semanticModel, triviaConvertingExpressionVisitor);
    }

    public async Task<CSharpSyntaxNode> ConvertQueryAsync(VBSyntax.QueryExpressionSyntax query)
    {
        if (!QueryClauseConverter.IsSupported(query)) return await _legacyQueryConverter.ConvertClausesAsync(query.Clauses);

        bool originalIsWithinQuery = _triviaConvertingVisitor.IsWithinQuery;
        _triviaConvertingVisitor.IsWithinQuery = true;
        try {
            var converter = new QueryClauseConverter(query, _semanticModel, _commonConversions, _triviaConvertingVisitor, _rangeVariableReplacements, _generatedNames);
            return await converter.ConvertAsync();
        } finally {
            _triviaConvertingVisitor.IsWithinQuery = originalIsWithinQuery;
        }
    }
}
