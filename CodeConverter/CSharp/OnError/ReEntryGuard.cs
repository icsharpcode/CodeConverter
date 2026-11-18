namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// Detects a handler written to cope with running again: a condition in the handler or in the code it resumes to reads something that the handler, or that code, assigns.
/// Without one, a second pass through the handler does exactly what the first did, so VB's re-entry would loop forever and can't have been relied on.
/// </summary>
/// <remarks>
/// Conditions on <c>Err</c> alone don't count: they tell kinds of first error apart.
/// The condition can be in the resumed-to code, as in a retry loop that counts its own attempts: <c>Retry: i += 1 : If i &lt; 3 Then Work()</c>.
/// That only says the handler can re-enter code that is protected by it, so it counts for code inside the covered statements,
/// but not for what follows the handler: an ordinary tail (<c>If conn IsNot Nothing Then Close(conn)</c>) tests state the same way without being a guard.
/// </remarks>
internal static class ReEntryGuard
{
    public static bool IsPresent(IEnumerable<VBSyntax.StatementSyntax> handlerStatements, IEnumerable<VBSyntax.StatementSyntax> resumedToStatements, bool conditionsInResumedToCodeCount, SemanticModel semanticModel)
    {
        var handlerNodes = handlerStatements.SelectMany(s => s.DescendantNodesAndSelf()).ToArray();
        var resumedToNodes = resumedToStatements.SelectMany(s => s.DescendantNodesAndSelf()).ToArray();
        var conditionReads = handlerNodes.Concat(conditionsInResumedToCodeCount ? resumedToNodes : Array.Empty<SyntaxNode>()).SelectMany(GetConditions)
            .SelectMany(c => c.DescendantNodesAndSelf().OfType<VBSyntax.IdentifierNameSyntax>())
            .Select(i => semanticModel.GetSymbolInfo(i).Symbol)
            .Where(IsGuardStateSymbol)
            .ToArray();
        if (!conditionReads.Any()) return false;

        var assigned = handlerNodes.Concat(resumedToNodes)
            .OfType<VBSyntax.AssignmentStatementSyntax>()
            .Select(a => semanticModel.GetSymbolInfo(a.Left).Symbol)
            .Where(s => s != null);
        return conditionReads.Intersect(assigned, SymbolEqualityComparer.Default).Any();
    }

    private static IEnumerable<VBSyntax.ExpressionSyntax> GetConditions(SyntaxNode node) => node switch {
        VBSyntax.IfStatementSyntax s => new[] { s.Condition },
        VBSyntax.ElseIfStatementSyntax s => new[] { s.Condition },
        VBSyntax.SingleLineIfStatementSyntax s => new[] { s.Condition },
        VBSyntax.SelectStatementSyntax s => new[] { s.Expression },
        VBSyntax.WhileStatementSyntax s => new[] { s.Condition },
        VBSyntax.WhileOrUntilClauseSyntax s => new[] { s.Condition },
        VBSyntax.TernaryConditionalExpressionSyntax s => new[] { s.Condition },
        _ => Array.Empty<VBSyntax.ExpressionSyntax>()
    };

    private static bool IsGuardStateSymbol(ISymbol symbol) =>
        symbol is ILocalSymbol or IFieldSymbol or IPropertySymbol && symbol.ContainingType?.GetFullMetadataName() != "Microsoft.VisualBasic.ErrObject";
}
