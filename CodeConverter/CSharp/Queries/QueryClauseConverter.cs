using Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using VBSyntaxKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;

namespace ICSharpCode.CodeConverter.CSharp.Queries;

/// <summary>
/// Converts one VB query expression clause by clause into a <see cref="CsQueryBuilder"/>. See README.md.
/// </summary>
internal partial class QueryClauseConverter
{
    private readonly VBSyntax.QueryExpressionSyntax _vbQuery;
    private readonly SemanticModel _semanticModel;
    private readonly CommonConversions _commonConversions;
    private readonly CommentConvertingVisitorWrapper _visitor;
    private readonly RangeVariableReplacements _replacements;
    private readonly HashSet<string> _generatedNames;
    private readonly Stack<CsQueryBuilder> _activeBuilders;
    private readonly QuerySourceConverter _sources;
    private readonly FlatJoinPlanner _joinPlanner;
    private SubstitutionRules _substitution;
    private readonly ExpressionTreeDetector _expressionTrees;
    private bool _isWithinExpressionTreeOutsideQuery;

    /// <param name="activeBuilders">Builders for the queries being converted, the innermost of which encloses this query</param>
    /// <param name="isWithinExpressionTreeOutsideQuery">For the parts of the query that run as ordinary code even when its lambdas are an expression tree</param>
    public QueryClauseConverter(VBSyntax.QueryExpressionSyntax vbQuery, SemanticModel semanticModel, CommonConversions commonConversions,
        CommentConvertingVisitorWrapper visitor, RangeVariableReplacements replacements, HashSet<string> generatedNames, Stack<CsQueryBuilder> activeBuilders,
        bool isWithinExpressionTreeOutsideQuery)
    {
        _isWithinExpressionTreeOutsideQuery = isWithinExpressionTreeOutsideQuery;
        _vbQuery = vbQuery;
        _semanticModel = semanticModel;
        _commonConversions = commonConversions;
        _visitor = visitor;
        _replacements = replacements;
        _generatedNames = generatedNames;
        _activeBuilders = activeBuilders;
        _sources = new QuerySourceConverter(semanticModel, commonConversions, ConvertSourceAsync);
        _joinPlanner = new FlatJoinPlanner(semanticModel);
        _substitution = SubstitutionRules.ForQuery(vbQuery, semanticModel);
        _expressionTrees = new ExpressionTreeDetector(semanticModel, commonConversions.KnownTypes.System_Linq_Expressions_Expression_T);
    }

    public async Task<ExpressionSyntax> ConvertAsync()
    {
        var clauses = _vbQuery.Clauses;
        if (clauses.First() is VBSyntax.AggregateClauseSyntax aggregate) {
            if (clauses.Count > 1) throw new NotSupportedException("Query operators after an initial Aggregate clause aren't valid VB");
            return await ConvertAggregateQueryAsync(aggregate);
        }
        return await WithBuilderAsync(async builder => {
            var finalSelect = clauses.Last() as VBSyntax.SelectClauseSyntax;
            foreach (var clause in finalSelect == null ? clauses : clauses.Take(clauses.Count - 1)) {
                await ConvertClauseAsync(builder, clause);
            }
            if (finalSelect == null) return builder.MaterializeResult();
            builder.EnsureQueryOpen();
            return builder.BuildQuery(await ConvertFinalSelectAsync(finalSelect));
        });
    }

    private Task ConvertClauseAsync(CsQueryBuilder builder, VBSyntax.QueryClauseSyntax clause)
    {
        if (!IsConvertedToMethodCall(clause)) builder.EnsureQueryOpen();
        return clause switch {
            VBSyntax.FromClauseSyntax x => ConvertFromAsync(builder, x),
            VBSyntax.LetClauseSyntax x => ConvertLetAsync(builder, x),
            VBSyntax.WhereClauseSyntax x => ConvertWhereAsync(builder, x),
            VBSyntax.OrderByClauseSyntax x => ConvertOrderByAsync(builder, x),
            VBSyntax.SelectClauseSyntax x => ConvertSelectAsync(builder, x),
            VBSyntax.DistinctClauseSyntax => ConvertDistinctAsync(builder),
            VBSyntax.PartitionClauseSyntax x => ConvertPartitionAsync(builder, x),
            VBSyntax.PartitionWhileClauseSyntax x => ConvertPartitionWhileAsync(builder, x),
            VBSyntax.SimpleJoinClauseSyntax x => ConvertJoinAsync(builder, x),
            VBSyntax.GroupJoinClauseSyntax x => ConvertGroupJoinAsync(builder, x),
            VBSyntax.GroupByClauseSyntax x => ConvertGroupByAsync(builder, x),
            VBSyntax.AggregateClauseSyntax x => ConvertAggregateClauseAsync(builder, x),
            _ => throw new NotImplementedException($"Conversion for query clause with kind '{clause.Kind()}' not implemented")
        };
    }

    internal static bool IsConvertedToMethodCall(VBSyntax.QueryClauseSyntax clause) =>
        clause is VBSyntax.DistinctClauseSyntax or VBSyntax.PartitionClauseSyntax or VBSyntax.PartitionWhileClauseSyntax;

    private async Task ConvertFromAsync(CsQueryBuilder builder, VBSyntax.FromClauseSyntax from)
    {
        foreach (var vbVariable in from.Variables) {
            var (type, collection) = await _sources.ConvertAsync(vbVariable);
            builder.AddFrom(RegisterVariable(vbVariable), type, collection);
        }
    }

    private async Task ConvertLetAsync(CsQueryBuilder builder, VBSyntax.LetClauseSyntax let)
    {
        foreach (var vbVariable in let.Variables) {
            var value = await ConvertValueAsync(vbVariable);
            var variable = RegisterVariable(vbVariable);
            builder.DefineVariable(variable, value, _substitution.CanSubstitute(variable, vbVariable, value), _substitution.IsElementMaterializedNext(let));
            builder.AddToScope(variable);
        }
    }

    private async Task ConvertWhereAsync(CsQueryBuilder builder, VBSyntax.WhereClauseSyntax where) =>
        builder.AddClause(SyntaxFactory.WhereClause(await ConvertConditionAsync(where.Condition)));

    private async Task ConvertOrderByAsync(CsQueryBuilder builder, VBSyntax.OrderByClauseSyntax orderBy)
    {
        var orderings = await orderBy.Orderings.SelectAsync(o => o.AcceptAsync<OrderingSyntax>(_visitor));
        builder.AddClause(SyntaxFactory.OrderByClause(SyntaxFactory.SeparatedList(orderings)));
    }

    /// <summary>
    /// C# can't drop the old range variables without a continuation, but doesn't need to since the element is built from VB's scope
    /// </summary>
    private async Task ConvertSelectAsync(CsQueryBuilder builder, VBSyntax.SelectClauseSyntax select)
    {
        var values = await select.Variables.SelectAsync(v => ConvertValueAsync(v));
        var variables = select.Variables.Select(RegisterVariable).ToList();
        for (int i = 0; i < variables.Count; i++) {
            builder.DefineVariable(variables[i], values[i], _substitution.CanSubstitute(variables[i], select.Variables[i], values[i]), _substitution.IsElementMaterializedNext(select));
        }
        builder.SetScope(variables);
    }

    private async Task<SelectClauseSyntax> ConvertFinalSelectAsync(VBSyntax.SelectClauseSyntax select)
    {
        var values = await select.Variables.SelectAsync(v => ConvertValueAsync(v, withoutParentheses: true));
        if (values.Length == 1) return SyntaxFactory.SelectClause(values[0]);
        var names = select.Variables.Select(GetVariableName);
        return SyntaxFactory.SelectClause(CsQueryBuilder.CreateAnonymousObject(names.Zip(values, (n, v) => (n, v))));
    }

    private static Task ConvertDistinctAsync(CsQueryBuilder builder)
    {
        builder.ApplyMethod(nameof(Enumerable.Distinct));
        return Task.CompletedTask;
    }

    private async Task ConvertPartitionAsync(CsQueryBuilder builder, VBSyntax.PartitionClauseSyntax partition)
    {
        var methodName = partition.SkipOrTakeKeyword.IsKind(VBSyntaxKind.SkipKeyword) ? nameof(Enumerable.Skip) : nameof(Enumerable.Take);
        var count = await ConvertOutsideExpressionTreeAsync(partition.Count);
        builder.ApplyMethod(methodName, _commonConversions.TypeConversionAnalyzer.AddExplicitConversion(partition.Count, count,
            forceTargetType: _semanticModel.Compilation.GetSpecialType(SpecialType.System_Int32)));
    }

    private async Task ConvertPartitionWhileAsync(CsQueryBuilder builder, VBSyntax.PartitionWhileClauseSyntax partitionWhile)
    {
        var methodName = partitionWhile.SkipOrTakeKeyword.IsKind(VBSyntaxKind.SkipKeyword) ? nameof(Enumerable.SkipWhile) : nameof(Enumerable.TakeWhile);
        var predicate = await builder.CreateElementLambdaAsync(builder.Scope, () => ConvertConditionAsync(partitionWhile.Condition), isOutsideQuery: true);
        builder.ApplyMethod(methodName, predicate);
    }

    private QueryRangeVariable RegisterVariable(VBSyntax.CollectionRangeVariableSyntax vbDeclaration) => RegisterVariable(VBasic.VisualBasicExtensions.GetDeclaredSymbol(_semanticModel, vbDeclaration));
    private QueryRangeVariable RegisterVariable(VBSyntax.ExpressionRangeVariableSyntax vbDeclaration) => RegisterVariable(VBasic.VisualBasicExtensions.GetDeclaredSymbol(_semanticModel, vbDeclaration));
    private QueryRangeVariable RegisterVariable(VBSyntax.AggregationRangeVariableSyntax vbDeclaration) => RegisterVariable(VBasic.VisualBasicExtensions.GetDeclaredSymbol(_semanticModel, vbDeclaration));

    private QueryRangeVariable RegisterVariable(IRangeVariableSymbol symbol)
    {
        var variable = new QueryRangeVariable(symbol, GetName(symbol));
        _replacements.Register(variable);
        return variable;
    }

    private string GetVariableName(VBSyntax.ExpressionRangeVariableSyntax vbVariable) => GetName(VBasic.VisualBasicExtensions.GetDeclaredSymbol(_semanticModel, vbVariable));

    private static string GetName(IRangeVariableSymbol symbol) => symbol?.Name ?? CsQueryBuilder.GeneratedElementName;

    /// <summary>
    /// Nested queries (including subqueries generated for joins and Aggregate) use the enclosing builder to avoid name clashes
    /// </summary>
    private async Task<T> WithBuilderAsync<T>(Func<CsQueryBuilder, Task<T>> useBuilder)
    {
        var enclosing = _activeBuilders.Count > 0 ? _activeBuilders.Peek() : null;
        var builder = new CsQueryBuilder(CreateIsNameFreeOutsideQuery(), enclosing);
        _activeBuilders.Push(builder);
        try {
            return await useBuilder(builder);
        } finally {
            _activeBuilders.Pop();
        }
    }

    private Func<string, bool> CreateIsNameFreeOutsideQuery()
    {
        var namesDeclaredWithinQuery = new HashSet<string>(QueryDeclarations.Get(_vbQuery, _semanticModel).Select(d => d.Name), StringComparer.OrdinalIgnoreCase);
        return name => !namesDeclaredWithinQuery.Contains(name) && _semanticModel.LookupSymbols(_vbQuery.SpanStart, name: name).IsEmpty &&
                       NameGenerator.CS.IsNameFreeInScope(_semanticModel, _generatedNames, _vbQuery, name);
    }

    /// <summary>
    /// C# infers a range variable's type from its value, so make VB's declared type explicit where C# can't infer it
    /// </summary>
    private async Task<ExpressionSyntax> ConvertValueAsync(VBSyntax.ExpressionRangeVariableSyntax vbVariable, bool withoutParentheses = false)
    {
        var value = withoutParentheses ? await ConvertWithoutParenthesesAsync(vbVariable.Expression) : await ConvertAsync(vbVariable.Expression);
        if (_semanticModel.GetTypeInfo(vbVariable.Expression).ConvertedType is not { } type) return value;
        var delegateMethod = vbVariable.Expression switch {
            VBSyntax.LambdaExpressionSyntax lambda => _semanticModel.GetSymbolInfo(lambda).Symbol,
            VBSyntax.UnaryExpressionSyntax addressOf when addressOf.IsKind(VBasic.SyntaxKind.AddressOfExpression) => _semanticModel.GetSymbolInfo(addressOf.Operand).Symbol,
            _ => null
        };
        if (delegateMethod is IMethodSymbol method) {
            var delegateType = type.IsAnonymousType ? _commonConversions.GetFuncTypeSyntax(type, method) : _commonConversions.GetTypeSyntax(type);
            return SyntaxFactory.ObjectCreationExpression(delegateType, ExpressionSyntaxExtensions.CreateArgList(value.SkipIntoParens()), null)
                .WithAdditionalAnnotations(SubstitutionRules.DelegateCreationAnnotation);
        }
        if (vbVariable.NameEquals?.AsClause != null) value = _commonConversions.TypeConversionAnalyzer.AddExplicitConversion(vbVariable.Expression, value, defaultToCast: true);
        if (value.IsKind(SyntaxKind.NullLiteralExpression)) return SyntaxFactory.CastExpression(_commonConversions.GetTypeSyntax(type), value);
        return value;
    }

    private Task<ExpressionSyntax> ConvertAsync(VBSyntax.ExpressionSyntax expression) => expression.AcceptAsync<ExpressionSyntax>(_visitor);

    /// <summary>
    /// A provider is passed the first source (of the query, or of an Aggregate clause's own query) and a join's inner source as they are,
    /// whereas later sources are within lambdas
    /// </summary>
    private Task<ExpressionSyntax> ConvertSourceAsync(VBSyntax.ExpressionSyntax collection) =>
        collection.Parent is VBSyntax.CollectionRangeVariableSyntax variable &&
        (variable.Parent is VBSyntax.JoinClauseSyntax ||
         variable.Parent is VBSyntax.AggregateClauseSyntax aggregate && aggregate.Variables.First() == variable ||
         variable.Parent is VBSyntax.FromClauseSyntax from && from == _vbQuery.Clauses.First() && from.Variables.First() == variable)
            ? ConvertOutsideExpressionTreeAsync(collection)
            : ConvertAsync(collection);

    private async Task<ExpressionSyntax> ConvertOutsideExpressionTreeAsync(VBSyntax.ExpressionSyntax expression)
    {
        var isWithinExpressionTree = _visitor.IsWithinExpressionTree;
        _visitor.IsWithinExpressionTree = _isWithinExpressionTreeOutsideQuery;
        try {
            return await ConvertAsync(expression);
        } finally {
            _visitor.IsWithinExpressionTree = isWithinExpressionTree;
        }
    }

    /// <summary>
    /// VB treats a null Boolean? condition as false
    /// </summary>
    private async Task<ExpressionSyntax> ConvertConditionAsync(VBSyntax.ExpressionSyntax condition) =>
        _commonConversions.TypeConversionAnalyzer.AddExplicitConversion(condition, await ConvertAsync(condition), forceTargetType: _commonConversions.KnownTypes.Boolean);

    /// <summary>
    /// For positions that never need parentheses, but where the simplifier doesn't remove them
    /// </summary>
    private async Task<ExpressionSyntax> ConvertWithoutParenthesesAsync(VBSyntax.ExpressionSyntax expression)
    {
        var converted = await ConvertAsync(expression);
        return converted is ParenthesizedExpressionSyntax p ? p.Expression : converted;
    }
}
