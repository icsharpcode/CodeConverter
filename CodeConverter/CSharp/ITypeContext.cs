using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal interface ITypeContext
    {
        AdditionalInitializers Initializers { get; }
        MethodsWithHandles MethodsWithHandles { get; }
        HoistedNodeState HoistedState { get; }
        IEnumerable<IAssemblySymbol> AssembliesBeingConverted { get; }
        bool Any();
    }
}