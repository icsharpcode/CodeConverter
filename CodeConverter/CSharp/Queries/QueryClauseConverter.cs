using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using VBSyntaxKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;

namespace ICSharpCode.CodeConverter.CSharp.Queries;

/// <summary>
/// Converts a single VB query expression, one clause at a time, into a <see cref="CsQueryBuilder"/>.
/// </summary>
/// <remarks>
/// Each VB clause maps to the C# query clause with matching semantics where one exists, falling back to method syntax for the rest.
/// The VB binder's translation of each clause into method calls is documented in Binder_Query.vb in the Roslyn repo, see QueryConversion.md.
/// </remarks>
internal class QueryClauseConverter
{
    /// <summary>
    /// Base name for a generated group variable, matching the name VB gives the group in an Into clause
    /// </summary>
    private const string GroupName = "Group";

    private readonly VBSyntax.QueryExpressionSyntax _vbQuery;
    private readonly SemanticModel _semanticModel;
    private readonly CommonConversions _commonConversions;
    private readonly CommentConvertingVisitorWrapper _visitor;
    private readonly RangeVariableReplacements _replacements;
    private readonly HashSet<string> _generatedNames;
    private readonly Stack<CsQueryBuilder> _activeBuilders;
    private readonly List<QueryRangeVariable> _declaredVariables = new();

    /// <param name="activeBuilders">The builders for queries currently being converted, the innermost of which encloses this query</param>
    public QueryClauseConverter(VBSyntax.QueryExpressionSyntax vbQuery, SemanticModel semanticModel, CommonConversions commonConversions,
        CommentConvertingVisitorWrapper visitor, RangeVariableReplacements replacements, HashSet<string> generatedNames, Stack<CsQueryBuilder> activeBuilders)
    {
        _activeBuilders = activeBuilders;
        _vbQuery = vbQuery;
        _semanticModel = semanticModel;
        _commonConversions = commonConversions;
        _visitor = visitor;
        _replacements = replacements;
        _generatedNames = generatedNames;
    }

    public async Task<ExpressionSyntax> ConvertAsync()
    {
        try {
            var clauses = _vbQuery.Clauses;
            if (clauses.First() is VBSyntax.AggregateClauseSyntax aggregate) {
                if (clauses.Count > 1) throw new NotImplementedException("Query operators after an initial Aggregate clause not implemented");
                return await ConvertAggregateQueryAsync(aggregate);
            }
            return await WithBuilderAsync(async builder => {
                var finalSelect = clauses.Last() as VBSyntax.SelectClauseSyntax;
                foreach (var clause in finalSelect == null ? clauses : clauses.Take(clauses.Count - 1)) {
                    await ConvertClauseAsync(builder, clause);
                }
                if (finalSelect == null) return builder.Materialize();
                builder.EnsureQuery();
                return builder.Build(await ConvertFinalSelectAsync(finalSelect));
            });
        } finally {
            foreach (var variable in _declaredVariables) _replacements.Unregister(variable);
        }
    }

    /// <summary>
    /// An initial Aggregate produces a single value rather than a sequence: the aggregation, or an anonymous type when there are several
    /// </summary>
    private async Task<ExpressionSyntax> ConvertAggregateQueryAsync(VBSyntax.AggregateClauseSyntax aggregate)
    {
        var (source, element) = await ConvertAggregateSourceAsync(aggregate);
        return await WithBuilderAsync(async builder => {
            var functions = aggregate.AggregationVariables.Select(a => (VBSyntax.FunctionAggregationSyntax)a.Aggregation).ToList();
            if (functions.Count == 1) return await ConvertAggregationFunctionAsync(builder, functions[0], source, element);

            var group = CsQueryBuilder.IsCheapToRepeat(source) ? source : ValidSyntaxFactory.IdentifierName(builder.GenerateName(GroupName));
            var values = await functions.SelectAsync(f => ConvertAggregationFunctionAsync(builder, f, group, element));
            var names = aggregate.AggregationVariables.Select(a => VBasic.VisualBasicExtensions.GetDeclaredSymbol(_semanticModel, a).Name);
            var result = CsQueryBuilder.CreateAnonymousObject(names.Zip(values, (n, v) => (n, v)));
            if (group == source) return result;

            // VB evaluates the source once for all the aggregations
            var sourceArray = SyntaxFactory.ImplicitArrayCreationExpression(SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression, SyntaxFactory.SingletonSeparatedList(source)));
            var select = SyntaxFactory.InvocationExpression(ValidSyntaxFactory.MemberAccess(sourceArray, nameof(Enumerable.Select)),
                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(SyntaxFactory.SimpleLambdaExpression(SyntaxFactory.Parameter(((IdentifierNameSyntax)group).Identifier), result)))));
            return SyntaxFactory.InvocationExpression(ValidSyntaxFactory.MemberAccess(select, nameof(Enumerable.Single)), SyntaxFactory.ArgumentList());
        });
    }

    /// <summary>
    /// Within a query, Aggregate adds a range variable for each aggregation over a nested query.
    /// When there are several, the nested query is evaluated once into a group variable, mirroring VB's translation.
    /// </summary>
    private async Task ConvertAggregateClauseAsync(CsQueryBuilder builder, VBSyntax.AggregateClauseSyntax aggregate)
    {
        var (source, element) = await ConvertAggregateSourceAsync(aggregate);
        var group = source;
        if (aggregate.AggregationVariables.Count > 1) {
            var groupVariable = new QueryRangeVariable(null, GroupName);
            builder.Define(groupVariable, source, CsQueryBuilder.IsCheapToRepeat(source) && !IsShadowedLater(source, aggregate), isUsedInPlace: false);
            group = groupVariable.CsExpression;
        }
        foreach (var aggregation in await ConvertAggregationsAsync(builder, aggregate, aggregate.AggregationVariables, group, element)) {
            builder.AddToScope(aggregation);
        }
    }

    private Task<(ExpressionSyntax Source, IReadOnlyList<QueryRangeVariable> Element)> ConvertAggregateSourceAsync(VBSyntax.AggregateClauseSyntax aggregate) =>
        WithBuilderAsync(async builder => {
            foreach (var vbVariable in aggregate.Variables) {
                var (type, collection) = await ConvertCollectionAsync(vbVariable);
                builder.AddFrom(DeclareVariable(vbVariable), type, collection);
            }
            foreach (var clause in aggregate.AdditionalQueryOperators) {
                await ConvertClauseAsync(builder, clause);
            }
            return (builder.Materialize(), builder.Scope);
        });

    private Task ConvertClauseAsync(CsQueryBuilder builder, VBSyntax.QueryClauseSyntax clause)
    {
        if (!IsMethodBoundary(clause)) builder.EnsureQuery();
        return clause switch {
            VBSyntax.FromClauseSyntax x => ConvertFromAsync(builder, x),
            VBSyntax.LetClauseSyntax x => ConvertLetAsync(builder, x),
            VBSyntax.WhereClauseSyntax x => ConvertWhereAsync(builder, x),
            VBSyntax.OrderByClauseSyntax x => ConvertOrderByAsync(builder, x),
            VBSyntax.SelectClauseSyntax x => ConvertSelectAsync(builder, x),
            VBSyntax.DistinctClauseSyntax x => ConvertDistinctAsync(builder, x),
            VBSyntax.PartitionClauseSyntax x => ConvertPartitionAsync(builder, x),
            VBSyntax.PartitionWhileClauseSyntax x => ConvertPartitionWhileAsync(builder, x),
            VBSyntax.SimpleJoinClauseSyntax x => ConvertJoinAsync(builder, x),
            VBSyntax.GroupJoinClauseSyntax x => ConvertGroupJoinAsync(builder, x),
            VBSyntax.GroupByClauseSyntax x => ConvertGroupByAsync(builder, x),
            VBSyntax.AggregateClauseSyntax x => ConvertAggregateClauseAsync(builder, x),
            _ => throw new NotImplementedException($"Conversion for query clause with kind '{clause.Kind()}' not implemented")
        };
    }

    /// <summary>
    /// Clauses with no C# query equivalent, which are converted to a method call on the sequence of elements
    /// </summary>
    private static bool IsMethodBoundary(VBSyntax.QueryClauseSyntax clause) =>
        clause is VBSyntax.DistinctClauseSyntax or VBSyntax.PartitionClauseSyntax or VBSyntax.PartitionWhileClauseSyntax;

    private async Task ConvertFromAsync(CsQueryBuilder builder, VBSyntax.FromClauseSyntax from)
    {
        foreach (var vbVariable in from.Variables) {
            var (type, collection) = await ConvertCollectionAsync(vbVariable);
            builder.AddFrom(DeclareVariable(vbVariable), type, collection);
        }
    }

    private async Task ConvertLetAsync(CsQueryBuilder builder, VBSyntax.LetClauseSyntax let)
    {
        foreach (var vbVariable in let.Variables) {
            var value = await ConvertAsync(vbVariable.Expression);
            var variable = DeclareVariable(vbVariable);
            builder.Define(variable, value, CanInline(variable, vbVariable, value), IsElementUsedNext(let));
            builder.AddToScope(variable);
        }
    }

    private async Task ConvertWhereAsync(CsQueryBuilder builder, VBSyntax.WhereClauseSyntax where) =>
        builder.AddClause(SyntaxFactory.WhereClause(await ConvertAsync(where.Condition)));

    private async Task ConvertOrderByAsync(CsQueryBuilder builder, VBSyntax.OrderByClauseSyntax orderBy)
    {
        var orderings = await orderBy.Orderings.SelectAsync(o => o.AcceptAsync<OrderingSyntax>(_visitor));
        builder.AddClause(SyntaxFactory.OrderByClause(SyntaxFactory.SeparatedList(orderings)));
    }

    /// <summary>
    /// A VB select that isn't the last clause replaces the range variables in scope. C# can't drop the old ones without a continuation, but it doesn't need to:
    /// any clashing names are renamed, and the element is constructed from VB's scope when it's needed.
    /// </summary>
    private async Task ConvertSelectAsync(CsQueryBuilder builder, VBSyntax.SelectClauseSyntax select)
    {
        var values = await select.Variables.SelectAsync(v => ConvertAsync(v.Expression));
        var variables = select.Variables.Select(DeclareVariable).ToList();
        for (int i = 0; i < variables.Count; i++) {
            builder.Define(variables[i], values[i], CanInline(variables[i], select.Variables[i], values[i]), IsElementUsedNext(select));
        }
        builder.SetScope(variables);
    }

    private async Task<SelectClauseSyntax> ConvertFinalSelectAsync(VBSyntax.SelectClauseSyntax select)
    {
        var values = await select.Variables.SelectAsync(v => ConvertWithoutParenthesesAsync(v.Expression));
        if (values.Length == 1) return SyntaxFactory.SelectClause(values[0]);
        var names = select.Variables.Select(GetName);
        return SyntaxFactory.SelectClause(CsQueryBuilder.CreateAnonymousObject(names.Zip(values, (n, v) => (n, v))));
    }

    private static Task ConvertDistinctAsync(CsQueryBuilder builder, VBSyntax.DistinctClauseSyntax distinct)
    {
        builder.ApplyMethod(distinct, nameof(Enumerable.Distinct));
        return Task.CompletedTask;
    }

    private async Task ConvertPartitionAsync(CsQueryBuilder builder, VBSyntax.PartitionClauseSyntax partition)
    {
        var methodName = partition.SkipOrTakeKeyword.IsKind(VBSyntaxKind.SkipKeyword) ? nameof(Enumerable.Skip) : nameof(Enumerable.Take);
        builder.ApplyMethod(partition, methodName, await ConvertAsync(partition.Count));
    }

    private async Task ConvertPartitionWhileAsync(CsQueryBuilder builder, VBSyntax.PartitionWhileClauseSyntax partitionWhile)
    {
        var methodName = partitionWhile.SkipOrTakeKeyword.IsKind(VBSyntaxKind.SkipKeyword) ? nameof(Enumerable.SkipWhile) : nameof(Enumerable.TakeWhile);
        var predicate = await builder.CreateElementLambdaAsync(builder.Scope, () => ConvertAsync(partitionWhile.Condition));
        builder.ApplyMethod(partitionWhile, methodName, predicate);
    }

    /// <summary>
    /// VB allows joins to be nested within a join, e.g. `Join y In ys Join z In zs On z Equals y On x Equals y`.
    /// For inner joins, the result is the same as a flat sequence of joins, so each condition is attached to the earliest join where everything it references is in scope.
    /// </summary>
    private async Task ConvertJoinAsync(CsQueryBuilder builder, VBSyntax.SimpleJoinClauseSyntax join)
    {
        var plan = PlanFlattenedJoins(join) ?? throw new NotImplementedException("Group Join nested within Join not implemented");
        foreach (var (vbJoin, conditions) in plan) {
            var joined = await DeclareJoinedVariableAsync(builder, vbJoin);
            var (outerKey, innerKey) = await ConvertJoinConditionsAsync(conditions, GetSymbols(joined.Element));
            if (plan.Count > 1) (outerKey, innerKey) = (outerKey.WithoutSourceMapping(), innerKey.WithoutSourceMapping()); // Conditions have been reordered
            builder.AddClause(SyntaxFactory.JoinClause(joined.Type, joined.Identifier, joined.Collection, outerKey, innerKey, null));
            foreach (var variable in joined.Element) builder.AddToScope(variable);
        }
    }

    /// <returns>Each join with the conditions to attach to it, or null if they can't be flattened</returns>
    private List<(VBSyntax.JoinClauseSyntax Join, List<VBSyntax.JoinConditionSyntax> Conditions)> PlanFlattenedJoins(VBSyntax.SimpleJoinClauseSyntax join)
    {
        var joins = FlattenJoins(join).ToList();
        if (joins.Skip(1).Any(j => j is not VBSyntax.SimpleJoinClauseSyntax)) return null;
        var unplacedConditions = joins.SelectMany(j => j.JoinConditions).ToList();
        var joinedSymbols = joins.Select(GetJoinedSymbol).ToList();
        var inScope = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        var plan = new List<(VBSyntax.JoinClauseSyntax, List<VBSyntax.JoinConditionSyntax>)>();
        foreach (var vbJoin in joins) {
            var joinedSymbol = GetJoinedSymbol(vbJoin);
            inScope.Add(joinedSymbol);
            var conditions = unplacedConditions.Where(c => {
                var referenced = GetReferencedSymbols(c, joinedSymbols);
                return referenced.Contains(joinedSymbol) && referenced.All(inScope.Contains);
            }).ToList();
            unplacedConditions.RemoveAll(conditions.Contains);
            plan.Add((vbJoin, conditions));
        }
        return unplacedConditions.Any() ? null : plan;
    }

    private async Task ConvertGroupJoinAsync(CsQueryBuilder builder, VBSyntax.GroupJoinClauseSyntax groupJoin)
    {
        if (groupJoin.AdditionalJoins.Any()) throw new NotImplementedException("Join nested within Group Join not implemented");
        var joined = await DeclareJoinedVariableAsync(builder, groupJoin);
        var (outerKey, innerKey) = await ConvertJoinConditionsAsync(groupJoin.JoinConditions, GetSymbols(joined.Element));
        builder.RemoveRangeVariable(joined.Identifier);

        var groupVariable = GetGroupVariable(groupJoin.AggregationVariables);
        var into = builder.DeclareRangeVariable(groupVariable);
        builder.AddClause(SyntaxFactory.JoinClause(joined.Type, joined.Identifier, joined.Collection, outerKey, innerKey, SyntaxFactory.JoinIntoClause(into)));
        foreach (var aggregation in await ConvertAggregationsAsync(builder, groupJoin, groupJoin.AggregationVariables, groupVariable.CsExpression, joined.Element)) {
            builder.AddToScope(aggregation);
        }
    }

    /// <summary>
    /// Group By replaces the VB range variables in scope with its keys and aggregations, which C# can only do with a continuation.
    /// The keys and aggregations are then expressed in terms of the group.
    /// </summary>
    private async Task ConvertGroupByAsync(CsQueryBuilder builder, VBSyntax.GroupByClauseSyntax groupBy)
    {
        var element = groupBy.Items.Any() ? await DeclareExpressionVariablesAsync(groupBy.Items) : builder.Scope.ToList();
        var keys = await DeclareExpressionVariablesAsync(groupBy.Keys);
        var groupClause = SyntaxFactory.GroupClause(CsQueryBuilder.CreateElement(element), CsQueryBuilder.CreateElement(keys));

        var groupVariable = GetGroupVariable(groupBy.AggregationVariables);
        var into = builder.ContinueInto(groupClause, groupVariable.Name, groupVariable.Symbol == null);
        var group = SyntaxFactory.IdentifierName(into);
        var groupKey = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, group, ValidSyntaxFactory.IdentifierName("Key"));
        foreach (var key in keys) {
            key.CsExpression = keys.Count == 1 ? groupKey : SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, groupKey, ValidSyntaxFactory.IdentifierName(key.Name));
        }
        var aggregations = await ConvertAggregationsAsync(builder, groupBy, groupBy.AggregationVariables, group, element);
        builder.SetScope(keys.Concat(aggregations));
    }

    private async Task<List<QueryRangeVariable>> DeclareExpressionVariablesAsync(SeparatedSyntaxList<VBSyntax.ExpressionRangeVariableSyntax> vbVariables)
    {
        var variables = new List<QueryRangeVariable>();
        foreach (var vbVariable in vbVariables) {
            var value = await ConvertWithoutParenthesesAsync(vbVariable.Expression);
            var variable = DeclareVariable(vbVariable);
            variable.CsExpression = value;
            variables.Add(variable);
        }
        return variables;
    }

    /// <summary>
    /// The joined element: the VB range variables a C# join brings into scope, and the C# range variable that refers to them
    /// </summary>
    private sealed record JoinedElement(IReadOnlyList<QueryRangeVariable> Element, SyntaxToken Identifier, TypeSyntax Type, ExpressionSyntax Collection);

    private async Task<JoinedElement> DeclareJoinedVariableAsync(CsQueryBuilder builder, VBSyntax.JoinClauseSyntax join)
    {
        var vbVariable = join.JoinedVariables.Single();
        var (type, collection) = await ConvertCollectionAsync(vbVariable);
        var variable = DeclareVariable(vbVariable);
        return new JoinedElement(new[] { variable }, builder.DeclareRangeVariable(variable), type, collection);
    }

    private IRangeVariableSymbol GetJoinedSymbol(VBSyntax.JoinClauseSyntax join) => VBasic.VisualBasicExtensions.GetDeclaredSymbol(_semanticModel, join.JoinedVariables.Single());

    private static IReadOnlyCollection<ISymbol> GetSymbols(IEnumerable<QueryRangeVariable> variables) => variables.Select(v => (ISymbol)v.Symbol).ToList();

    /// <summary>
    /// C# requires the outer key on the left of equals, and multiple conditions to be combined into a single key
    /// </summary>
    private async Task<(ExpressionSyntax Outer, ExpressionSyntax Inner)> ConvertJoinConditionsAsync(IReadOnlyCollection<VBSyntax.JoinConditionSyntax> conditions, IReadOnlyCollection<ISymbol> joinedSymbols)
    {
        if (!conditions.Any()) throw new NotImplementedException("Join without a condition referencing the joined variable not implemented");
        var keys = await conditions.SelectAsync(async c => {
            var (vbOuter, vbInner) = GetReferencedSymbols(c.Left, joinedSymbols).Any() ? (c.Right, c.Left) : (c.Left, c.Right);
            if (GetReferencedSymbols(vbOuter, joinedSymbols).Any()) throw new NotImplementedException("Join condition referencing the joined variable on both sides not implemented");
            return (Outer: await ConvertKeyAsync(vbOuter), Inner: await ConvertKeyAsync(vbInner));
        });
        if (keys.Length == 1) return keys[0];
        return (CreateJoinKey(keys.Select(k => k.Outer)), CreateJoinKey(keys.Select(k => k.Inner)));

        async Task<ExpressionSyntax> ConvertKeyAsync(VBSyntax.ExpressionSyntax vbKey) =>
            _commonConversions.TypeConversionAnalyzer.AddExplicitConversion(vbKey, await ConvertAsync(vbKey));
        static ExpressionSyntax CreateJoinKey(IEnumerable<ExpressionSyntax> keys) =>
            CsQueryBuilder.CreateAnonymousObject(keys.Select((k, i) => ("key" + i, k)));
    }

    private static IEnumerable<VBSyntax.JoinClauseSyntax> FlattenJoins(VBSyntax.JoinClauseSyntax join) =>
        join.AdditionalJoins.SelectMany(FlattenJoins).Prepend(join);

    private HashSet<ISymbol> GetReferencedSymbols(SyntaxNode node, IReadOnlyCollection<ISymbol> candidates) =>
        new(node.DescendantNodesAndSelf().OfType<VBSyntax.IdentifierNameSyntax>()
            .Select(id => _semanticModel.GetSymbolInfo(id).Symbol)
            .Where(s => s != null && candidates.Contains(s, SymbolEqualityComparer.Default)), SymbolEqualityComparer.Default);

    /// <summary>
    /// The C# variable for a group, named after VB's `Group` aggregation if there is one
    /// </summary>
    private QueryRangeVariable GetGroupVariable(SeparatedSyntaxList<VBSyntax.AggregationRangeVariableSyntax> aggregations) =>
        aggregations.FirstOrDefault(a => a.Aggregation is VBSyntax.GroupAggregationSyntax) is { } groupAggregation
            ? DeclareVariable(groupAggregation)
            : new QueryRangeVariable(null, GroupName);

    /// <summary>
    /// Each aggregation is a method call on the group, e.g. `Sum(x.Total)` => `group.Sum(x => x.Total)`
    /// </summary>
    private async Task<List<QueryRangeVariable>> ConvertAggregationsAsync(CsQueryBuilder builder, VBSyntax.QueryClauseSyntax clause, SeparatedSyntaxList<VBSyntax.AggregationRangeVariableSyntax> vbAggregations,
        ExpressionSyntax group, IReadOnlyList<QueryRangeVariable> groupElement)
    {
        var aggregations = new List<QueryRangeVariable>();
        foreach (var vbAggregation in vbAggregations) {
            var variable = DeclareVariable(vbAggregation);
            if (vbAggregation.Aggregation is VBSyntax.FunctionAggregationSyntax function) {
                var value = await ConvertAggregationFunctionAsync(builder, function, group, groupElement);
                builder.Define(variable, value, CanInline(variable, vbAggregation, value), IsElementUsedNext(clause));
            } else {
                variable.CsExpression = group;
            }
            aggregations.Add(variable);
        }
        return aggregations;
    }

    private async Task<ExpressionSyntax> ConvertAggregationFunctionAsync(CsQueryBuilder builder, VBSyntax.FunctionAggregationSyntax function, ExpressionSyntax group, IReadOnlyList<QueryRangeVariable> groupElement)
    {
        var methodName = _semanticModel.GetSymbolInfo(function).Symbol?.Name ?? function.FunctionName.ValueText;
        var arguments = function.Argument == null
            ? Array.Empty<ArgumentSyntax>()
            : new[] { SyntaxFactory.Argument(await builder.CreateElementLambdaAsync(groupElement, () => ConvertAsync(function.Argument))) };
        var method = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, CsQueryBuilder.ParenthesizeIfNeeded(group), ValidSyntaxFactory.IdentifierName(methodName));
        return SyntaxFactory.InvocationExpression(method, SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));
    }

    /// <summary>
    /// VB implicitly calls AsEnumerable or Cast where needed to make a source queryable, and converts each element when the range variable's type is given.
    /// </summary>
    private async Task<(TypeSyntax Type, ExpressionSyntax Expression)> ConvertCollectionAsync(VBSyntax.CollectionRangeVariableSyntax vbVariable)
    {
        var expression = await ConvertAsync(vbVariable.Expression);
        var sourceElementType = _semanticModel.GetTypeInfo(vbVariable.Expression).Type?.GetEnumerableElementTypeOrDefault();
        TypeSyntax castType = null;
        var parentOperation = _semanticModel.GetOperation(vbVariable.Expression)?.Parent;
        while (parentOperation is IConversionOperation { IsImplicit: true }) parentOperation = parentOperation.Parent;
        if (parentOperation is IInvocationOperation { IsImplicit: true } io && io.TargetMethod.MethodKind == MethodKind.ReducedExtension) {
            if (io.TargetMethod.Name == nameof(Enumerable.AsEnumerable)) {
                expression = SyntaxFactory.InvocationExpression(ValidSyntaxFactory.MemberAccess(expression, io.TargetMethod.Name), SyntaxFactory.ArgumentList());
            } else if (io.TargetMethod.Name == nameof(Enumerable.Cast)) {
                sourceElementType = io.TargetMethod.TypeArguments.Single();
                castType = _commonConversions.GetTypeSyntax(sourceElementType);
            }
        }

        var rangeVariableType = vbVariable.AsClause == null ? null : _semanticModel.GetTypeInfo(vbVariable.AsClause.Type).Type;
        if (rangeVariableType == null || sourceElementType == null || SymbolEqualityComparer.Default.Equals(sourceElementType, rangeVariableType)) {
            return (castType, expression);
        }

        var conversion = VBasic.VisualBasicExtensions.ClassifyConversion(_semanticModel.Compilation, sourceElementType, rangeVariableType);
        if (conversion.IsReference && !conversion.IsUserDefined && rangeVariableType.SpecialType != SpecialType.System_String) {
            return (_commonConversions.GetTypeSyntax(rangeVariableType), expression);
        }

        // e.g. a numeric conversion, which C#'s Cast<T> would throw for
        var parameter = CommonConversions.CsEscapedIdentifier(rangeVariableType.Name == vbVariable.Identifier.Identifier.ValueText ? "x" : vbVariable.Identifier.Identifier.ValueText);
        var converted = _commonConversions.TypeConversionAnalyzer.AddExplicitConversion(vbVariable.Expression, SyntaxFactory.IdentifierName(parameter),
            defaultToCast: true, forceSourceType: sourceElementType, forceTargetType: rangeVariableType);
        var source = castType == null ? expression : SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, CsQueryBuilder.ParenthesizeIfNeeded(expression),
                SyntaxFactory.GenericName(ValidSyntaxFactory.IdentifierName(nameof(Enumerable.Cast)).Identifier, SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(castType)))),
            SyntaxFactory.ArgumentList());
        var select = SyntaxFactory.InvocationExpression(ValidSyntaxFactory.MemberAccess(CsQueryBuilder.ParenthesizeIfNeeded(source), nameof(Enumerable.Select)),
            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(SyntaxFactory.SimpleLambdaExpression(SyntaxFactory.Parameter(parameter), converted)))));
        return (null, select);
    }

    /// <summary>
    /// Substituting an expression for each reference must not change what it refers to, or how many times it's evaluated (with the exception of cheap expressions)
    /// </summary>
    /// <remarks>
    /// When it's not referenced, it can only be evaluated as part of the element the next time the element is constructed (if it survives that long).
    /// After a method boundary, the variable refers to a member of the constructed element, so later references don't evaluate it again.
    /// When it's referenced once from the final select (and not from a nested lambda/query), that's the only place it will be evaluated.
    /// </remarks>
    private bool CanInline(QueryRangeVariable variable, VisualBasicSyntaxNode declaration, ExpressionSyntax value)
    {
        if (IsShadowedLater(value, declaration)) return false;
        if (variable.Symbol == null || CsQueryBuilder.IsCheapToRepeat(value)) return true;
        var boundary = _vbQuery.Clauses.FirstOrDefault(c => c.SpanStart > declaration.Span.End && IsMethodBoundary(c));
        var searchEnd = boundary?.SpanStart ?? _vbQuery.Span.End;
        var references = _vbQuery.DescendantNodes(TextSpan.FromBounds(declaration.Span.End, searchEnd)).OfType<VBSyntax.IdentifierNameSyntax>()
            .Where(id => id.SpanStart >= declaration.Span.End && id.Span.End <= searchEnd && SymbolEqualityComparer.Default.Equals(_semanticModel.GetSymbolInfo(id).Symbol, variable.Symbol))
            .Take(2).ToList();
        return references.Count switch {
            0 => true,
            1 => boundary == null && _vbQuery.Clauses.Last() is VBSyntax.SelectClauseSyntax finalSelect && finalSelect.Span.Contains(references[0].Span) &&
                 !references[0].Ancestors().TakeWhile(a => a != finalSelect).Any(a => a is VBSyntax.LambdaExpressionSyntax or VBSyntax.QueryExpressionSyntax),
            _ => false
        };
    }

    /// <summary>
    /// A C# range variable that VB has dropped from scope (e.g. `x` after `Select y = x`) can still be referenced by a substituted expression,
    /// but VB code may reuse its name for a lambda parameter, e.g. `Where ys.Any(Function(x) x > y)`, which would capture the reference.
    /// Other names declared later are C# range variables, which are renamed to avoid clashes anyway.
    /// </summary>
    private bool IsShadowedLater(ExpressionSyntax value, VisualBasicSyntaxNode declaration)
    {
        var referencedNames = new HashSet<string>(value.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
            .Where(id => id.Parent is not MemberAccessExpressionSyntax { Name: var name } || name != id)
            .Select(id => id.Identifier.ValueText), StringComparer.OrdinalIgnoreCase);
        return _vbQuery.DescendantNodes().OfType<VBSyntax.ParameterSyntax>()
            .Any(p => p.SpanStart >= declaration.Span.End && referencedNames.Contains(p.Identifier.Identifier.ValueText));
    }

    private IEnumerable<string> GetNamesDeclaredWithinQuery(int startPosition = 0) =>
        _vbQuery.DescendantNodes().Where(n => n.SpanStart >= startPosition).Select(n => n switch {
            VBSyntax.ModifiedIdentifierSyntax x => x.Identifier.ValueText,
            VBSyntax.CollectionRangeVariableSyntax x => x.Identifier.Identifier.ValueText,
            VBSyntax.ExpressionRangeVariableSyntax x => VBasic.VisualBasicExtensions.GetDeclaredSymbol(_semanticModel, x)?.Name,
            VBSyntax.AggregationRangeVariableSyntax x => VBasic.VisualBasicExtensions.GetDeclaredSymbol(_semanticModel, x)?.Name,
            _ => null
        }).Where(n => n != null);

    /// <summary>
    /// When the element will be used straight after this clause, values inlined into it stay in the same order relative to other clauses
    /// </summary>
    private bool IsElementUsedNext(VBSyntax.QueryClauseSyntax clause)
    {
        var clauses = _vbQuery.Clauses;
        var index = clauses.IndexOf(clause);
        return index < 0 || index == clauses.Count - 1 || IsMethodBoundary(clauses[index + 1]);
    }

    private QueryRangeVariable DeclareVariable(VBSyntax.CollectionRangeVariableSyntax vbDeclaration) => DeclareVariable(VBasic.VisualBasicExtensions.GetDeclaredSymbol(_semanticModel, vbDeclaration));
    private QueryRangeVariable DeclareVariable(VBSyntax.ExpressionRangeVariableSyntax vbDeclaration) => DeclareVariable(VBasic.VisualBasicExtensions.GetDeclaredSymbol(_semanticModel, vbDeclaration));
    private QueryRangeVariable DeclareVariable(VBSyntax.AggregationRangeVariableSyntax vbDeclaration) => DeclareVariable(VBasic.VisualBasicExtensions.GetDeclaredSymbol(_semanticModel, vbDeclaration));

    private QueryRangeVariable DeclareVariable(IRangeVariableSymbol symbol)
    {
        var variable = new QueryRangeVariable(symbol, symbol?.Name ?? "x");
        _replacements.Register(variable);
        _declaredVariables.Add(variable);
        return variable;
    }

    private string GetName(VBSyntax.ExpressionRangeVariableSyntax vbVariable) =>
        VBasic.VisualBasicExtensions.GetDeclaredSymbol(_semanticModel, vbVariable)?.Name ?? vbVariable.Expression.ExtractAnonymousTypeMemberName()?.Text ?? "x";

    private async Task<T> WithBuilderAsync<T>(Func<CsQueryBuilder, Task<T>> useBuilder)
    {
        var enclosing = _activeBuilders.Count > 0 ? _activeBuilders.Peek() : null;
        var namesDeclaredWithinQuery = new HashSet<string>(GetNamesDeclaredWithinQuery(), StringComparer.OrdinalIgnoreCase);
        bool IsNameFreeOutsideQuery(string name) =>
            !namesDeclaredWithinQuery.Contains(name) && _semanticModel.LookupSymbols(_vbQuery.SpanStart, name: name).IsEmpty &&
            NameGenerator.CS.IsNameFreeInScope(_semanticModel, _generatedNames, _vbQuery, name);
        var builder = new CsQueryBuilder(IsNameFreeOutsideQuery, enclosing);
        _activeBuilders.Push(builder);
        try {
            return await useBuilder(builder);
        } finally {
            _activeBuilders.Pop();
        }
    }

    private Task<ExpressionSyntax> ConvertAsync(VBSyntax.ExpressionSyntax expression) => expression.AcceptAsync<ExpressionSyntax>(_visitor);

    /// <summary>
    /// For positions which never need parentheses, but where the simplifier doesn't remove them
    /// </summary>
    private async Task<ExpressionSyntax> ConvertWithoutParenthesesAsync(VBSyntax.ExpressionSyntax expression)
    {
        var converted = await ConvertAsync(expression);
        return converted is ParenthesizedExpressionSyntax p ? p.Expression : converted;
    }
}
