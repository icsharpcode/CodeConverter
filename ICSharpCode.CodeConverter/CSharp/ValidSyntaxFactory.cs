using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    /// <summary>
    /// As of VS2019 Preview 3, Roslyn's SyntaxFactory no longer aims to provide a valid syntax tree in the default case.
    /// Not passing some elements no longer always means "use the thing that works in most cases", but "don't use one at all" for some special cases.
    /// I'll add those special methods here as they come up.
    /// </summary>
    public static class ValidSyntaxFactory
    {
        /// <summary>
        /// As of VS2019 Preview 3 this is required since not passing these arguments now means "I don't want any parentheses"
        /// https://github.com/dotnet/roslyn/issues/33685
        /// https://github.com/icsharpcode/CodeConverter/issues/246
        /// </summary>
        public static SwitchStatementSyntax SwitchStatement(ExpressionSyntax expr, List<SwitchSectionSyntax> sections)
        {
            return SyntaxFactory.SwitchStatement(SyntaxFactory.Token(SyntaxKind.SwitchKeyword),
                SyntaxFactory.Token(SyntaxKind.OpenParenToken), expr,
                SyntaxFactory.Token(SyntaxKind.CloseParenToken), SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                SyntaxFactory.List(sections), SyntaxFactory.Token(SyntaxKind.CloseBraceToken));
        }

        public static ExpressionSyntax MemberAccess(params string[] nameParts)
        {
            ExpressionSyntax lhs = null;
            foreach (var namePart in nameParts) {
                if (lhs == null) lhs = SyntaxFactory.IdentifierName(namePart);
                else {
                    lhs = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        lhs, SyntaxFactory.IdentifierName(namePart));
                }
            }

            return lhs;
        }
        
        public static CastExpressionSyntax CastExpression(TypeSyntax type, ExpressionSyntax expressionSyntax)
        {
            return SyntaxFactory.CastExpression(type, expressionSyntax);
        }
    }
}