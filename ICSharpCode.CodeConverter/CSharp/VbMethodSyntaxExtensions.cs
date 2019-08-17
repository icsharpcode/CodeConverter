using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class VbMethodSyntaxExtensions
    {
        public static bool AllowsImplicitReturn(this Microsoft.CodeAnalysis.VisualBasic.Syntax.MethodBlockBaseSyntax node)
        {
            return !IsIterator(node) && node.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.FunctionBlock, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.GetAccessorBlock);
        }

        public static bool IsIterator(this Microsoft.CodeAnalysis.VisualBasic.Syntax.MethodBlockBaseSyntax node)
        {
            var modifiableNode = node.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.GetAccessorBlock) ? node.GetAncestor<Microsoft.CodeAnalysis.VisualBasic.Syntax.PropertyBlockSyntax>().PropertyStatement : node.BlockStatement;
            return HasIteratorModifier(modifiableNode);
        }

        private static bool HasIteratorModifier(this Microsoft.CodeAnalysis.VisualBasic.Syntax.MethodBaseSyntax d)
        {
            return d.Modifiers.Any(m => SyntaxTokenExtensions.IsKind(m, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.IteratorKeyword));
        }
    }
}