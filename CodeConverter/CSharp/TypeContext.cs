using System.Collections.Generic;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class TypeContext : ITypeContext
    {
        private readonly Stack<(AdditionalInitializers Initializers, MethodsWithHandles Methods)> _contextStack = new Stack<(AdditionalInitializers Initializers, MethodsWithHandles Methods)>();

        public AdditionalInitializers Initializers => _contextStack.Peek().Initializers;
        public MethodsWithHandles MethodsWithHandles => _contextStack.Peek().Methods;

        public HoistedNodeState HoistedState { get; internal set; } = new HoistedNodeState();

        public void Push(MethodsWithHandles methodWithHandles, AdditionalInitializers additionalInitializers)
        {
            _contextStack.Push((additionalInitializers, methodWithHandles));
        }

        public void Pop() => _contextStack.Pop();
    }
}
