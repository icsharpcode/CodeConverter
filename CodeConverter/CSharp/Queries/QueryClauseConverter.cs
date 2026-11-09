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
    private readonly VBSyntax.QueryExpressionSyntax _vbQuery;
    private readonly SemanticModel _semanticModel;
    private readonly CommonConversions _commonConversions;
    private readonly CommentConvertingVisitorWrapper _visitor;
    private readonly RangeVariableReplacements _replacements;
    private readonly HashSet<string> _generatedNames;
    private readonly List<QueryRangeVariable> _declaredVariables = new();

    public QueryClauseConverter(VBSyntax.QueryExpressionSyntax vbQuery, SemanticModel semanticModel, CommonConversions commonConversions,
        CommentConvertingVisitorWrapper visitor, RangeVariableReplacements replacements, HashSet<string> generatedNames)
    {
        _vbQuery = vbQuery;
        _semanticModel = semanticModel;
        _commonConversions = commonConversions;
        _visitor = visitor;
        _replacements = replacements;
        _generatedNames = generatedNames;
    }

    public static bool IsSupported(VBSyntax.QueryExpressionSyntax query) =>
        query.Clauses.First() is VBSyntax.FromClauseSyntax &&
        query.Clauses.All(c => c is VBSyntax.FromClauseSyntax or VBSyntax.LetClauseSyntax or VBSyntax.WhereClauseSyntax or VBSyntax.OrderByClauseSyntax
            or VBSyntax.SelectClauseSyntax or VBSyntax.DistinctClauseSyntax or VBSyntax.PartitionClauseSyntax or VBSyntax.PartitionWhileClauseSyntax
            or VBSyntax.JoinClauseSyntax);

    public async Task<ExpressionSyntax> ConvertAsync()
    {
        try {
            var builder = CreateBuilder(_ => true);
            var clauses = _vbQuery.Clauses;
            var finalSelect = clauses.Last() as VBSyntax.SelectClauseSyntax;
            foreach (var clause in finalSelect == null ? clauses : clauses.Take(clauses.Count - 1)) {
                await ConvertClauseAsync(builder, clause);
            }
            if (finalSelect == null) return builder.Materialize();
            builder.EnsureQuery();
            return builder.Build(await ConvertFinalSelectAsync(finalSelect));
        } finally {
            foreach (var variable in _declaredVariables) _replacements.Unregister(variable);
        }
    }

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
            builder.Define(variable, value, CanInline(variable, vbVariable), IsElementUsedNext(let));
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
            builder.Define(variables[i], values[i], CanInline(variables[i], select.Variables[i]), IsElementUsedNext(select));
        }
        builder.SetScope(variables);
    }

    private async Task<SelectClauseSyntax> ConvertFinalSelectAsync(VBSyntax.SelectClauseSyntax select)
    {
        var values = await select.Variables.SelectAsync(v => ConvertAsync(v.Expression));
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
        var joins = FlattenJoins(join).ToList();
        if (joins.Skip(1).Any(j => j is not VBSyntax.SimpleJoinClauseSyntax)) {
            throw new NotImplementedException("Group Join nested within Join not implemented");
        }
        var unplacedConditions = joins.SelectMany(j => j.JoinConditions).ToList();
        var joinedSymbols = joins.Select(j => (ISymbol)VBasic.VisualBasicExtensions.GetDeclaredSymbol(_semanticModel, j.JoinedVariables.Single())).ToList();
        var inScope = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        foreach (var vbJoin in joins) {
            var (variable, identifier, collection) = await DeclareJoinedVariableAsync(builder, vbJoin);
            inScope.Add(variable.Symbol);
            var conditions = unplacedConditions.Where(c => {
                var referenced = GetReferencedSymbols(c, joinedSymbols);
                return referenced.Contains(variable.Symbol) && referenced.All(inScope.Contains);
            }).ToList();
            unplacedConditions.RemoveAll(conditions.Contains);
            var (outerKey, innerKey) = await ConvertJoinConditionsAsync(conditions, variable.Symbol);
            if (joins.Count > 1) (outerKey, innerKey) = (outerKey.WithoutSourceMapping(), innerKey.WithoutSourceMapping()); // Conditions have been reordered
            builder.AddClause(SyntaxFactory.JoinClause(null, identifier, collection, outerKey, innerKey, null));
            builder.AddToScope(variable);
        }
        if (unplacedConditions.Any()) throw new NotImplementedException("Join condition that doesn't reference the joined variable not implemented");
    }

    private async Task ConvertGroupJoinAsync(CsQueryBuilder builder, VBSyntax.GroupJoinClauseSyntax groupJoin)
    {
        if (groupJoin.AdditionalJoins.Any()) throw new NotImplementedException("Join nested within Group Join not implemented");
        var (joinedVariable, identifier, collection) = await DeclareJoinedVariableAsync(builder, groupJoin);
        var (outerKey, innerKey) = await ConvertJoinConditionsAsync(groupJoin.JoinConditions, joinedVariable.Symbol);
        builder.RemoveRangeVariable(identifier);

        var groupVariable = GetGroupVariable(groupJoin.AggregationVariables);
        var into = builder.DeclareRangeVariable(groupVariable);
        builder.AddClause(SyntaxFactory.JoinClause(null, identifier, collection, outerKey, innerKey, SyntaxFactory.JoinIntoClause(into)));
        foreach (var aggregation in await ConvertAggregationsAsync(builder, groupJoin, groupJoin.AggregationVariables, groupVariable.CsExpression, new[] { joinedVariable })) {
            builder.AddToScope(aggregation);
        }
    }

    private async Task<(QueryRangeVariable Variable, SyntaxToken Identifier, ExpressionSyntax Collection)> DeclareJoinedVariableAsync(CsQueryBuilder builder, VBSyntax.JoinClauseSyntax join)
    {
        var vbVariable = join.JoinedVariables.Single();
        var (_, collection) = await ConvertCollectionAsync(vbVariable);
        var variable = DeclareVariable(vbVariable);
        return (variable, builder.DeclareRangeVariable(variable), collection);
    }

    /// <summary>
    /// C# requires the outer key on the left of equals, and multiple conditions to be combined into a single key
    /// </summary>
    private async Task<(ExpressionSyntax Outer, ExpressionSyntax Inner)> ConvertJoinConditionsAsync(IReadOnlyCollection<VBSyntax.JoinConditionSyntax> conditions, IRangeVariableSymbol joinedSymbol)
    {
        if (!conditions.Any()) throw new NotImplementedException("Join without a condition referencing the joined variable not implemented");
        var keys = await conditions.SelectAsync(async c => {
            var (vbOuter, vbInner) = References(c.Left, joinedSymbol) ? (c.Right, c.Left) : (c.Left, c.Right);
            if (References(vbOuter, joinedSymbol)) throw new NotImplementedException("Join condition referencing the joined variable on both sides not implemented");
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

    private bool References(SyntaxNode node, ISymbol symbol) => GetReferencedSymbols(node, new[] { symbol }).Any();

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
            : new QueryRangeVariable(null, "Group");

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
                builder.Define(variable, value, CanInline(variable, vbAggregation), IsElementUsedNext(clause));
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

    private async Task<(TypeSyntax Type, ExpressionSyntax Expression)> ConvertCollectionAsync(VBSyntax.CollectionRangeVariableSyntax vbVariable)
    {
        var expression = await ConvertAsync(vbVariable.Expression);
        var parentOperation = _semanticModel.GetOperation(vbVariable.Expression)?.Parent;
        if (parentOperation is IInvocationOperation { IsImplicit: true } io && io.TargetMethod.MethodKind == MethodKind.ReducedExtension &&
            io.TargetMethod.Name == nameof(Enumerable.AsEnumerable)) {
            expression = SyntaxFactory.InvocationExpression(ValidSyntaxFactory.MemberAccess(expression, io.TargetMethod.Name), SyntaxFactory.ArgumentList());
        }
        return (null, expression);
    }

    /// <summary>
    /// Substituting an expression for each reference must not change how many times it's evaluated (with the exception of cheap expressions)
    /// </summary>
    /// <remarks>
    /// When it's not referenced, it can only be evaluated as part of the element the next time the element is constructed (if it survives that long).
    /// After a method boundary, the variable refers to a member of the constructed element, so later references don't evaluate it again.
    /// When it's referenced once from the final select (and not from a nested lambda/query), that's the only place it will be evaluated.
    /// </remarks>
    private bool CanInline(QueryRangeVariable variable, VisualBasicSyntaxNode declaration)
    {
        if (variable.Symbol == null) return true;
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

    private CsQueryBuilder CreateBuilder(Func<string, bool> isNameFreeInEnclosingQuery) =>
        new(name => isNameFreeInEnclosingQuery(name) && NameGenerator.CS.IsNameFreeInScope(_semanticModel, _generatedNames, _vbQuery, name));

    private Task<ExpressionSyntax> ConvertAsync(VBSyntax.ExpressionSyntax expression) => expression.AcceptAsync<ExpressionSyntax>(_visitor);
}
