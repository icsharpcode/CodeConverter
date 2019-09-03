using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class DefiniteAssignmentAnalyzer
    {
        private static Func<DataFlowAnalysis, IEnumerable<ISymbol>> _unassignedVariablesProperty;

        public static bool IsDefinitelyAssignedBeforeRead(ISymbol localSymbol, DataFlowAnalysis methodFlow)
        {
            if (!methodFlow.ReadInside.Contains(localSymbol)) return true;
            var unassignedVariables = GetUnassignedVariables(methodFlow);
            return unassignedVariables != null && !unassignedVariables.Contains(localSymbol);
        }

        /// <remarks>Unfortunately the roslyn UnassignedVariablesWalker and all useful collections created from it are internal only
        /// Other attempts using DataFlowsIn on each reference showed that "DataFlowsIn" even from an uninitialized variable (at least in the case of ints)
        /// https://github.com/dotnet/roslyn/blob/007022c37c6d21ee100728954bd75113e0dfe4bd/src/Compilers/VisualBasic/Portable/Analysis/FlowAnalysis/UnassignedVariablesWalker.vb#L15
        /// It'd be possible to see the result of the diagnostic analysis, but that would miss out value types, which don't cause a warning in VB
        /// </remarks>
        private static IEnumerable<ISymbol> GetUnassignedVariables(DataFlowAnalysis methodFlow)
        {
            //PERF: Assume we'll only be passed one type of data flow analysis (VisualBasicDataFlowAnalysis)
            _unassignedVariablesProperty = _unassignedVariablesProperty ?? CreateDelegate(methodFlow);
            return _unassignedVariablesProperty(methodFlow);
        }

        private static Func<DataFlowAnalysis, IEnumerable<ISymbol>> CreateDelegate(DataFlowAnalysis methodFlow)
        {
            try {
                var getMethod = methodFlow.GetType().GetProperty("UnassignedVariables")?.GetMethod;
                var getDelegate = getMethod?.CreateOpenInstanceDelegateForcingType<DataFlowAnalysis, IEnumerable>();
                return dataFlowAnalysis => getDelegate?.Invoke(dataFlowAnalysis).Cast<ISymbol>();
            } catch (Exception e) {
                Debug.Fail(e.Message);
                return _ => null;
            }
        }
    }
}