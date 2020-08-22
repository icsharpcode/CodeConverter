using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using CS = Microsoft.CodeAnalysis.CSharp;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.Util
{
    internal static class SyntaxExtensions
    {
        /// <summary>
        /// return only skipped tokens
        /// </summary>
        private static IEnumerable<SyntaxToken> GetSkippedTokens(SyntaxTriviaList list)
        {
            return list.Where(trivia => trivia.RawKind == (int)CS.SyntaxKind.SkippedTokensTrivia)
                .SelectMany(t => ((VBSyntax.SkippedTokensTriviaSyntax)t.GetStructure()).Tokens);
        }

        public static CS.Syntax.ExpressionSyntax SkipIntoParens(this CS.Syntax.ExpressionSyntax expression)
        {
            if (expression == null)
                return null;
            while (expression is CS.Syntax.ParenthesizedExpressionSyntax pes) {
                expression = pes.Expression;
            }
            return expression;
        }

        public static VBSyntax.ExpressionSyntax SkipIntoParens(this VBSyntax.ExpressionSyntax expression)
        {
            if (expression == null)
                return null;
            while (expression is VBSyntax.ParenthesizedExpressionSyntax pes) {
                expression = pes.Expression;
            }
            return expression;
        }

        public static CS.Syntax.ExpressionSyntax SkipOutOfParens(this CS.Syntax.ExpressionSyntax expression)
        {
            if (expression == null)
                return null;
            while (expression is CS.Syntax.ParenthesizedExpressionSyntax pes) {
                expression = pes.Parent as CS.Syntax.ExpressionSyntax;
            }
            return expression;
        }

        public static VBSyntax.ExpressionSyntax SkipOutOfParens(this VBSyntax.ExpressionSyntax expression)
        {
            if (expression == null)
                return null;
            while (expression is VBSyntax.ParenthesizedExpressionSyntax pes) {
                expression = pes.Parent as VBSyntax.ExpressionSyntax;
            }
            return expression;
        }

        public static bool IsParentKind(this SyntaxNode node, CS.SyntaxKind kind)
        {
            return node != null && node.Parent.IsKind(kind);
        }

        public static bool IsParentKind(this SyntaxNode node, VBasic.SyntaxKind kind)
        {
            return node?.Parent.IsKind(kind) == true;
        }

        public static bool IsParentKind(this SyntaxToken node, CS.SyntaxKind kind)
        {
            return node.Parent?.IsKind(kind) == true;
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
                switch (CS.CSharpExtensions.Kind(declSpace)) {
                    // These are declaration-space-defining syntaxes, by the spec:
                    case CS.SyntaxKind.MethodDeclaration:
                    case CS.SyntaxKind.IndexerDeclaration:
                    case CS.SyntaxKind.OperatorDeclaration:
                    case CS.SyntaxKind.ConstructorDeclaration:
                    case CS.SyntaxKind.Block:
                    case CS.SyntaxKind.ParenthesizedLambdaExpression:
                    case CS.SyntaxKind.SimpleLambdaExpression:
                    case CS.SyntaxKind.AnonymousMethodExpression:
                    case CS.SyntaxKind.SwitchStatement:
                    case CS.SyntaxKind.ForEachKeyword:
                    case CS.SyntaxKind.ForStatement:
                    case CS.SyntaxKind.UsingStatement:

                    // SPEC VIOLATION: We also want to stop walking out if, say, we are in a field
                    // initializer. Technically according to the wording of the spec it should be
                    // legal to use a simple name inconsistently inside a field initializer because
                    // it does not define a local variable declaration space. In practice of course
                    // we want to check for that. (As the native compiler does as well.)

                    case CS.SyntaxKind.FieldDeclaration:
                        return declSpace;
                }
            }

            return null;
        }
    }
}

