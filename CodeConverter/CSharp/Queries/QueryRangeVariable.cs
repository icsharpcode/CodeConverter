using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp.Queries;

/// <summary>
/// A VB range variable and the C# expression that currently evaluates it
/// </summary>
/// <remarks>
/// <see cref="CsExpression"/> changes as the C# query is built, e.g. `x` becomes `g.Key.x` after a group by.
/// <see cref="Symbol"/> is null for a generated variable, or VB's unnamed `Select x * 2`, which can't be referenced but is still part of the element.
/// </remarks>
internal sealed class QueryRangeVariable
{
    public QueryRangeVariable(IRangeVariableSymbol symbol, string name)
    {
        Symbol = symbol;
        Name = name;
    }

    public IRangeVariableSymbol Symbol { get; }
    public string Name { get; }
    public ExpressionSyntax CsExpression { get; set; }

    public override string ToString() => $"{Name} => {CsExpression}";
}
