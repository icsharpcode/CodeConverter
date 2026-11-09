using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace ICSharpCode.CodeConverter.CSharp.Queries;

internal partial class QueryClauseConverter
{
    /// <summary>
    /// What VB calls the group in an Into clause, used when it isn't otherwise named
    /// </summary>
    private const string GeneratedGroupName = "Group";

    /// <summary>
    /// VB replaces the range variables in scope with the keys and aggregations, which in C# are read from the continuation's group
    /// </summary>
    private async Task ConvertGroupByAsync(CsQueryBuilder builder, VBSyntax.GroupByClauseSyntax groupBy)
    {
        var element = groupBy.Items.Any() ? await RegisterValueVariablesAsync(groupBy.Items) : builder.Scope.ToList();
        var keys = await RegisterValueVariablesAsync(groupBy.Keys);
        var groupVariable = CreateGroupVariable(groupBy.AggregationVariables);
        builder.ContinueInto(SyntaxFactory.GroupClause(CsQueryBuilder.CreateElement(element), CsQueryBuilder.CreateElement(keys)), groupVariable);

        var groupKey = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, groupVariable.CsExpression, ValidSyntaxFactory.IdentifierName("Key"));
        foreach (var key in keys) {
            key.CsExpression = keys.Count == 1 ? groupKey : SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, groupKey, ValidSyntaxFactory.IdentifierName(key.Name));
        }
        var aggregations = await ConvertAggregationsAsync(builder, groupBy, groupBy.AggregationVariables, groupVariable.CsExpression, element);
        builder.SetScope(keys.Concat(aggregations));
    }

    /// <summary>
    /// Produces a single value: the aggregation, or an anonymous type of several
    /// </summary>
    private async Task<ExpressionSyntax> ConvertAggregateQueryAsync(VBSyntax.AggregateClauseSyntax aggregate)
    {
        var (source, element) = await ConvertAggregateSourceAsync(aggregate);
        return await WithBuilderAsync(async builder => {
            var functions = aggregate.AggregationVariables.Select(a => (VBSyntax.FunctionAggregationSyntax)a.Aggregation).ToList();
            if (functions.Count == 1) return await ConvertAggregationFunctionAsync(builder, functions[0], source, element);

            var sourceEvaluatedOnce = CsQueryBuilder.IsCheapToRepeat(source) ? null : ValidSyntaxFactory.IdentifierName(builder.GenerateName(GeneratedGroupName));
            var group = sourceEvaluatedOnce ?? source;
            var values = await functions.SelectAsync(f => ConvertAggregationFunctionAsync(builder, f, group, element));
            var names = aggregate.AggregationVariables.Select(a => VBasic.VisualBasicExtensions.GetDeclaredSymbol(_semanticModel, a).Name);
            var result = CsQueryBuilder.CreateAnonymousObject(names.Zip(values, (n, v) => (n, v)));
            return sourceEvaluatedOnce == null ? result : EvaluateOnce(source, sourceEvaluatedOnce.Identifier, result);
        });
    }

    /// <summary>
    /// `new[] { value }.Select(identifier => result).Single()`, since C# has no let expression
    /// </summary>
    private static ExpressionSyntax EvaluateOnce(ExpressionSyntax value, SyntaxToken identifier, ExpressionSyntax result)
    {
        var array = SyntaxFactory.ImplicitArrayCreationExpression(SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression, SyntaxFactory.SingletonSeparatedList(value)));
        var lambda = SyntaxFactory.SimpleLambdaExpression(SyntaxFactory.Parameter(identifier), result);
        var select = SyntaxFactory.InvocationExpression(ValidSyntaxFactory.MemberAccess(array, nameof(Enumerable.Select)),
            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(lambda))));
        return SyntaxFactory.InvocationExpression(ValidSyntaxFactory.MemberAccess(select, nameof(Enumerable.Single)), SyntaxFactory.ArgumentList());
    }

    /// <summary>
    /// Several aggregations share one evaluation of the nested query, as in VB's translation
    /// </summary>
    private async Task ConvertAggregateClauseAsync(CsQueryBuilder builder, VBSyntax.AggregateClauseSyntax aggregate)
    {
        var originalIsWithinExpressionTree = _visitor.IsWithinExpressionTree;
        _visitor.IsWithinExpressionTree |= _expressionTrees.IsExpressionTree(aggregate);
        try {
            await ConvertAggregateClauseWithinTrackedScopeAsync(builder, aggregate);
        } finally {
            _visitor.IsWithinExpressionTree = originalIsWithinExpressionTree;
        }
    }

    private async Task ConvertAggregateClauseWithinTrackedScopeAsync(CsQueryBuilder builder, VBSyntax.AggregateClauseSyntax aggregate)
    {
        var (source, element) = await ConvertAggregateSourceAsync(aggregate);
        var group = source;
        if (aggregate.AggregationVariables.Count > 1) {
            var groupVariable = new QueryRangeVariable(null, GeneratedGroupName);
            builder.DefineVariable(groupVariable, source, CsQueryBuilder.IsCheapToRepeat(source) && !_substitution.IsShadowedLater(source, aggregate), keepSourceMapping: false);
            group = groupVariable.CsExpression;
        }
        foreach (var aggregation in await ConvertAggregationsAsync(builder, aggregate, aggregate.AggregationVariables, group, element)) {
            builder.AddToScope(aggregation);
        }
    }

    private Task<(ExpressionSyntax Source, IReadOnlyList<QueryRangeVariable> Element)> ConvertAggregateSourceAsync(VBSyntax.AggregateClauseSyntax aggregate) =>
        WithBuilderAsync(async builder => {
            foreach (var vbVariable in aggregate.Variables) {
                var (type, collection) = await _sources.ConvertAsync(vbVariable);
                builder.AddFrom(RegisterVariable(vbVariable), type, collection);
            }
            var enclosingSubstitution = _substitution;
            _substitution = SubstitutionRules.ForAggregateSource(_vbQuery, aggregate, _semanticModel);
            try {
                foreach (var clause in aggregate.AdditionalQueryOperators) {
                    await ConvertClauseAsync(builder, clause);
                }
            } finally {
                _substitution = enclosingSubstitution;
            }
            return (builder.Materialize(), builder.Scope);
        });

    private QueryRangeVariable CreateGroupVariable(SeparatedSyntaxList<VBSyntax.AggregationRangeVariableSyntax> aggregations) =>
        aggregations.FirstOrDefault(a => a.Aggregation is VBSyntax.GroupAggregationSyntax) is { } groupAggregation
            ? RegisterVariable(groupAggregation)
            : new QueryRangeVariable(null, GeneratedGroupName);

    private async Task<List<QueryRangeVariable>> ConvertAggregationsAsync(CsQueryBuilder builder, VBSyntax.QueryClauseSyntax clause, SeparatedSyntaxList<VBSyntax.AggregationRangeVariableSyntax> vbAggregations,
        ExpressionSyntax group, IReadOnlyList<QueryRangeVariable> groupElement)
    {
        var aggregations = new List<QueryRangeVariable>();
        foreach (var vbAggregation in vbAggregations) {
            var variable = RegisterVariable(vbAggregation);
            if (vbAggregation.Aggregation is VBSyntax.FunctionAggregationSyntax function) {
                var value = await ConvertAggregationFunctionAsync(builder, function, group, groupElement);
                builder.DefineVariable(variable, value, _substitution.CanSubstitute(variable, vbAggregation, value), _substitution.IsElementMaterializedNext(clause));
            } else {
                variable.CsExpression = group;
            }
            aggregations.Add(variable);
        }
        return aggregations;
    }

    /// <summary>
    /// VB's `Sum(x.Total)` calls a method on the group with a lambda over the group's element: `group.Sum(x => x.Total)`
    /// </summary>
    private async Task<ExpressionSyntax> ConvertAggregationFunctionAsync(CsQueryBuilder builder, VBSyntax.FunctionAggregationSyntax function, ExpressionSyntax group, IReadOnlyList<QueryRangeVariable> groupElement)
    {
        var methodName = _semanticModel.GetSymbolInfo(function).Symbol?.Name ?? function.FunctionName.ValueText;
        var arguments = function.Argument == null
            ? Array.Empty<ArgumentSyntax>()
            : new[] { SyntaxFactory.Argument(await builder.CreateElementLambdaAsync(groupElement, () => ConvertAggregationArgumentAsync(function.Argument))) };
        var method = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, group.AddParensUnlessPrimary(), ValidSyntaxFactory.IdentifierName(methodName));
        return SyntaxFactory.InvocationExpression(method, SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));
    }

    /// <summary>
    /// VB converts the argument to the lambda's return type, e.g. a Boolean? predicate to Boolean (throwing for Nothing)
    /// </summary>
    private async Task<ExpressionSyntax> ConvertAggregationArgumentAsync(VBSyntax.ExpressionSyntax argument) =>
        _commonConversions.TypeConversionAnalyzer.AddExplicitConversion(argument, await ConvertAsync(argument));

    /// <summary>
    /// Used for group keys and items, which are only evaluated where the group clause is
    /// </summary>
    private async Task<List<QueryRangeVariable>> RegisterValueVariablesAsync(SeparatedSyntaxList<VBSyntax.ExpressionRangeVariableSyntax> vbVariables)
    {
        var variables = new List<QueryRangeVariable>();
        foreach (var vbVariable in vbVariables) {
            var value = await ConvertValueAsync(vbVariable, withoutParentheses: true);
            var variable = RegisterVariable(vbVariable);
            variable.CsExpression = value;
            variables.Add(variable);
        }
        return variables;
    }
}
