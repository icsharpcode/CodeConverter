namespace ICSharpCode.CodeConverter.CSharp;

internal class FieldWriteAnalysis : IFieldMutationOracle
{
    public static FieldWriteAnalysis For(Compilation compilation) => new();

    /// <remarks>Assumes a called method doesn't assign the fields its caller reads.</remarks>
    public bool MayChangeDuringCall(IFieldSymbol field, IOperation call) => false;
}
