using System;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using VisualBasicExtensions = Microsoft.CodeAnalysis.VisualBasicExtensions;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class SemanticModelExtensions
    {
        /// <summary>
        /// This check is entirely to avoid some unnecessary default initializations so the code looks less cluttered and more like the VB did.
        /// The caller should default to outputting an initializer which is always safe for equivalence/correctness.
        /// </summary>
        public static bool IsDefinitelyAssignedBeforeRead(this SemanticModel semanticModel, VariableDeclaratorSyntax localDeclarator, ModifiedIdentifierSyntax name)
        {
            Func<string, bool> equalsId = s => s.Equals(name.Identifier.ValueText, StringComparison.OrdinalIgnoreCase);

            // Find the first and last statements in the method (property, constructor, etc.) which contain the identifier
            // This may overshoot where there are multiple identifiers with the same name - this is ok, it just means we could output an initializer where one is not needed
            var statements = localDeclarator.GetAncestor<MethodBlockBaseSyntax>().Statements.Where(s =>
                s.DescendantTokens().Any(id => VisualBasicExtensions.IsKind((SyntaxToken) id, SyntaxKind.IdentifierToken) && equalsId(id.ValueText))
            ).Take(2).ToList();
            var first = statements.First();
            var last = statements.Last();

            // Analyze the data flow in this block to see if initialization is required
            // If the last statement where the identifier is used is an if block, we look at the condition rather than the whole statement. This is an easy special
            // case which catches eg. the if (TryParse()) pattern. This could happen for any node which allows multiple statements.
            var dataFlow = last is MultiLineIfBlockSyntax ifBlock
                ? semanticModel.AnalyzeDataFlow(ifBlock.IfStatement.Condition)
                : semanticModel.AnalyzeDataFlow(first, last);

            bool alwaysAssigned = dataFlow.AlwaysAssigned.Any(s => equalsId(s.Name));
            bool readInside = dataFlow.ReadInside.Any(s => equalsId(s.Name));
            bool writtenInside = dataFlow.WrittenInside.Any(s => equalsId(s.Name));
            return alwaysAssigned && !writtenInside || !readInside;
        }
    }
}