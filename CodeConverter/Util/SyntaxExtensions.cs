using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.Util
{
    internal static class SyntaxExtensions
    {

        /// <summary>
        /// return only skipped tokens
        /// </summary>
        private static IEnumerable<SyntaxToken> GetSkippedTokens(SyntaxTriviaList list)
        {
            return list.Where(trivia => trivia.RawKind == (int)SyntaxKind.SkippedTokensTrivia)
                .SelectMany(t => ((SkippedTokensTriviaSyntax)t.GetStructure()).Tokens);
        }

        public static Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax SkipParens(this Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax expression)
        {
            if (expression == null)
                return null;
            while (expression is Microsoft.CodeAnalysis.CSharp.Syntax.ParenthesizedExpressionSyntax pes) {
                expression = pes.Expression;
            }
            return expression;
        }

        public static Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax SkipParens(this Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax expression)
        {
            if (expression == null)
                return null;
            while (expression is Microsoft.CodeAnalysis.VisualBasic.Syntax.ParenthesizedExpressionSyntax pes) {
                expression = pes.Expression;
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
    }
}

