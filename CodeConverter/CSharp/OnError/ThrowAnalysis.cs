namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// A conservative "can this statement raise an error" check, used to tell when a secondary error is impossible.
/// </summary>
/// <remarks>
/// Only exits, jumps, labels and copies of constants, locals and parameters without a conversion are treated as safe.
/// Compound assignment isn't: <c>x += 1</c> can overflow.
/// </remarks>
internal static class ThrowAnalysis
{
    public static bool CanThrow(VBSyntax.StatementSyntax statement, SemanticModel semanticModel) => statement switch {
        VBSyntax.LabelStatementSyntax or VBSyntax.GoToStatementSyntax or VBSyntax.ResumeStatementSyntax or VBSyntax.ExitStatementSyntax => false,
        VBSyntax.ReturnStatementSyntax r => r.Expression != null && !IsSafeValue(r.Expression, semanticModel),
        VBSyntax.AssignmentStatementSyntax a when a.IsKind(VBasic.SyntaxKind.SimpleAssignmentStatement) =>
            !IsLocal(a.Left, semanticModel) || !IsSafeValue(a.Right, semanticModel),
        VBSyntax.LocalDeclarationStatementSyntax d => d.Declarators.Any(v => v.AsClause is VBSyntax.AsNewClauseSyntax || v.Names.Any(n => n.ArrayBounds != null) ||
                                                                           v.Initializer != null && !IsSafeValue(v.Initializer.Value, semanticModel)),
        _ => true
    };

    private static bool IsLocal(VBSyntax.ExpressionSyntax expression, SemanticModel semanticModel) =>
        semanticModel.GetSymbolInfo(expression).Symbol is ILocalSymbol;

    private static bool IsSafeValue(VBSyntax.ExpressionSyntax expression, SemanticModel semanticModel)
    {
        if (semanticModel.GetConstantValue(expression).HasValue) return true;
        var typeInfo = semanticModel.GetTypeInfo(expression);
        return semanticModel.GetSymbolInfo(expression).Symbol is ILocalSymbol or IParameterSymbol &&
               SymbolEqualityComparer.Default.Equals(typeInfo.Type, typeInfo.ConvertedType);
    }
}
