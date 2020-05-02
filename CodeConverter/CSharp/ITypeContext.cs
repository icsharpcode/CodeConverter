namespace ICSharpCode.CodeConverter.CSharp
{
    internal interface ITypeContext
    {
        AdditionalInitializers Initializers { get; }
        MethodsWithHandles MethodsWithHandles { get; }
        HoistedNodeState HoistedState { get; }
    }
}