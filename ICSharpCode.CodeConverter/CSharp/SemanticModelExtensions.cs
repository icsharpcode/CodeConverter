using System.Collections.Generic;
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
        /// <summary>
        /// This check is entirely to avoid some unnecessary default initializations so the code looks less cluttered and more like the VB did.
        /// The caller should default to outputting an initializer which is always safe for equivalence/correctness.
        /// </summary>
        public static bool IsDefinitelyAssignedBeforeRead(this SemanticModel semanticModel,
            ISymbol locallyDeclaredSymbol, ModifiedIdentifierSyntax syntaxForSymbol)
        {
            var methodBlockBaseSyntax = syntaxForSymbol.GetAncestor<MethodBlockBaseSyntax>();
            var methodFlow = semanticModel.AnalyzeDataFlow(methodBlockBaseSyntax.Statements.First(), methodBlockBaseSyntax.Statements.Last());
            return DefiniteAssignmentAnalyzer.IsDefinitelyAssignedBeforeRead(locallyDeclaredSymbol, methodFlow);
        }
    }
}