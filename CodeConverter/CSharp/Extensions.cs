using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class Extensions
    {
        /// <summary>
        /// Returns the single statement in a block if it has no nested statements.
        /// If it has nested statements, and the surrounding block was removed, it could be ambiguous,
        /// e.g. if (...) { if (...) return null; } else return "";
        /// Unbundling the middle if statement would bind the else to it, rather than the outer if statement
        /// </summary>
        public static StatementSyntax UnpackNonNestedBlock(this BlockSyntax block)
        {
            return block.Statements.Count == 1 && !block.ContainsNestedStatements() ? block.Statements[0] : block;
        }

        /// <summary>
        /// Returns the single statement in a block
        /// </summary>
        public static bool TryUnpackSingleStatement(this IReadOnlyCollection<StatementSyntax> statements, out StatementSyntax singleStatement)
        {
            singleStatement = statements.Count == 1 ? statements.Single() : null;
            if (singleStatement is BlockSyntax block && TryUnpackSingleStatement(block.Statements, out var s)) {
                singleStatement = s;
            }

            return singleStatement != null;
        }

        /// <summary>
        /// Returns the single expression in a statement
        /// </summary>
        public static bool TryUnpackSingleExpressionFromStatement(this StatementSyntax statement, out ExpressionSyntax singleExpression)
        {
            switch(statement){
                case BlockSyntax blockSyntax:
                    singleExpression = null;
                    return TryUnpackSingleStatement(blockSyntax.Statements, out var nestedStmt) &&
                           TryUnpackSingleExpressionFromStatement(nestedStmt, out singleExpression);
                case ExpressionStatementSyntax expressionStatementSyntax:
                    singleExpression = expressionStatementSyntax.Expression;
                    return singleExpression != null;
                case ReturnStatementSyntax returnStatementSyntax:
                    singleExpression = returnStatementSyntax.Expression;
                    return singleExpression != null;
                default:
                    singleExpression = null;
                    return false;
            }
        }

        /// <summary>
        /// Only use this over <see cref="UnpackNonNestedBlock"/> in special cases where it will display more neatly and where you're sure nested statements don't introduce ambiguity
        /// </summary>
        public static StatementSyntax UnpackPossiblyNestedBlock(this BlockSyntax block)
        {
            SyntaxList<StatementSyntax> statementSyntaxs = block.Statements;
            return statementSyntaxs.Count == 1 ? statementSyntaxs[0] : block;
        }

        private static bool ContainsNestedStatements(this BlockSyntax block)
        {
            return block.Statements.Any(HasDescendantCSharpStatement);
        }

        private static bool HasDescendantCSharpStatement(this StatementSyntax c)
        {
            return c.DescendantNodes().OfType<StatementSyntax>().Any();
        }
    }
}
