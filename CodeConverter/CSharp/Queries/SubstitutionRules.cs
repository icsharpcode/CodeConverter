using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ICSharpCode.CodeConverter.CSharp.Queries;

/// <summary>
/// Decides whether a value can be substituted for each reference to its range variable, rather than declared with a let clause
/// </summary>
internal class SubstitutionRules
{
    private readonly VBSyntax.QueryExpressionSyntax _vbQuery;
    private readonly IReadOnlyList<VBSyntax.QueryClauseSyntax> _clauses;
    private readonly VBSyntax.SelectClauseSyntax _finalSelect;
    private readonly int _end;
    private readonly SemanticModel _semanticModel;
    private readonly Lazy<IReadOnlyList<QueryDeclarations.Declaration>> _declarations;

    /// <param name="clauses">The clauses of the (sub)query being built, which decide where values are evaluated</param>
    /// <param name="end">Where the clauses' elements are finally materialized</param>
    private SubstitutionRules(VBSyntax.QueryExpressionSyntax vbQuery, IReadOnlyList<VBSyntax.QueryClauseSyntax> clauses, VBSyntax.SelectClauseSyntax finalSelect, int end, SemanticModel semanticModel)
    {
        _vbQuery = vbQuery;
        _clauses = clauses;
        _finalSelect = finalSelect;
        _end = end;
        _semanticModel = semanticModel;
        _declarations = new(() => QueryDeclarations.Get(vbQuery, semanticModel).ToList());
    }

    public static SubstitutionRules ForQuery(VBSyntax.QueryExpressionSyntax vbQuery, SemanticModel semanticModel) =>
        new(vbQuery, vbQuery.Clauses, vbQuery.Clauses.Last() as VBSyntax.SelectClauseSyntax, vbQuery.Span.End, semanticModel);

    /// <summary>
    /// The clauses within an Aggregate clause build a separate query, whose elements are materialized for its aggregations
    /// </summary>
    public static SubstitutionRules ForAggregateSource(VBSyntax.QueryExpressionSyntax vbQuery, VBSyntax.AggregateClauseSyntax aggregate, SemanticModel semanticModel) =>
        new(vbQuery, aggregate.AdditionalQueryOperators, null, aggregate.IntoKeyword.SpanStart, semanticModel);

    /// <summary>
    /// Substituting must not change what the value refers to, or (unless it's cheap) which elements it's evaluated for, how many times, or in what order
    /// </summary>
    /// <remarks>
    /// VB evaluates the value once per element where it's declared, in declaration order.
    /// An unreferenced value is evaluated as part of the element the next time it's materialized (at a method call, the end of a join subquery, or the end of a query without a final select).
    /// After that, references read the materialized element instead.
    /// A single reference from the final select, a Where or a Let (outside any nested lambda/query or conditional operand) is the only place it's evaluated,
    /// so long as the element isn't built from it later.
    /// Clauses in between other than Let could filter, reorder or repeat elements, or (Select) drop the value from the element.
    /// Values declared in between that stay where they're declared would then be evaluated first.
    /// Substituting a delegate (`new Func<int>(() => 1)(...)`) would be valid but obscure.
    /// A cheap value still can't be substituted into a lambda or nested query that may run after the query reads it, unless it can't change.
    /// Those in a Let, Select or Group By can, since the resulting value can be used later.
    /// </remarks>
    public bool CanSubstitute(QueryRangeVariable variable, VisualBasicSyntaxNode declaration, ExpressionSyntax value)
    {
        if (IsShadowedLater(value, declaration) || value.HasAnnotation(DelegateCreationAnnotation)) return false;
        if (CsQueryBuilder.IsCheapToRepeat(value)) return !IsReadLaterThanVbReadsIt(variable.Symbol, declaration);
        return CanEvaluateWhereReferenced(variable.Symbol, declaration);
    }

    /// <summary>
    /// Marks a value that creates a delegate, which stays in a let
    /// </summary>
    public static readonly SyntaxAnnotation DelegateCreationAnnotation = new("CodeConverter.Queries.DelegateCreation");

    private bool IsReadLaterThanVbReadsIt(ISymbol symbol, VisualBasicSyntaxNode declaration) =>
        declaration is VBSyntax.ExpressionRangeVariableSyntax { Expression: var value } && !IsImmutableWithinQuery(value) &&
        GetReferences(symbol, TextSpan.FromBounds(declaration.Span.End, _vbQuery.Span.End)).Any(r => {
            var withinQuery = r.Ancestors().TakeWhile(a => a != _vbQuery).ToList();
            return withinQuery.Any(a => a is VBSyntax.LambdaExpressionSyntax or VBSyntax.QueryExpressionSyntax) &&
                   withinQuery.Last() is VBSyntax.LetClauseSyntax or VBSyntax.SelectClauseSyntax or VBSyntax.GroupByClauseSyntax;
        });

    private bool IsImmutableWithinQuery(VBSyntax.ExpressionSyntax value) =>
        value.DescendantNodesAndSelf().OfType<VBSyntax.ExpressionSyntax>().All(e =>
            e is VBSyntax.LiteralExpressionSyntax or VBSyntax.ParenthesizedExpressionSyntax ||
            e is VBSyntax.IdentifierNameSyntax && _semanticModel.GetSymbolInfo(e).Symbol is IRangeVariableSymbol rangeVariable &&
            (GetDeclaredValue(rangeVariable) is not { } declaredValue || IsImmutableWithinQuery(declaredValue)));

    /// <summary>
    /// Null for a range variable that's an element of a source, a group, or a group key, which are evaluated once and stored
    /// </summary>
    private VBSyntax.ExpressionSyntax GetDeclaredValue(IRangeVariableSymbol rangeVariable) =>
        _vbQuery.DescendantNodes().Select(n => n switch {
            VBSyntax.ExpressionRangeVariableSyntax v when n.Parent is not VBSyntax.GroupByClauseSyntax &&
                SymbolEqualityComparer.Default.Equals(VBasic.VisualBasicExtensions.GetDeclaredSymbol(_semanticModel, v), rangeVariable) => v.Expression,
            VBSyntax.AggregationRangeVariableSyntax { Aggregation: VBSyntax.FunctionAggregationSyntax aggregation } a when
                SymbolEqualityComparer.Default.Equals(VBasic.VisualBasicExtensions.GetDeclaredSymbol(_semanticModel, a), rangeVariable) => aggregation,
            _ => null
        }).FirstOrDefault(v => v != null);

    private bool CanEvaluateWhereReferenced(ISymbol symbol, VisualBasicSyntaxNode declaration) =>
        GetOnlyEvaluationPosition(symbol, declaration) is { } evaluatedAt && HasOnlyLetClausesBetween(declaration, evaluatedAt) &&
        GetValueDeclarations().Where(d => d.Declaration.SpanStart > declaration.Span.End && d.Declaration.SpanStart < evaluatedAt)
            .All(d => IsCheapToRepeat(d.Value) || d.Value is not VBSyntax.LambdaExpressionSyntax && CanEvaluateWhereReferenced(d.Symbol, d.Declaration));

    /// <summary>
    /// Values VB evaluates where they're declared, unless substituted. Group keys and items are only evaluated by the group clause.
    /// </summary>
    private IEnumerable<(VisualBasicSyntaxNode Declaration, ISymbol Symbol, VBSyntax.ExpressionSyntax Value)> GetValueDeclarations() =>
        _clauses.SelectMany(c => c switch {
            VBSyntax.LetClauseSyntax let => let.Variables.Select(v => ((VisualBasicSyntaxNode)v, (ISymbol)VBasic.VisualBasicExtensions.GetDeclaredSymbol(_semanticModel, v), v.Expression)),
            VBSyntax.SelectClauseSyntax select => select.Variables.Select(v => ((VisualBasicSyntaxNode)v, (ISymbol)VBasic.VisualBasicExtensions.GetDeclaredSymbol(_semanticModel, v), v.Expression)),
            VBSyntax.GroupByClauseSyntax groupBy => GetAggregations(groupBy.AggregationVariables),
            VBSyntax.GroupJoinClauseSyntax groupJoin => GetAggregations(groupJoin.AggregationVariables),
            VBSyntax.AggregateClauseSyntax aggregate => GetAggregations(aggregate.AggregationVariables),
            _ => Enumerable.Empty<(VisualBasicSyntaxNode, ISymbol, VBSyntax.ExpressionSyntax)>()
        });

    private IEnumerable<(VisualBasicSyntaxNode, ISymbol, VBSyntax.ExpressionSyntax)> GetAggregations(SeparatedSyntaxList<VBSyntax.AggregationRangeVariableSyntax> aggregations) =>
        aggregations.Select(a => ((VisualBasicSyntaxNode)a, (ISymbol)VBasic.VisualBasicExtensions.GetDeclaredSymbol(_semanticModel, a),
            a.Aggregation is VBSyntax.FunctionAggregationSyntax ? (VBSyntax.ExpressionSyntax)a.Aggregation : null));

    /// <summary>
    /// The VB equivalent of <see cref="CsQueryBuilder.IsCheapToRepeat"/>. A group aggregation (`Into Group`) just names the group.
    /// </summary>
    private bool IsCheapToRepeat(VBSyntax.ExpressionSyntax value) =>
        value == null || value.DescendantNodesAndSelf().OfType<VBSyntax.ExpressionSyntax>().All(e =>
            e is VBSyntax.IdentifierNameSyntax or VBSyntax.MemberAccessExpressionSyntax or VBSyntax.LiteralExpressionSyntax or VBSyntax.MeExpressionSyntax or VBSyntax.ParenthesizedExpressionSyntax &&
            !e.IsCallOrConversion(_semanticModel) &&
            (_semanticModel.GetSymbolInfo(e).Symbol is not IRangeVariableSymbol rangeVariable || IsCheapToRepeat(GetDeclaredValue(rangeVariable))));

    private int? GetOnlyEvaluationPosition(ISymbol symbol, VisualBasicSyntaxNode declaration)
    {
        var materializedAt = GetJoinWithSubqueryContaining(declaration)?.OnKeyword.SpanStart
            ?? _clauses.FirstOrDefault(c => c.SpanStart > declaration.Span.End && QueryClauseConverter.IsConvertedToMethodCall(c))?.SpanStart;
        var referencesBeforeMaterialized = GetReferences(symbol, TextSpan.FromBounds(declaration.Span.End, materializedAt ?? _end)).Take(2).ToList();
        return referencesBeforeMaterialized.Count switch {
            0 => materializedAt ?? (_finalSelect == null ? _end : null),
            1 when materializedAt == null => GetOnlyEvaluatingClause(referencesBeforeMaterialized[0], declaration)?.SpanStart,
            _ => null
        };
    }

    /// <summary>
    /// Clauses that evaluate an expression once for each element reaching them, in order, and don't build the element from the scope later
    /// </summary>
    private VBSyntax.QueryClauseSyntax GetOnlyEvaluatingClause(VBSyntax.IdentifierNameSyntax reference, VisualBasicSyntaxNode declaration)
    {
        var clause = _clauses.FirstOrDefault(c => c.Span.Contains(reference.Span));
        SyntaxNode evaluated = clause switch {
            _ when clause == _finalSelect => clause,
            VBSyntax.WhereClauseSyntax or VBSyntax.LetClauseSyntax => clause,
            _ => null
        };
        if (evaluated == null || !evaluated.Span.Contains(reference.Span) || reference.Ancestors().TakeWhile(a => a != evaluated).Any(IsConditionallyOrRepeatedlyEvaluated)) return null;
        var isElementBuiltLater = clause != _finalSelect && (_finalSelect == null || _clauses.Any(c => c.SpanStart > clause.SpanStart && c is VBSyntax.GroupByClauseSyntax));
        return !isElementBuiltLater && IsEvaluatedBeforeOtherValues(evaluated, reference, declaration) ? clause : null;
    }

    private bool HasOnlyLetClausesBetween(VisualBasicSyntaxNode declaration, int position)
    {
        var declaringClause = declaration.AncestorsAndSelf().OfType<VBSyntax.QueryClauseSyntax>().First();
        return _clauses.Where(c => c.SpanStart > declaringClause.SpanStart && c.SpanStart < position).All(c => c is VBSyntax.LetClauseSyntax);
    }

    private IEnumerable<VBSyntax.IdentifierNameSyntax> GetReferences(ISymbol symbol, TextSpan span) =>
        symbol == null ? Enumerable.Empty<VBSyntax.IdentifierNameSyntax>() : _vbQuery.DescendantNodes(span).OfType<VBSyntax.IdentifierNameSyntax>()
            .Where(id => span.Contains(id.Span) && !id.Ancestors().Any(a => a is VBSyntax.NameOfExpressionSyntax) && SymbolEqualityComparer.Default.Equals(_semanticModel.GetSymbolInfo(id).Symbol, symbol));

    /// <summary>
    /// Anything evaluated before the reference must not have side effects that VB would run after the value.
    /// That's true of values declared earlier, and expressions cheap enough to always substitute.
    /// </summary>
    private bool IsEvaluatedBeforeOtherValues(SyntaxNode evaluated, VBSyntax.IdentifierNameSyntax reference, VisualBasicSyntaxNode declaration) =>
        evaluated.DescendantNodes().OfType<VBSyntax.ExpressionSyntax>().Where(e => e.Span.End <= reference.SpanStart).All(e => e switch {
            _ when e.IsCallOrConversion(_semanticModel) => false,
            VBSyntax.IdentifierNameSyntax id => _semanticModel.GetSymbolInfo(id).Symbol is not IRangeVariableSymbol rangeVariable ||
                                                rangeVariable.Locations.FirstOrDefault()?.SourceSpan.Start < declaration.SpanStart,
            VBSyntax.LiteralExpressionSyntax or VBSyntax.MeExpressionSyntax or VBSyntax.MemberAccessExpressionSyntax or VBSyntax.ParenthesizedExpressionSyntax
                or VBSyntax.BinaryExpressionSyntax or VBSyntax.UnaryExpressionSyntax => true,
            _ => false
        });

    private static bool IsConditionallyOrRepeatedlyEvaluated(SyntaxNode node) =>
        node is VBSyntax.LambdaExpressionSyntax or VBSyntax.QueryExpressionSyntax
            or VBSyntax.TernaryConditionalExpressionSyntax or VBSyntax.BinaryConditionalExpressionSyntax or VBSyntax.ConditionalAccessExpressionSyntax ||
        node.IsKind(VBasic.SyntaxKind.AndAlsoExpression, VBasic.SyntaxKind.OrElseExpression);

    /// <summary>
    /// Declarations within joins nested in a join are part of a subquery, which is materialized before the outer join's conditions
    /// </summary>
    private static VBSyntax.JoinClauseSyntax GetJoinWithSubqueryContaining(SyntaxNode declaration) =>
        declaration.Ancestors().OfType<VBSyntax.JoinClauseSyntax>().LastOrDefault(j => j.AdditionalJoins.Any(a => a.Span.Contains(declaration.Span)));

    /// <summary>
    /// A name declared later in the VB query could capture a name in the substituted value:
    /// * C# keeps range variables VB has dropped (e.g. `x` after `Select y = x`), which a later lambda parameter or local can reuse (`Where ys.Any(Function(x) x > y)`).
    /// * A later range variable can reuse the name of a field or local the value references (`Select v = field Let field = 1 Select v`).
    ///   Clashes between range variables don't matter since those are renamed.
    /// </summary>
    public bool IsShadowedLater(ExpressionSyntax value, VisualBasicSyntaxNode declaration)
    {
        var csNames = new HashSet<string>(value.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
            .Where(id => id.Parent is not MemberAccessExpressionSyntax { Name: var name } || name != id)
            .Select(id => id.Identifier.ValueText), StringComparer.OrdinalIgnoreCase);
        var namesOutsideQuery = new HashSet<string>(declaration.DescendantNodesAndSelf().OfType<VBSyntax.IdentifierNameSyntax>()
            .Where(id => id.Parent is not VBSyntax.MemberAccessExpressionSyntax { Name: var name } || name != id)
            .Where(id => id.Parent is not VBSyntax.NamedFieldInitializerSyntax { Name: var fieldName } || fieldName != id)
            .Where(id => _semanticModel.GetSymbolInfo(id).Symbol is not IRangeVariableSymbol)
            .Select(id => id.Identifier.ValueText), StringComparer.OrdinalIgnoreCase);
        return _declarations.Value
            .Any(d => d.Position >= declaration.Span.End && (d.IsRangeVariable ? namesOutsideQuery : csNames).Contains(d.Name));
    }

    /// <summary>
    /// If so, a value substituted into the element stays in the same place relative to other clauses, so can keep its comments
    /// </summary>
    public bool IsElementMaterializedNext(VBSyntax.QueryClauseSyntax clause)
    {
        var index = _clauses.ToList().IndexOf(clause);
        return index < 0 || index == _clauses.Count - 1 || QueryClauseConverter.IsConvertedToMethodCall(_clauses[index + 1]);
    }
}
