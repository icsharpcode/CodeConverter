using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using ExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax;
using ParenthesizedExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ParenthesizedExpressionSyntax;
using SkippedTokensTriviaSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.SkippedTokensTriviaSyntax;

namespace ICSharpCode.CodeConverter.Util
{
#if NR6
    public
#endif
    static class SyntaxExtensions
    {
        /// <summary>
        /// Look inside a trivia list for a skipped token that contains the given position.
        /// </summary>
        private static readonly Func<SyntaxTriviaList, int, SyntaxToken> s_findSkippedTokenBackward =
            (l, p) => FindTokenHelper.FindSkippedTokenBackward(GetSkippedTokens(l), p);

        /// <summary>
        /// return only skipped tokens
        /// </summary>
        private static IEnumerable<SyntaxToken> GetSkippedTokens(SyntaxTriviaList list)
        {
            return list.Where(trivia => trivia.RawKind == (int)SyntaxKind.SkippedTokensTrivia)
                .SelectMany(t => ((SkippedTokensTriviaSyntax)t.GetStructure()).Tokens);
        }

        public static ExpressionSyntax SkipParens(this ExpressionSyntax expression)
        {
            if (expression == null)
                return null;
            while (expression != null && expression.IsKind(SyntaxKind.ParenthesizedExpression)) {
                expression = ((ParenthesizedExpressionSyntax)expression).Expression;
            }
            return expression;
        }

        public static bool IsParentKind(this SyntaxNode node, SyntaxKind kind)
        {
            return node != null && node.Parent.IsKind(kind);
        }

        public static bool IsParentKind(this SyntaxToken node, SyntaxKind kind)
        {
            return node.Parent != null && node.Parent.IsKind(kind);
        }

        public static TSymbol GetEnclosingSymbol<TSymbol>(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
            where TSymbol : ISymbol
        {
            for (var symbol = semanticModel.GetEnclosingSymbol(position, cancellationToken);
                 symbol != null;
                 symbol = symbol.ContainingSymbol) {
                if (symbol is TSymbol) {
                    return (TSymbol)symbol;
                }
            }

            return default(TSymbol);
        }

        internal static bool IsValidSymbolInfo(ISymbol symbol)
        {
            // name bound to only one symbol is valid
            return symbol != null && !symbol.IsErrorType();
        }


        private static SyntaxNode FindImmediatelyEnclosingLocalVariableDeclarationSpace(SyntaxNode syntax)
        {
            for (var declSpace = syntax; declSpace != null; declSpace = declSpace.Parent) {
                switch (declSpace.Kind()) {
                    // These are declaration-space-defining syntaxes, by the spec:
                    case SyntaxKind.MethodDeclaration:
                    case SyntaxKind.IndexerDeclaration:
                    case SyntaxKind.OperatorDeclaration:
                    case SyntaxKind.ConstructorDeclaration:
                    case SyntaxKind.Block:
                    case SyntaxKind.ParenthesizedLambdaExpression:
                    case SyntaxKind.SimpleLambdaExpression:
                    case SyntaxKind.AnonymousMethodExpression:
                    case SyntaxKind.SwitchStatement:
                    case SyntaxKind.ForEachKeyword:
                    case SyntaxKind.ForStatement:
                    case SyntaxKind.UsingStatement:

                    // SPEC VIOLATION: We also want to stop walking out if, say, we are in a field
                    // initializer. Technically according to the wording of the spec it should be
                    // legal to use a simple name inconsistently inside a field initializer because
                    // it does not define a local variable declaration space. In practice of course
                    // we want to check for that. (As the native compiler does as well.)

                    case SyntaxKind.FieldDeclaration:
                        return declSpace;
                }
            }

            return null;
        }
        public static bool HasOperandOfUnconvertedType(this AssignmentStatementSyntax node, string operandType, SemanticModel semanticModel)
        {
            return new[] { node.Left, node.Right }.Any(e => ExpressionSyntaxExtensions.UnconvertedIsType(e, operandType, semanticModel));
        }
    }
}

