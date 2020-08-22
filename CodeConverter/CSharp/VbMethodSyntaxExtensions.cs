using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class VbMethodSyntaxExtensions
    {
        /// <summary>
        /// Use in conjunction with <see cref="IMethodSymbolExtensions.ReturnsVoidOrAsyncTask(IMethodSymbol)" />
        /// </summary>
        public static bool MustReturn(this VBSyntax.MethodBlockBaseSyntax node)
        {
            return !IsIterator(node) && node.IsKind(VBasic.SyntaxKind.FunctionBlock, VBasic.SyntaxKind.GetAccessorBlock)
                && !node.IsIterator();
        }

        public static bool IsIterator(this VBSyntax.MethodBlockBaseSyntax node)
        {
            return GetMethodBlock(node).HasModifier(VBasic.SyntaxKind.IteratorKeyword);
        }

        public static bool HasModifier(this VBSyntax.MethodBaseSyntax d, VBasic.SyntaxKind modifierKind)
        {
            return d.Modifiers.Any(m => SyntaxTokenExtensions.IsKind(m, modifierKind));
        }

        private static VBSyntax.MethodBaseSyntax GetMethodBlock(VBSyntax.MethodBlockBaseSyntax node)
        {
            return node.IsKind(VBasic.SyntaxKind.GetAccessorBlock) ? node.GetAncestor<VBSyntax.PropertyBlockSyntax>().PropertyStatement : node.BlockStatement;
        }
    }
}
