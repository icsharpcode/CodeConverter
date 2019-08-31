using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using TypeSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax;
using VisualBasicExtensions = Microsoft.CodeAnalysis.VisualBasicExtensions;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class SemanticModelExtensions
    {
        private static PropertyInfo _unassignedVariablesProperty;

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
            _unassignedVariablesProperty = _unassignedVariablesProperty ?? methodFlow.GetType().GetProperty("UnassignedVariables");
            if (_unassignedVariablesProperty != null && _unassignedVariablesProperty.GetValue(methodFlow) is IEnumerable unassignedVariables)
                return unassignedVariables.Cast<ISymbol>();
            return null;
        }
    }
}