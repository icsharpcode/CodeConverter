using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

        /// <summary>
        /// If the position is inside of token, return that token; otherwise, return the token to the left.
        /// </summary>
        public static SyntaxToken FindTokenOnLeftOfPosition(
            this SyntaxNode root,
            int position,
            bool includeSkipped = true,
            bool includeDirectives = false,
            bool includeDocumentationComments = false)
        {
            var skippedTokenFinder = includeSkipped ? s_findSkippedTokenBackward : (Func<SyntaxTriviaList, int, SyntaxToken>)null;

            return FindTokenHelper.FindTokenOnLeftOfPosition<CompilationUnitSyntax>(
                root, position, skippedTokenFinder, includeSkipped, includeDirectives, includeDocumentationComments);
        }



        //        public static bool IntersectsWith(this SyntaxToken token, int position)
        //        {
        //            return token.Span.IntersectsWith(position);
        //        }

        //        public static bool IsLeftSideOfDot(this ExpressionSyntax syntax)
        //        {
        //            return (bool)isLeftSideOfDotMethod.Invoke(null, new object[] { syntax });
        //        }
        //
        //        public static bool IsRightSideOfDot(this ExpressionSyntax syntax)
        //        {
        //            return (bool)isRightSideOfDotMethod.Invoke(null, new object[] { syntax });
        //        }

        //        public static INamedTypeSymbol GetEnclosingNamedType(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        //        {
        //            return (INamedTypeSymbol)getEnclosingNamedTypeMethod.Invoke(null, new object[] { semanticModel, position, cancellationToken });
        //        }
        //


        public static ExpressionSyntax SkipParens(this ExpressionSyntax expression)
        {
            if (expression == null)
                return null;
            while (expression != null && expression.IsKind(SyntaxKind.ParenthesizedExpression)) {
                expression = ((ParenthesizedExpressionSyntax)expression).Expression;
            }
            return expression;
        }

        public static SyntaxNode SkipArgument(this SyntaxNode expression)
        {
            if (expression is ArgumentSyntax)
                return ((ArgumentSyntax)expression).Expression;
            if (expression is Microsoft.CodeAnalysis.VisualBasic.Syntax.ArgumentSyntax)
                return ((Microsoft.CodeAnalysis.VisualBasic.Syntax.ArgumentSyntax)expression).GetExpression();
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

        public static INamedTypeSymbol GetEnclosingNamedType(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            return semanticModel.GetEnclosingSymbol<INamedTypeSymbol>(position, cancellationToken);
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

        private static bool IsThisOrTypeOrNamespace(MemberAccessExpressionSyntax memberAccess, SemanticModel semanticModel)
        {
            if (memberAccess.Expression.Kind() == SyntaxKind.ThisExpression) {
                var previousToken = memberAccess.Expression.GetFirstToken().GetPreviousToken();

                var symbol = semanticModel.GetSymbolInfo(memberAccess.Name).Symbol;

                if (previousToken.Kind() == SyntaxKind.OpenParenToken &&
                    previousToken.IsParentKind(SyntaxKind.ParenthesizedExpression) &&
                    !previousToken.Parent.IsParentKind(SyntaxKind.ParenthesizedExpression) &&
                    ((ParenthesizedExpressionSyntax)previousToken.Parent).Expression.Kind() == SyntaxKind.SimpleMemberAccessExpression &&
                    symbol != null && symbol.Kind == SymbolKind.Method) {
                    return false;
                }

                return true;
            }

            var expressionInfo = semanticModel.GetSymbolInfo(memberAccess.Expression);
            if (IsValidSymbolInfo(expressionInfo.Symbol)) {
                if (expressionInfo.Symbol is INamespaceOrTypeSymbol) {
                    return true;
                }

                if (expressionInfo.Symbol.IsThisParameter()) {
                    return true;
                }
            }

            return false;
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

        private static bool ParserWouldTreatExpressionAsCast(ExpressionSyntax reducedNode, MemberAccessExpressionSyntax originalNode)
        {
            SyntaxNode parent = originalNode;
            while (parent != null) {
                if (parent.IsParentKind(SyntaxKind.SimpleMemberAccessExpression)) {
                    parent = parent.Parent;
                    continue;
                }

                if (!parent.IsParentKind(SyntaxKind.ParenthesizedExpression)) {
                    return false;
                }

                break;
            }

            var newExpression = parent.ReplaceNode((SyntaxNode)originalNode, reducedNode);

            // detect cast ambiguities according to C# spec #7.7.6 
            if (IsNameOrMemberAccessButNoExpression(newExpression)) {
                var nextToken = parent.Parent.GetLastToken().GetNextToken();

                return nextToken.Kind() == SyntaxKind.OpenParenToken ||
                nextToken.Kind() == SyntaxKind.TildeToken ||
                nextToken.Kind() == SyntaxKind.ExclamationToken ||
                (SyntaxFacts.IsKeywordKind(nextToken.Kind()) && !(nextToken.Kind() == SyntaxKind.AsKeyword || nextToken.Kind() == SyntaxKind.IsKeyword));
            }

            return false;
        }

        private static bool IsMemberAccessADynamicInvocation(MemberAccessExpressionSyntax memberAccess, SemanticModel semanticModel)
        {
            var ancestorInvocation = memberAccess.FirstAncestorOrSelf<InvocationExpressionSyntax>();

            if (ancestorInvocation != null && ancestorInvocation.SpanStart == memberAccess.SpanStart) {
                var typeInfo = semanticModel.GetTypeInfo(ancestorInvocation);
                if (typeInfo.Type != null &&
                    typeInfo.Type.Kind == SymbolKind.DynamicType) {
                    return true;
                }
            }

            return false;
        }

        private static bool IsNameOrMemberAccessButNoExpression(SyntaxNode node)
        {
            if (node.IsKind(SyntaxKind.SimpleMemberAccessExpression)) {
                var memberAccess = (MemberAccessExpressionSyntax)node;

                return memberAccess.Expression.IsKind(SyntaxKind.IdentifierName) ||
                IsNameOrMemberAccessButNoExpression(memberAccess.Expression);
            }

            return node.IsKind(SyntaxKind.IdentifierName);
        }

        private static bool AccessMethodWithDynamicArgumentInsideStructConstructor(this MemberAccessExpressionSyntax memberAccess, SemanticModel semanticModel)
        {
            var constructor = memberAccess.Ancestors().OfType<ConstructorDeclarationSyntax>().SingleOrDefault();

            if (constructor == null || constructor.Parent.Kind() != SyntaxKind.StructDeclaration) {
                return false;
            }

            return semanticModel.GetSymbolInfo(memberAccess.Name).CandidateReason == CandidateReason.LateBound;
        }

        private static bool IsNullableTypeInPointerExpression(ExpressionSyntax expression, ExpressionSyntax simplifiedNode)
        {
            // Note: nullable type syntax is not allowed in pointer type syntax
            if (simplifiedNode.Kind() == SyntaxKind.NullableType &&
                simplifiedNode.DescendantNodes().Any(n => n is PointerTypeSyntax)) {
                return true;
            }

            return false;
        }

        private static bool IsAmbiguousCast(ExpressionSyntax expression, ExpressionSyntax simplifiedNode)
        {
            // Can't simplify a type name in a cast expression if it would then cause the cast to be
            // parsed differently.  For example:  (Foo::Bar)+1  is a cast.  But if that simplifies to
            // (Bar)+1  then that's an arithmetic expression.
            if (expression.IsParentKind(SyntaxKind.CastExpression)) {
                var castExpression = (CastExpressionSyntax)expression.Parent;
                if (castExpression.Type == expression) {
                    var newCastExpression = castExpression.ReplaceNode((SyntaxNode)castExpression.Type, simplifiedNode);
                    var reparsedCastExpression = SyntaxFactory.ParseExpression(newCastExpression.ToString());

                    if (!reparsedCastExpression.IsKind(SyntaxKind.CastExpression)) {
                        return true;
                    }
                }
            }

            return false;
        }
        private static bool IsNonNameSyntaxInUsingDirective(ExpressionSyntax expression, ExpressionSyntax simplifiedNode)
        {
            return
                expression.IsParentKind(SyntaxKind.UsingDirective) &&
                !(simplifiedNode is NameSyntax);
        }

        private static bool IsNotNullableReplacable(this NameSyntax name, TypeSyntax reducedName)
        {
            var isNotNullableReplacable = false;
            // var isLeftSideOfDot = name.IsLeftSideOfDot();
            // var isRightSideOfDot = name.IsRightSideOfDot();

            if (reducedName.Kind() == SyntaxKind.NullableType) {
                if (((NullableTypeSyntax)reducedName).ElementType.Kind() == SyntaxKind.OmittedTypeArgument) {
                    isNotNullableReplacable = true;
                } else {
                    isNotNullableReplacable = name.IsLeftSideOfDot() || name.IsRightSideOfDot();
                }
            }

            return isNotNullableReplacable;
        }

        public static SyntaxTokenList GetModifiers(this MemberDeclarationSyntax member)
        {
            if (member == null)
                throw new ArgumentNullException("member");
            var method = member as BaseMethodDeclarationSyntax;
            if (method != null)
                return method.Modifiers;
            var property = member as BasePropertyDeclarationSyntax;
            if (property != null)
                return property.Modifiers;
            var field = member as BaseFieldDeclarationSyntax;
            if (field != null)
                return field.Modifiers;
            return new SyntaxTokenList();
        }

        public static ExplicitInterfaceSpecifierSyntax GetExplicitInterfaceSpecifierSyntax(this MemberDeclarationSyntax member)
        {
            if (member == null)
                throw new ArgumentNullException("member");
            var method = member as MethodDeclarationSyntax;
            if (method != null)
                return method.ExplicitInterfaceSpecifier;
            var property = member as BasePropertyDeclarationSyntax;
            if (property != null)
                return property.ExplicitInterfaceSpecifier;
            var evt = member as EventDeclarationSyntax;
            if (evt != null)
                return evt.ExplicitInterfaceSpecifier;
            return null;
        }
        //        public static bool IsKind(this SyntaxToken token, SyntaxKind kind)
        //        {
        //            return token.RawKind == (int)kind;
        //        }
        //
        //        public static bool IsKind(this SyntaxTrivia trivia, SyntaxKind kind)
        //        {
        //            return trivia.RawKind == (int)kind;
        //        }
        //
        //        public static bool IsKind(this SyntaxNode node, SyntaxKind kind)
        //        {
        //            return node?.RawKind == (int)kind;
        //        }
        //
        //        public static bool IsKind(this SyntaxNodeOrToken nodeOrToken, SyntaxKind kind)
        //        {
        //            return nodeOrToken.RawKind == (int)kind;
        //        }
        //

        //        public static SyntaxNode GetParent(this SyntaxNode node)
        //        {
        //            return node != null ? node.Parent : null;
        //        }
        public static bool HasOperandOfUnconvertedType(this Microsoft.CodeAnalysis.VisualBasic.Syntax.AssignmentStatementSyntax node, string operandType, SemanticModel semanticModel)
        {
            return new[] { node.Left, node.Right }.Any(e => ExpressionSyntaxExtensions.UnconvertedIsType(e, operandType, semanticModel));
        }
    }
}

