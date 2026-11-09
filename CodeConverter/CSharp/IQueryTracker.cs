namespace ICSharpCode.CodeConverter.CSharp;

internal interface IQueryTracker
{
    /// <summary>
    /// Within a query, or a lambda converted to an expression tree. Comparisons are kept simple so they can be translated, e.g. by EF.
    /// </summary>
    bool IsWithinQuery { get; set; }

    /// <summary>
    /// Within a lambda or query that will be converted to an expression tree rather than a delegate.
    /// </summary>
    bool IsWithinExpressionTree { get; set; }
}