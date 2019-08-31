using System;
using System.Linq;
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
        /// <summary>
        /// This check is entirely to avoid some unnecessary default initializations so the code looks less cluttered and more like the VB did.
        /// The caller should default to outputting an initializer which is always safe for equivalence/correctness.
        /// </summary>
        /// <remarks>Unfortunately the roslyn UnassignedVariablesWalker is internal only
        /// https://github.com/dotnet/roslyn/blob/007022c37c6d21ee100728954bd75113e0dfe4bd/src/Compilers/VisualBasic/Portable/Analysis/FlowAnalysis/UnassignedVariablesWalker.vb#L15
        /// It'd be possible to see the result of the diagnostic analysis, but that would miss out value types, which don't cause a warning in VB
        /// </remarks>
        public static bool IsDefinitelyAssignedBeforeRead(this SemanticModel semanticModel, Document document,
            ISymbol localSymbol, ModifiedIdentifierSyntax name)
        {
            MethodBlockBaseSyntax methodBlockBaseSyntax = name.GetAncestor<MethodBlockBaseSyntax>();
            var methodFlow = semanticModel.AnalyzeDataFlow(methodBlockBaseSyntax.Statements.First(), methodBlockBaseSyntax.Statements.Last());
            if (!methodFlow.ReadInside.Contains(localSymbol)) return true;
            if (!methodFlow.AlwaysAssigned.Contains(localSymbol)) return false;
            var nameStmt = name.GetAncestor<StatementSyntax>();

            var references = SymbolFinder.FindReferencesAsync(localSymbol, document.Project.Solution).GetAwaiter().GetResult().ToList();//TODO asyncify
            return references.SelectMany(r => r.Locations)
                .Select(r => AnalyzeDataFlow(semanticModel, methodBlockBaseSyntax, r))
                .All(nodeFlow => !nodeFlow.ReadInside.Contains(localSymbol) || nodeFlow.DataFlowsIn.Contains(localSymbol));
        }

        private static DataFlowAnalysis AnalyzeDataFlow(SemanticModel semanticModel, SyntaxNode ancestorOfLocation, ReferenceLocation location)
        {
            return AnalyzeDataFlow(semanticModel, ancestorOfLocation.FindNode(location.Location.SourceSpan));
        }

        private static DataFlowAnalysis AnalyzeDataFlow(SemanticModel semanticModel, SyntaxNode refNode)
        {
            var nodeExprOrStmt = refNode.GetAncestors().First(a => a is ExpressionSyntax || a is ExecutableStatementSyntax);
            var nodeFlow = semanticModel.AnalyzeDataFlow(nodeExprOrStmt);
            return nodeFlow;
        }
    }
}