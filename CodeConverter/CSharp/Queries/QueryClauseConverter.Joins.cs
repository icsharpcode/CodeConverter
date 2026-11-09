using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ICSharpCode.CodeConverter.CSharp.Queries;

internal partial class QueryClauseConverter
{
    /// <summary>
    /// The VB range variables a C# join brings into scope, and the C# range variable they're accessed through
    /// </summary>
    private sealed record JoinedElement(IReadOnlyList<QueryRangeVariable> Variables, SyntaxToken Identifier, TypeSyntax Type, ExpressionSyntax Collection);

    /// <summary>
    /// VB allows joins nested within a join, e.g. `Join y In ys Join z In zs On z Equals y On x Equals y`.
    /// These are inner joins, so usually the same as a flat sequence of C# joins. Otherwise they're joined as a subquery.
    /// </summary>
    private async Task ConvertJoinAsync(CsQueryBuilder builder, VBSyntax.SimpleJoinClauseSyntax join)
    {
        if (_joinPlanner.Plan(join) is not { } flatJoins) {
            await AddJoinAsync(builder, await DeclareJoinedSubqueryAsync(builder, join), join.JoinConditions, keepSourceMapping: true);
            return;
        }
        foreach (var (vbJoin, conditions) in flatJoins) {
            await AddJoinAsync(builder, await DeclareJoinedVariableAsync(builder, vbJoin), conditions, keepSourceMapping: flatJoins.Count == 1);
        }
    }

    /// <param name="keepSourceMapping">False when conditions have been moved to a different join</param>
    private async Task AddJoinAsync(CsQueryBuilder builder, JoinedElement joined, IReadOnlyCollection<VBSyntax.JoinConditionSyntax> conditions, bool keepSourceMapping)
    {
        var (outerKey, innerKey) = await ConvertJoinConditionsAsync(conditions, GetSymbols(joined.Variables));
        if (!keepSourceMapping) (outerKey, innerKey) = (outerKey.WithoutSourceMapping(), innerKey.WithoutSourceMapping());
        builder.AddClause(SyntaxFactory.JoinClause(joined.Type, joined.Identifier, joined.Collection, outerKey, innerKey, null));
        foreach (var variable in joined.Variables) builder.AddToScope(variable);
    }

    private async Task ConvertGroupJoinAsync(CsQueryBuilder builder, VBSyntax.GroupJoinClauseSyntax groupJoin)
    {
        var joined = groupJoin.AdditionalJoins.Any() ? await DeclareJoinedSubqueryAsync(builder, groupJoin) : await DeclareJoinedVariableAsync(builder, groupJoin);
        var (outerKey, innerKey) = await ConvertJoinConditionsAsync(groupJoin.JoinConditions, GetSymbols(joined.Variables));
        builder.RemoveRangeVariable(joined.Identifier);

        var groupVariable = CreateGroupVariable(groupJoin.AggregationVariables);
        var into = builder.DeclareRangeVariable(groupVariable);
        builder.AddClause(SyntaxFactory.JoinClause(joined.Type, joined.Identifier, joined.Collection, outerKey, innerKey, SyntaxFactory.JoinIntoClause(into)));
        foreach (var aggregation in await ConvertAggregationsAsync(builder, groupJoin, groupJoin.AggregationVariables, groupVariable.CsExpression, joined.Variables)) {
            builder.AddToScope(aggregation);
        }
    }

    private async Task<JoinedElement> DeclareJoinedVariableAsync(CsQueryBuilder builder, VBSyntax.JoinClauseSyntax join)
    {
        var vbVariable = join.JoinedVariables.Single();
        var (type, collection) = await _sources.ConvertAsync(vbVariable);
        var variable = RegisterVariable(vbVariable);
        return new JoinedElement(new[] { variable }, builder.DeclareRangeVariable(variable), type, collection);
    }

    /// <summary>
    /// Mirrors VB's own translation: `xs.Join(ys.Join(zs, ...), ...)`
    /// </summary>
    private async Task<JoinedElement> DeclareJoinedSubqueryAsync(CsQueryBuilder builder, VBSyntax.JoinClauseSyntax join)
    {
        var (collection, variables) = await WithBuilderAsync(async subquery => {
            var vbVariable = join.JoinedVariables.Single();
            var (type, source) = await _sources.ConvertAsync(vbVariable);
            subquery.AddFrom(RegisterVariable(vbVariable), type, source);
            foreach (var nestedJoin in join.AdditionalJoins) await ConvertClauseAsync(subquery, nestedJoin);
            return (subquery.Materialize(), subquery.Scope.ToList());
        });
        return new JoinedElement(variables, builder.DeclareElementRangeVariable(variables), null, collection);
    }

    /// <summary>
    /// VB allows either side of Equals to be the joined variable, and combines conditions with And
    /// </summary>
    private async Task<(ExpressionSyntax Outer, ExpressionSyntax Inner)> ConvertJoinConditionsAsync(IReadOnlyCollection<VBSyntax.JoinConditionSyntax> conditions, IReadOnlyCollection<ISymbol> joinedSymbols)
    {
        if (!conditions.Any()) throw new NotImplementedException("Join without a condition referencing the joined variable not implemented");
        var keys = await conditions.SelectAsync(async c => {
            var (vbOuter, vbInner) = _joinPlanner.GetReferencedSymbols(c.Left, joinedSymbols).Any() ? (c.Right, c.Left) : (c.Left, c.Right);
            if (_joinPlanner.GetReferencedSymbols(vbOuter, joinedSymbols).Any()) throw new NotImplementedException("Join condition referencing the joined variable on both sides not implemented");
            return (Outer: await ConvertKeyAsync(vbOuter), Inner: await ConvertKeyAsync(vbInner));
        });
        if (keys.Length == 1) return keys[0];
        return (CreateCompositeKey(keys.Select(k => k.Outer)), CreateCompositeKey(keys.Select(k => k.Inner)));

        async Task<ExpressionSyntax> ConvertKeyAsync(VBSyntax.ExpressionSyntax vbKey) =>
            _commonConversions.TypeConversionAnalyzer.AddExplicitConversion(vbKey, await ConvertAsync(vbKey));
        static ExpressionSyntax CreateCompositeKey(IEnumerable<ExpressionSyntax> keys) =>
            CsQueryBuilder.CreateAnonymousObject(keys.Select((k, i) => ("key" + i, k)));
    }

    private static IReadOnlyCollection<ISymbol> GetSymbols(IEnumerable<QueryRangeVariable> variables) => variables.Select(v => (ISymbol)v.Symbol).ToList();
}
