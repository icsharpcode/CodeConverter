using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class TypeContext : ITypeContext
    {
        private readonly Stack<(AdditionalInitializers Initializers, HandledEventsAnalysis Methods)> _contextStack = new Stack<(AdditionalInitializers Initializers, HandledEventsAnalysis Methods)>();

        public AdditionalInitializers Initializers => _contextStack.Peek().Initializers;
        public HandledEventsAnalysis HandledEventsAnalysis => _contextStack.Peek().Methods;

        public HoistedNodeState HoistedState { get; internal set; } = new HoistedNodeState();

        public void Push(HandledEventsAnalysis methodWithHandles, AdditionalInitializers additionalInitializers)
        {
            _contextStack.Push((additionalInitializers, methodWithHandles));
        }

        public void Pop() => _contextStack.Pop();
        public bool Any() => _contextStack.Count > 0;
    }
}
