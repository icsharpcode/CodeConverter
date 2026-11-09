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
    /// Substituting must not change what the value refers to, or how many times it's evaluated (unless that's cheap)
    /// </summary>
    /// <remarks>
    /// An unreferenced value is evaluated at most once, as part of the element the next time it's materialized.
    /// After that (at a method call or the end of a join subquery), references read the materialized element instead.
    /// A single reference from the final select (outside any nested lambda/query) is the only place it's evaluated.
    /// </remarks>
    public bool CanSubstitute(QueryRangeVariable variable, VisualBasicSyntaxNode declaration, ExpressionSyntax value)
    {
        if (IsShadowedLater(value, declaration)) return false;
        if (variable.Symbol == null || CsQueryBuilder.IsCheapToRepeat(value)) return true;
        var materializedAt = GetJoinWithSubqueryContaining(declaration)?.OnKeyword.SpanStart
            ?? _vbQuery.Clauses.FirstOrDefault(c => c.SpanStart > declaration.Span.End && QueryClauseConverter.IsConvertedToMethodCall(c))?.SpanStart;
        var referencesBeforeMaterialized = GetReferences(variable, TextSpan.FromBounds(declaration.Span.End, materializedAt ?? _vbQuery.Span.End)).Take(2).ToList();
        return referencesBeforeMaterialized.Count switch {
            0 => true,
            1 => materializedAt == null && IsOnlyEvaluationInFinalSelect(referencesBeforeMaterialized[0]),
            _ => false
        };
    }

    private IEnumerable<VBSyntax.IdentifierNameSyntax> GetReferences(QueryRangeVariable variable, TextSpan span) =>
        _vbQuery.DescendantNodes(span).OfType<VBSyntax.IdentifierNameSyntax>()
            .Where(id => span.Contains(id.Span) && SymbolEqualityComparer.Default.Equals(_semanticModel.GetSymbolInfo(id).Symbol, variable.Symbol));

    private bool IsOnlyEvaluationInFinalSelect(VBSyntax.IdentifierNameSyntax reference) =>
        _vbQuery.Clauses.Last() is VBSyntax.SelectClauseSyntax finalSelect && finalSelect.Span.Contains(reference.Span) &&
        !reference.Ancestors().TakeWhile(a => a != finalSelect).Any(a => a is VBSyntax.LambdaExpressionSyntax or VBSyntax.QueryExpressionSyntax);

    /// <summary>
    /// Declarations within joins nested in a join are part of a subquery, which is materialized before the outer join's conditions
    /// </summary>
    private static VBSyntax.JoinClauseSyntax GetJoinWithSubqueryContaining(SyntaxNode declaration) =>
        declaration.Ancestors().OfType<VBSyntax.JoinClauseSyntax>().LastOrDefault(j => j.AdditionalJoins.Any(a => a.Span.Contains(declaration.Span)));

    /// <summary>
    /// C# keeps range variables VB has dropped (e.g. `x` after `Select y = x`), which a substituted value may reference.
    /// A later VB lambda can reuse that name for a parameter (`Where ys.Any(Function(x) x > y)`), which would capture it.
    /// Later range variables don't need checking since clashing ones are renamed.
    /// </summary>
    public bool IsShadowedLater(ExpressionSyntax value, VisualBasicSyntaxNode declaration)
    {
        var referencedNames = new HashSet<string>(value.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
            .Where(id => id.Parent is not MemberAccessExpressionSyntax { Name: var name } || name != id)
            .Select(id => id.Identifier.ValueText), StringComparer.OrdinalIgnoreCase);
        return _vbQuery.DescendantNodes().OfType<VBSyntax.ParameterSyntax>()
            .Any(p => p.SpanStart >= declaration.Span.End && referencedNames.Contains(p.Identifier.Identifier.ValueText));
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
