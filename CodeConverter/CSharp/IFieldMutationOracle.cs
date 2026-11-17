namespace ICSharpCode.CodeConverter.CSharp;

/// <summary>
/// Answers whether a field can hold a different value after a call than before it, so the converter knows whether reading it again is safe.
/// </summary>
internal interface IFieldMutationOracle
{
    /// <param name="call">The call whose ByRef argument reads the field. Lets an implementation narrow the answer to what that call can reach.</param>
    /// <returns>False only when the field certainly keeps its value while the call runs.</returns>
    bool MayChangeDuringCall(IFieldSymbol field, IOperation call);
}
