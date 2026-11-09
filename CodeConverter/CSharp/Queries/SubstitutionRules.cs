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
    /// VB evaluates the value once per element where it's declared.
    /// An unreferenced value is evaluated as part of the element the next time it's materialized (at a method call, the end of a join subquery, or the end of a query without a final select).
    /// After that, references read the materialized element instead.
    /// A single reference from the final select (outside any nested lambda/query) is the only place it's evaluated.
    /// Clauses in between other than Let and Select could filter, reorder or repeat elements.
    /// </remarks>
    public bool CanSubstitute(QueryRangeVariable variable, VisualBasicSyntaxNode declaration, ExpressionSyntax value)
    {
        if (IsShadowedLater(value, declaration)) return false;
        if (CsQueryBuilder.IsCheapToRepeat(value)) return true;
        return GetOnlyEvaluationPosition(variable, declaration) is { } evaluatedAt && HasOneToOneClausesBetween(declaration, evaluatedAt);
    }

    private int? GetOnlyEvaluationPosition(QueryRangeVariable variable, VisualBasicSyntaxNode declaration)
    {
        var materializedAt = GetJoinWithSubqueryContaining(declaration)?.OnKeyword.SpanStart
            ?? _vbQuery.Clauses.FirstOrDefault(c => c.SpanStart > declaration.Span.End && QueryClauseConverter.IsConvertedToMethodCall(c))?.SpanStart;
        var finalSelect = _vbQuery.Clauses.Last() as VBSyntax.SelectClauseSyntax;
        var referencesBeforeMaterialized = GetReferences(variable, TextSpan.FromBounds(declaration.Span.End, materializedAt ?? _vbQuery.Span.End)).Take(2).ToList();
        return referencesBeforeMaterialized.Count switch {
            0 => materializedAt ?? (finalSelect == null ? _vbQuery.Span.End : null),
            1 when materializedAt == null && IsOnlyEvaluationInFinalSelect(finalSelect, referencesBeforeMaterialized[0]) => finalSelect.SpanStart,
            _ => null
        };
    }

    private bool HasOneToOneClausesBetween(VisualBasicSyntaxNode declaration, int position)
    {
        var declaringClause = declaration.AncestorsAndSelf().OfType<VBSyntax.QueryClauseSyntax>().First();
        return _vbQuery.Clauses.Where(c => c.SpanStart > declaringClause.SpanStart && c.SpanStart < position)
            .All(c => c is VBSyntax.LetClauseSyntax or VBSyntax.SelectClauseSyntax);
    }

    private IEnumerable<VBSyntax.IdentifierNameSyntax> GetReferences(QueryRangeVariable variable, TextSpan span) =>
        variable.Symbol == null ? Enumerable.Empty<VBSyntax.IdentifierNameSyntax>() : _vbQuery.DescendantNodes(span).OfType<VBSyntax.IdentifierNameSyntax>()
            .Where(id => span.Contains(id.Span) && SymbolEqualityComparer.Default.Equals(_semanticModel.GetSymbolInfo(id).Symbol, variable.Symbol));

    private static bool IsOnlyEvaluationInFinalSelect(VBSyntax.SelectClauseSyntax finalSelect, VBSyntax.IdentifierNameSyntax reference) =>
        finalSelect != null && finalSelect.Span.Contains(reference.Span) &&
        !reference.Ancestors().TakeWhile(a => a != finalSelect).Any(IsConditionallyOrRepeatedlyEvaluated);

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
