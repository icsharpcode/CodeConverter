namespace ICSharpCode.CodeConverter.CSharp.Queries;

/// <summary>
/// The names declared within a VB query, including by lambdas and statements nested in it
/// </summary>
internal static class QueryDeclarations
{
    public sealed record Declaration(string Name, int Position, bool IsRangeVariable);

    public static IEnumerable<Declaration> Get(VBSyntax.QueryExpressionSyntax query, SemanticModel semanticModel) =>
        query.DescendantNodes().Select(n => n switch {
            VBSyntax.ModifiedIdentifierSyntax { Parent: VBSyntax.CollectionRangeVariableSyntax } x => new Declaration(x.Identifier.ValueText, x.SpanStart, true),
            VBSyntax.ModifiedIdentifierSyntax { Parent: VBSyntax.VariableNameEqualsSyntax } => null, // The range variable's own declaration below
            VBSyntax.ModifiedIdentifierSyntax x => new Declaration(x.Identifier.ValueText, x.SpanStart, false),
            VBSyntax.ExpressionRangeVariableSyntax x => FromSymbol(VBasic.VisualBasicExtensions.GetDeclaredSymbol(semanticModel, x), x),
            VBSyntax.AggregationRangeVariableSyntax x => FromSymbol(VBasic.VisualBasicExtensions.GetDeclaredSymbol(semanticModel, x), x),
            VBSyntax.ForEachStatementSyntax { ControlVariable: VBSyntax.IdentifierNameSyntax x } => new Declaration(x.Identifier.ValueText, x.SpanStart, false),
            VBSyntax.ForStatementSyntax { ControlVariable: VBSyntax.IdentifierNameSyntax x } => new Declaration(x.Identifier.ValueText, x.SpanStart, false),
            VBSyntax.CatchStatementSyntax { IdentifierName: { } x } => new Declaration(x.Identifier.ValueText, x.SpanStart, false),
            _ => null
        }).Where(d => d != null);

    private static Declaration FromSymbol(ISymbol symbol, SyntaxNode node) => symbol == null ? null : new Declaration(symbol.Name, node.SpanStart, true);
}
