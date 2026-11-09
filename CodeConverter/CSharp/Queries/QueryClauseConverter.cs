using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
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
    private readonly List<QueryRangeVariable> _registeredVariables = new();

    /// <param name="activeBuilders">Builders for the queries being converted, the innermost of which encloses this query</param>
    public QueryClauseConverter(VBSyntax.QueryExpressionSyntax vbQuery, SemanticModel semanticModel, CommonConversions commonConversions,
        CommentConvertingVisitorWrapper visitor, RangeVariableReplacements replacements, HashSet<string> generatedNames, Stack<CsQueryBuilder> activeBuilders)
    {
        _vbQuery = vbQuery;
        _semanticModel = semanticModel;
        _commonConversions = commonConversions;
        _visitor = visitor;
        _replacements = replacements;
        _generatedNames = generatedNames;
        _activeBuilders = activeBuilders;
    }

    public async Task<ExpressionSyntax> ConvertAsync()
    {
        try {
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
                if (finalSelect == null) return builder.Materialize();
                builder.EnsureQueryOpen();
                return builder.BuildQuery(await ConvertFinalSelectAsync(finalSelect));
            });
        } finally {
            foreach (var variable in _registeredVariables) _replacements.Unregister(variable);
        }
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

    private static bool IsConvertedToMethodCall(VBSyntax.QueryClauseSyntax clause) =>
        clause is VBSyntax.DistinctClauseSyntax or VBSyntax.PartitionClauseSyntax or VBSyntax.PartitionWhileClauseSyntax;

    private async Task ConvertFromAsync(CsQueryBuilder builder, VBSyntax.FromClauseSyntax from)
    {
        foreach (var vbVariable in from.Variables) {
            var (type, collection) = await ConvertCollectionAsync(vbVariable);
            builder.AddFrom(RegisterVariable(vbVariable), type, collection);
        }
    }

    private async Task ConvertLetAsync(CsQueryBuilder builder, VBSyntax.LetClauseSyntax let)
    {
        foreach (var vbVariable in let.Variables) {
            var value = await ConvertAsync(vbVariable.Expression);
            var variable = RegisterVariable(vbVariable);
            builder.DefineVariable(variable, value, CanSubstitute(variable, vbVariable, value), IsElementMaterializedNext(let));
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
    /// C# can't drop the old range variables without a continuation, but doesn't need to since the element is built from VB's scope
    /// </summary>
    private async Task ConvertSelectAsync(CsQueryBuilder builder, VBSyntax.SelectClauseSyntax select)
    {
        var values = await select.Variables.SelectAsync(v => ConvertAsync(v.Expression));
        var variables = select.Variables.Select(RegisterVariable).ToList();
        for (int i = 0; i < variables.Count; i++) {
            builder.DefineVariable(variables[i], values[i], CanSubstitute(variables[i], select.Variables[i], values[i]), IsElementMaterializedNext(select));
        }
        builder.SetScope(variables);
    }

    private async Task<SelectClauseSyntax> ConvertFinalSelectAsync(VBSyntax.SelectClauseSyntax select)
    {
        var values = await select.Variables.SelectAsync(v => ConvertWithoutParenthesesAsync(v.Expression));
        if (values.Length == 1) return SyntaxFactory.SelectClause(values[0]);
        var names = select.Variables.Select(GetVariableName);
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
    /// Makes explicit what VB does implicitly to query a source: AsEnumerable or Cast, and converting to the range variable's declared type
    /// </summary>
    private async Task<(TypeSyntax Type, ExpressionSyntax Expression)> ConvertCollectionAsync(VBSyntax.CollectionRangeVariableSyntax vbVariable)
    {
        var expression = await ConvertAsync(vbVariable.Expression);
        var sourceElementType = _semanticModel.GetTypeInfo(vbVariable.Expression).Type?.GetEnumerableElementTypeOrDefault();
        TypeSyntax castType = null;
        if (GetImplicitQueryableCall(vbVariable.Expression) is { } implicitCall) {
            if (implicitCall.TargetMethod.Name == nameof(Enumerable.AsEnumerable)) {
                expression = SyntaxFactory.InvocationExpression(ValidSyntaxFactory.MemberAccess(expression, nameof(Enumerable.AsEnumerable)), SyntaxFactory.ArgumentList());
            } else if (implicitCall.TargetMethod.Name == nameof(Enumerable.Cast)) {
                sourceElementType = implicitCall.TargetMethod.TypeArguments.Single();
                castType = _commonConversions.GetTypeSyntax(sourceElementType);
            }
        }

        var declaredType = vbVariable.AsClause == null ? null : _semanticModel.GetTypeInfo(vbVariable.AsClause.Type).Type;
        if (declaredType == null || sourceElementType == null || SymbolEqualityComparer.Default.Equals(sourceElementType, declaredType)) {
            return (castType, expression);
        }
        if (IsSameAsCast(sourceElementType, declaredType)) {
            return (_commonConversions.GetTypeSyntax(declaredType), expression);
        }
        var source = castType == null ? expression : CallCast(expression, castType);
        return (null, SelectConverted(vbVariable, source, sourceElementType, declaredType));
    }

    private IInvocationOperation GetImplicitQueryableCall(VBSyntax.ExpressionSyntax collection)
    {
        var parent = _semanticModel.GetOperation(collection)?.Parent;
        while (parent is IConversionOperation { IsImplicit: true }) parent = parent.Parent;
        return parent is IInvocationOperation { IsImplicit: true, TargetMethod.MethodKind: MethodKind.ReducedExtension } invocation ? invocation : null;
    }

    /// <summary>
    /// VB's CType runs conversions like Object to String or Integer to Long, but C#'s Cast&lt;T&gt; would throw
    /// </summary>
    private bool IsSameAsCast(ITypeSymbol sourceType, ITypeSymbol targetType)
    {
        var conversion = VBasic.VisualBasicExtensions.ClassifyConversion(_semanticModel.Compilation, sourceType, targetType);
        return conversion.IsReference && !conversion.IsUserDefined && targetType.SpecialType != SpecialType.System_String;
    }

    private static ExpressionSyntax CallCast(ExpressionSyntax source, TypeSyntax type)
    {
        var cast = SyntaxFactory.GenericName(ValidSyntaxFactory.IdentifierName(nameof(Enumerable.Cast)).Identifier, SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(type)));
        return SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, CsQueryBuilder.ParenthesizeIfNeeded(source), cast), SyntaxFactory.ArgumentList());
    }

    private ExpressionSyntax SelectConverted(VBSyntax.CollectionRangeVariableSyntax vbVariable, ExpressionSyntax source, ITypeSymbol sourceType, ITypeSymbol targetType)
    {
        var variableName = vbVariable.Identifier.Identifier.ValueText;
        var parameter = CommonConversions.CsEscapedIdentifier(targetType.Name == variableName ? "x" : variableName);
        var converted = _commonConversions.TypeConversionAnalyzer.AddExplicitConversion(vbVariable.Expression, SyntaxFactory.IdentifierName(parameter),
            defaultToCast: true, forceSourceType: sourceType, forceTargetType: targetType);
        var lambda = SyntaxFactory.SimpleLambdaExpression(SyntaxFactory.Parameter(parameter), converted);
        return SyntaxFactory.InvocationExpression(ValidSyntaxFactory.MemberAccess(CsQueryBuilder.ParenthesizeIfNeeded(source), nameof(Enumerable.Select)),
            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(lambda))));
    }

    private QueryRangeVariable RegisterVariable(VBSyntax.CollectionRangeVariableSyntax vbDeclaration) => RegisterVariable(VBasic.VisualBasicExtensions.GetDeclaredSymbol(_semanticModel, vbDeclaration));
    private QueryRangeVariable RegisterVariable(VBSyntax.ExpressionRangeVariableSyntax vbDeclaration) => RegisterVariable(VBasic.VisualBasicExtensions.GetDeclaredSymbol(_semanticModel, vbDeclaration));
    private QueryRangeVariable RegisterVariable(VBSyntax.AggregationRangeVariableSyntax vbDeclaration) => RegisterVariable(VBasic.VisualBasicExtensions.GetDeclaredSymbol(_semanticModel, vbDeclaration));

    private QueryRangeVariable RegisterVariable(IRangeVariableSymbol symbol)
    {
        var variable = new QueryRangeVariable(symbol, symbol?.Name ?? "x");
        _replacements.Register(variable);
        _registeredVariables.Add(variable);
        return variable;
    }

    private string GetVariableName(VBSyntax.ExpressionRangeVariableSyntax vbVariable) =>
        VBasic.VisualBasicExtensions.GetDeclaredSymbol(_semanticModel, vbVariable)?.Name ?? vbVariable.Expression.ExtractAnonymousTypeMemberName()?.Text ?? "x";

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
        var namesDeclaredWithinQuery = new HashSet<string>(GetNamesDeclaredWithinQuery(), StringComparer.OrdinalIgnoreCase);
        return name => !namesDeclaredWithinQuery.Contains(name) && _semanticModel.LookupSymbols(_vbQuery.SpanStart, name: name).IsEmpty &&
                       NameGenerator.CS.IsNameFreeInScope(_semanticModel, _generatedNames, _vbQuery, name);
    }

    private IEnumerable<string> GetNamesDeclaredWithinQuery() =>
        _vbQuery.DescendantNodes().Select(n => n switch {
            VBSyntax.ModifiedIdentifierSyntax x => x.Identifier.ValueText,
            VBSyntax.CollectionRangeVariableSyntax x => x.Identifier.Identifier.ValueText,
            VBSyntax.ExpressionRangeVariableSyntax x => VBasic.VisualBasicExtensions.GetDeclaredSymbol(_semanticModel, x)?.Name,
            VBSyntax.AggregationRangeVariableSyntax x => VBasic.VisualBasicExtensions.GetDeclaredSymbol(_semanticModel, x)?.Name,
            _ => null
        }).Where(n => n != null);

    private Task<ExpressionSyntax> ConvertAsync(VBSyntax.ExpressionSyntax expression) => expression.AcceptAsync<ExpressionSyntax>(_visitor);

    /// <summary>
    /// For positions that never need parentheses, but where the simplifier doesn't remove them
    /// </summary>
    private async Task<ExpressionSyntax> ConvertWithoutParenthesesAsync(VBSyntax.ExpressionSyntax expression)
    {
        var converted = await ConvertAsync(expression);
        return converted is ParenthesizedExpressionSyntax p ? p.Expression : converted;
    }
}
