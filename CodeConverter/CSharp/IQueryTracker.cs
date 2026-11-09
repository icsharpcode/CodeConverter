namespace ICSharpCode.CodeConverter.CSharp;

internal interface IQueryTracker
{
    /// <summary>
    /// Within a query, or a lambda converted to an expression tree. Comparisons are kept simple so providers like EF can translate them.
    /// </summary>
    bool IsWithinQuery { get; set; }

    /// <summary>
    /// Within a lambda or query converted to an expression tree rather than a delegate
    /// </summary>
    bool IsWithinExpressionTree { get; set; }
}