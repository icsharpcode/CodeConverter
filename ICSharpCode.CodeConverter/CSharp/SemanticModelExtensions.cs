using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class SemanticModelExtensions
    {
        private static Func<DataFlowAnalysis, IEnumerable<ISymbol>> _unassignedVariablesProperty;

        /// <summary>
        /// This check is entirely to avoid some unnecessary default initializations so the code looks less cluttered and more like the VB did.
        /// The caller should default to outputting an initializer which is always safe for equivalence/correctness.
        /// </summary>
        public static bool IsDefinitelyAssignedBeforeRead(this SemanticModel semanticModel, Document document,
            ISymbol localSymbol, ModifiedIdentifierSyntax name)
        {
            MethodBlockBaseSyntax methodBlockBaseSyntax = name.GetAncestor<MethodBlockBaseSyntax>();
            var methodFlow = semanticModel.AnalyzeDataFlow(methodBlockBaseSyntax.Statements.First(), methodBlockBaseSyntax.Statements.Last());
            if (!methodFlow.ReadInside.Contains(localSymbol)) return true;
            var unassignedVariables = GetUnassignedVariables(methodFlow);
            if (unassignedVariables != null) return !unassignedVariables.Contains(localSymbol);
            return false;
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
                var getDelegate = getMethod?.CreateOpenDelegateOfType<DataFlowAnalysis, IEnumerable>();
                return dataFlowAnalysis => getDelegate?.Invoke(dataFlowAnalysis).Cast<ISymbol>();
            } catch (Exception e) {
                Debug.Fail(e.Message);
                return _ => null;
            }
        }
    }
}