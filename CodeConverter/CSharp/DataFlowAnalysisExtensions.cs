using System;
using System.Collections.Generic;
using System.Text;

namespace ICSharpCode.CodeConverter.CSharp;
internal static class DataFlowAnalysisExtensions
{
    /// <summary>
    /// Accesses the <see cref="DataFlowAnalysis.ReadInside" /> second time in case of exception.
    /// This is a workaround for a bug present in Roslyn up to version 4.8.0
    /// (https://github.com/dotnet/roslyn/issues/71115)
    /// </summary>
    public static System.Collections.Immutable.ImmutableArray<ISymbol> ReadInsideSafe(this DataFlowAnalysis dataFlow)
    {
        try {
            return dataFlow.ReadInside;
        } catch {
            return dataFlow.ReadInside;
        }
    }
}
