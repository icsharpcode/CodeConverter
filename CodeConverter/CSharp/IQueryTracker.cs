namespace ICSharpCode.CodeConverter.CSharp;

internal interface IQueryTracker
{
    /// <summary>
    /// Within a lambda or query converted to an expression tree rather than a delegate, e.g. for IQueryable.
    /// Comparisons there are kept simple so providers like EF can translate them, since the provider decides their semantics anyway.
    /// </summary>
    bool IsWithinExpressionTree { get; set; }
}