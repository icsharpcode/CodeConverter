using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp.Queries;

/// <summary>
/// Lets expression conversion substitute the current C# expression for a reference to a VB range variable
/// </summary>
/// <remarks>
/// Keyed by symbol since the VB binder has resolved which declaration each reference refers to, including through shadowing and nested queries.
/// Range variable symbols only occur within their query, so they don't need removing afterwards.
/// </remarks>
internal class RangeVariableReplacements
{
    private readonly Dictionary<ISymbol, QueryRangeVariable> _variables = new(SymbolEqualityComparer.Default);

    public bool TryGetReplacement(ISymbol symbol, out ExpressionSyntax replacement)
    {
        replacement = symbol is IRangeVariableSymbol && _variables.TryGetValue(symbol, out var variable) ? variable.CsExpression : null;
        return replacement != null;
    }

    public void Register(QueryRangeVariable variable)
    {
        if (variable.Symbol != null) _variables[variable.Symbol] = variable;
    }

}
