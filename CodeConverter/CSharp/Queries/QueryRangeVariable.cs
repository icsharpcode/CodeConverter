using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp.Queries;

/// <summary>
/// A VB range variable that is in scope at some point of a query, together with the C# expression that currently evaluates it.
/// </summary>
/// <remarks>
/// The C# expression changes as the C# query is built, e.g. `x` might become `g.Key.x` after a group by, or `e.x` after a method call boundary.
/// An unnamed variable (from a single-expression VB Select such as `Select x * 2`) has no symbol and can't be referenced by VB code, but still forms the query's element.
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
