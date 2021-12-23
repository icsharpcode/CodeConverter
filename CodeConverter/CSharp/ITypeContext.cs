namespace ICSharpCode.CodeConverter.CSharp
{
    internal interface ITypeContext
    {
        AdditionalInitializers Initializers { get; }
        HandledEventsAnalysis HandledEventsAnalysis { get; }
        HoistedNodeState HoistedState { get; }
        bool Any();
    }
}