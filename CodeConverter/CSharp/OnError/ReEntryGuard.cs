namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// Detects a handler written to cope with running again: a condition in it reads something that the handler, or the code it resumes to, assigns.
/// Without one, a second pass through the handler does exactly what the first did, so VB's re-entry would loop forever and can't have been relied on.
/// </summary>
/// <remarks>Conditions on <c>Err</c> alone don't count: they tell kinds of first error apart.</remarks>
internal static class ReEntryGuard
{
    public static bool IsPresent(IEnumerable<VBSyntax.StatementSyntax> handlerStatements, IEnumerable<VBSyntax.StatementSyntax> resumedToStatements, SemanticModel semanticModel)
    {
        var handlerNodes = handlerStatements.SelectMany(s => s.DescendantNodesAndSelf()).ToArray();
        var conditionReads = handlerNodes.SelectMany(GetConditions)
            .SelectMany(c => c.DescendantNodesAndSelf().OfType<VBSyntax.IdentifierNameSyntax>())
            .Select(i => semanticModel.GetSymbolInfo(i).Symbol)
            .Where(IsGuardStateSymbol)
            .ToArray();
        if (!conditionReads.Any()) return false;

        var assigned = handlerNodes.Concat(resumedToStatements.SelectMany(s => s.DescendantNodesAndSelf()))
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
