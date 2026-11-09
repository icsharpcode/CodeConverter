using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ICSharpCode.CodeConverter.CSharp.Queries;

/// <summary>
/// Decides whether a value can be substituted for each reference to its range variable, rather than declared with a let clause
/// </summary>
internal class SubstitutionRules
{
    private readonly VBSyntax.QueryExpressionSyntax _vbQuery;
    private readonly SemanticModel _semanticModel;

    public SubstitutionRules(VBSyntax.QueryExpressionSyntax vbQuery, SemanticModel semanticModel)
    {
        _vbQuery = vbQuery;
        _semanticModel = semanticModel;
    }

    /// <summary>
    /// Substituting must not change what the value refers to, or (unless it's cheap) which elements it's evaluated for, how many times, or in what order
    /// </summary>
    /// <remarks>
    /// VB evaluates the value once per element where it's declared, in declaration order.
    /// An unreferenced value is evaluated as part of the element the next time it's materialized (at a method call, the end of a join subquery, or the end of a query without a final select).
    /// After that, references read the materialized element instead.
    /// A single reference from the final select (outside any nested lambda/query) is the only place it's evaluated.
    /// Clauses in between other than Let could filter, reorder or repeat elements, or (Select) drop the value from the element.
    /// Values declared in between that stay where they're declared would then be evaluated first.
    /// Substituting a lambda (`new Func<int>(() => 1)(...)`) would be valid but obscure.
    /// </remarks>
    public bool CanSubstitute(QueryRangeVariable variable, VisualBasicSyntaxNode declaration, ExpressionSyntax value)
    {
        if (IsShadowedLater(value, declaration) || IsDelegateCreation(value)) return false;
        if (CsQueryBuilder.IsCheapToRepeat(value)) return true;
        return CanEvaluateWhereReferenced(variable.Symbol, declaration);
    }

    private bool CanEvaluateWhereReferenced(ISymbol symbol, VisualBasicSyntaxNode declaration) =>
        GetOnlyEvaluationPosition(symbol, declaration) is { } evaluatedAt && HasOnlyLetClausesBetween(declaration, evaluatedAt) &&
        GetValueDeclarations().Where(d => d.Declaration.SpanStart > declaration.Span.End && d.Declaration.SpanStart < evaluatedAt)
            .All(d => IsCheapToRepeat(d.Value) || d.Value is not VBSyntax.LambdaExpressionSyntax && CanEvaluateWhereReferenced(d.Symbol, d.Declaration));

    /// <summary>
    /// Values VB evaluates where they're declared, unless substituted. Group keys and items are only evaluated by the group clause.
    /// </summary>
    private IEnumerable<(VisualBasicSyntaxNode Declaration, ISymbol Symbol, VBSyntax.ExpressionSyntax Value)> GetValueDeclarations() =>
        _vbQuery.Clauses.SelectMany(c => c switch {
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
            !e.IsCallOrConversion(_semanticModel));

    private static bool IsDelegateCreation(ExpressionSyntax value) =>
        value is ObjectCreationExpressionSyntax { ArgumentList.Arguments: { Count: 1 } arguments } && arguments[0].Expression is AnonymousFunctionExpressionSyntax;

    private int? GetOnlyEvaluationPosition(ISymbol symbol, VisualBasicSyntaxNode declaration)
    {
        var materializedAt = GetJoinWithSubqueryContaining(declaration)?.OnKeyword.SpanStart
            ?? _vbQuery.Clauses.FirstOrDefault(c => c.SpanStart > declaration.Span.End && QueryClauseConverter.IsConvertedToMethodCall(c))?.SpanStart;
        var finalSelect = _vbQuery.Clauses.Last() as VBSyntax.SelectClauseSyntax;
        var referencesBeforeMaterialized = GetReferences(symbol, TextSpan.FromBounds(declaration.Span.End, materializedAt ?? _vbQuery.Span.End)).Take(2).ToList();
        return referencesBeforeMaterialized.Count switch {
            0 => materializedAt ?? (finalSelect == null ? _vbQuery.Span.End : null),
            1 when materializedAt == null && IsOnlyEvaluationInFinalSelect(finalSelect, referencesBeforeMaterialized[0], declaration) => finalSelect.SpanStart,
            _ => null
        };
    }

    private bool HasOnlyLetClausesBetween(VisualBasicSyntaxNode declaration, int position)
    {
        var declaringClause = declaration.AncestorsAndSelf().OfType<VBSyntax.QueryClauseSyntax>().First();
        return _vbQuery.Clauses.Where(c => c.SpanStart > declaringClause.SpanStart && c.SpanStart < position).All(c => c is VBSyntax.LetClauseSyntax);
    }

    private IEnumerable<VBSyntax.IdentifierNameSyntax> GetReferences(ISymbol symbol, TextSpan span) =>
        symbol == null ? Enumerable.Empty<VBSyntax.IdentifierNameSyntax>() : _vbQuery.DescendantNodes(span).OfType<VBSyntax.IdentifierNameSyntax>()
            .Where(id => span.Contains(id.Span) && !id.Ancestors().Any(a => a is VBSyntax.NameOfExpressionSyntax) && SymbolEqualityComparer.Default.Equals(_semanticModel.GetSymbolInfo(id).Symbol, symbol));

    private bool IsOnlyEvaluationInFinalSelect(VBSyntax.SelectClauseSyntax finalSelect, VBSyntax.IdentifierNameSyntax reference, VisualBasicSyntaxNode declaration) =>
        finalSelect != null && finalSelect.Span.Contains(reference.Span) &&
        !reference.Ancestors().TakeWhile(a => a != finalSelect).Any(IsConditionallyOrRepeatedlyEvaluated) &&
        IsEvaluatedBeforeOtherValues(finalSelect, reference, declaration);

    /// <summary>
    /// Anything the final select evaluates before the reference must not have side effects that VB would run after the value.
    /// That's true of values declared earlier, and expressions cheap enough to always substitute.
    /// </summary>
    private bool IsEvaluatedBeforeOtherValues(VBSyntax.SelectClauseSyntax finalSelect, VBSyntax.IdentifierNameSyntax reference, VisualBasicSyntaxNode declaration) =>
        finalSelect.DescendantNodes().OfType<VBSyntax.ExpressionSyntax>().Where(e => e.Span.End <= reference.SpanStart).All(e => e switch {
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
            .Where(id => _semanticModel.GetSymbolInfo(id).Symbol is not IRangeVariableSymbol)
            .Select(id => id.Identifier.ValueText), StringComparer.OrdinalIgnoreCase);
        return QueryDeclarations.Get(_vbQuery, _semanticModel)
            .Any(d => d.Position >= declaration.Span.End && (d.IsRangeVariable ? namesOutsideQuery : csNames).Contains(d.Name));
    }

    /// <summary>
    /// If so, a value substituted into the element stays in the same place relative to other clauses, so can keep its comments
    /// </summary>
    public bool IsElementMaterializedNext(VBSyntax.QueryClauseSyntax clause)
    {
        var clauses = _vbQuery.Clauses;
        var index = clauses.IndexOf(clause);
        return index < 0 || index == clauses.Count - 1 || QueryClauseConverter.IsConvertedToMethodCall(clauses[index + 1]);
    }
}
