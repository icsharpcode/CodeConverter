namespace ICSharpCode.CodeConverter.CSharp;

internal interface ITypeContext
{
    AdditionalInitializers Initializers { get; }
    HandledEventsAnalysis HandledEventsAnalysis { get; }
    PerScopeState PerScopeState { get; }
    bool Any();
}