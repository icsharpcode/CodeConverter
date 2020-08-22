using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class SemanticModelExtensions
    {
        /// <summary>
        /// This check is entirely to avoid some unnecessary default initializations so the code looks less cluttered and more like the VB did.
        /// The caller should default to outputting an initializer which is always safe for equivalence/correctness.
        /// </summary>
        public static bool IsDefinitelyAssignedBeforeRead(this SemanticModel semanticModel,
            ISymbol locallyDeclaredSymbol, VBSyntax.ModifiedIdentifierSyntax syntaxForSymbol)
        {
            var methodBlockBaseSyntax = syntaxForSymbol.GetAncestor<VBSyntax.MethodBlockBaseSyntax>();
            var methodFlow = semanticModel.AnalyzeDataFlow(methodBlockBaseSyntax.Statements.First(), methodBlockBaseSyntax.Statements.Last());
            return DefiniteAssignmentAnalyzer.IsDefinitelyAssignedBeforeRead(locallyDeclaredSymbol, methodFlow);
        }

        public static IOperation GetExpressionOperation(this SemanticModel semanticModel, Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax expressionSyntax)
        {
            var op = semanticModel.GetOperation(expressionSyntax);
            while (true) {
                switch (op) {
                    case IArgumentOperation argumentOperation:
                        op = argumentOperation.Value;
                        continue;
                    case IConversionOperation conversionOperation:
                        op = conversionOperation.Operand;
                        continue;
                    case IParenthesizedOperation parenthesizedOperation:
                        op = parenthesizedOperation.Operand;
                        continue;
                    default:
                        return op;
                }
            }
        }

        /// <summary>
        /// Returns true only if expressions static (i.e. doesn't reference the containing instance)
        /// </summary>
        public static bool IsDefinitelyStatic(this SemanticModel semanticModel, VBSyntax.ModifiedIdentifierSyntax vbName, VBSyntax.ExpressionSyntax vbInitValue)
        {
            var arrayBoundExpressions = vbName.ArrayBounds != null ? vbName.ArrayBounds.Arguments.Select(a => a.GetExpression()) : Enumerable.Empty<VBSyntax.ExpressionSyntax>();
            var expressions = vbInitValue.Yield().Concat(arrayBoundExpressions).Where(x => x != null).ToArray();
            return expressions.All(e => semanticModel.IsDefinitelyStatic(e));
        }

        /// <summary>
        /// Returns true only if expression is static (i.e. doesn't reference the containing instance)
        /// </summary>
        private static bool IsDefinitelyStatic(this SemanticModel semanticModel, VBSyntax.ExpressionSyntax e)
        {
            return semanticModel.GetOperation(e).DescendantsAndSelf().OfType<IInstanceReferenceOperation>().Any() == false;
        }
    }
}