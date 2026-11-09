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
            or VBSyntax.SelectClauseSyntax or VBSyntax.DistinctClauseSyntax or VBSyntax.PartitionClauseSyntax or VBSyntax.PartitionWhileClauseSyntax);

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
            builder.Define(variable, value, CanInline(variable, vbVariable));
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
            builder.Define(variables[i], values[i], CanInline(variables[i], select.Variables[i]));
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
