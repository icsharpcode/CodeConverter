namespace ICSharpCode.CodeConverter.CSharp.Queries;

/// <summary>
/// Works out whether joins nested within a VB join can be written as a flat sequence of C# joins
/// </summary>
internal class FlatJoinPlanner
{
    private readonly SemanticModel _semanticModel;

    public FlatJoinPlanner(SemanticModel semanticModel) => _semanticModel = semanticModel;

    /// <summary>
    /// Attaches each condition to the earliest join where everything it references is in scope
    /// </summary>
    /// <returns>Null if C# can't express it as flat joins, e.g. if there's a Group Join or a condition's inner side references several joined variables</returns>
    public List<(VBSyntax.JoinClauseSyntax Join, List<VBSyntax.JoinConditionSyntax> Conditions)> Plan(VBSyntax.SimpleJoinClauseSyntax join)
    {
        var joins = GetNestedJoins(join).ToList();
        if (joins.Any(j => j is not VBSyntax.SimpleJoinClauseSyntax)) return null;
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
            if (!conditions.Any() || !conditions.All(c => IsValidCsJoinCondition(c, joinedSymbol, joinedSymbols))) return null;
            unplacedConditions.RemoveAll(conditions.Contains);
            plan.Add((vbJoin, conditions));
        }
        return unplacedConditions.Any() ? null : plan;
    }

    /// <summary>
    /// C# only allows the joined variable on the inner side, and the outer side can't reference it (CS1937, CS1938)
    /// </summary>
    private bool IsValidCsJoinCondition(VBSyntax.JoinConditionSyntax condition, ISymbol joinedSymbol, IReadOnlyCollection<ISymbol> nestedJoinedSymbols)
    {
        var (left, right) = (GetReferencedSymbols(condition.Left, nestedJoinedSymbols), GetReferencedSymbols(condition.Right, nestedJoinedSymbols));
        var (inner, outer) = left.Contains(joinedSymbol) ? (left, right) : (right, left);
        return inner.All(s => SymbolEqualityComparer.Default.Equals(s, joinedSymbol)) && !outer.Contains(joinedSymbol);
    }

    private static IEnumerable<VBSyntax.JoinClauseSyntax> GetNestedJoins(VBSyntax.JoinClauseSyntax join) =>
        join.AdditionalJoins.SelectMany(GetNestedJoins).Prepend(join);

    private IRangeVariableSymbol GetJoinedSymbol(VBSyntax.JoinClauseSyntax join) => VBasic.VisualBasicExtensions.GetDeclaredSymbol(_semanticModel, join.JoinedVariables.Single());

    public HashSet<ISymbol> GetReferencedSymbols(SyntaxNode node, IReadOnlyCollection<ISymbol> candidates) =>
        new(node.DescendantNodesAndSelf().OfType<VBSyntax.IdentifierNameSyntax>()
            .Select(id => _semanticModel.GetSymbolInfo(id).Symbol)
            .Where(s => s != null && candidates.Contains(s, SymbolEqualityComparer.Default)), SymbolEqualityComparer.Default);
}
