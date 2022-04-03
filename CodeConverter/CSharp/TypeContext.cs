namespace ICSharpCode.CodeConverter.CSharp;

internal class TypeContext : ITypeContext
{
    private readonly Stack<(AdditionalInitializers Initializers, HandledEventsAnalysis Methods)> _contextStack = new();

    public AdditionalInitializers Initializers => _contextStack.Peek().Initializers;
    public HandledEventsAnalysis HandledEventsAnalysis => _contextStack.Peek().Methods;

    public PerScopeState PerScopeState { get; internal set; } = new();

    public void Push(HandledEventsAnalysis methodWithHandles, AdditionalInitializers additionalInitializers)
    {
        _contextStack.Push((additionalInitializers, methodWithHandles));
    }

    public void Pop() => _contextStack.Pop();
    public bool Any() => _contextStack.Count > 0;
}