using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp.Queries;

/// <summary>
/// Lets expression conversion substitute the C# expression for a reference to a VB range variable.
/// </summary>
/// <remarks>
/// VB and C# queries scope range variables differently, so a VB identifier can't always be converted to a C# identifier of the same name.
/// Keyed by symbol since the VB binder resolves each reference to exactly the range variable declaration it refers to, which deals with shadowing and nested queries for us.
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

    public void Unregister(QueryRangeVariable variable)
    {
        if (variable.Symbol != null) _variables.Remove(variable.Symbol);
    }
}
