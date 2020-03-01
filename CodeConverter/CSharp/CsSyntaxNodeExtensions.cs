using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class CsSyntaxNodeExtensions
    {
        public static bool IsGlobalId(this SyntaxNode node)
        {
            return node is IdentifierNameSyntax ins && ins.Identifier.IsKind(SyntaxKind.GlobalKeyword);
        }
    }
}
