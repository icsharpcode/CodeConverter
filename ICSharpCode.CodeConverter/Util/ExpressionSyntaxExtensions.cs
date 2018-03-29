using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;

namespace ICSharpCode.CodeConverter.Util
{
#if NR6
    public
#endif
    static class ExpressionSyntaxExtensions
    {

        /// <summary>
        /// Returns the predefined keyword kind for a given specialtype.
        /// </summary>
        /// <param name="specialType">The specialtype of this type.</param>
        /// <returns>The keyword kind for a given special type, or SyntaxKind.None if the type name is not a predefined type.</returns>
        public static SyntaxKind GetPredefinedKeywordKind(this SpecialType specialType)
        {
            switch (specialType) {
                case SpecialType.System_Boolean:
                    return SyntaxKind.BoolKeyword;
                case SpecialType.System_Byte:
                    return SyntaxKind.ByteKeyword;
                case SpecialType.System_SByte:
                    return SyntaxKind.SByteKeyword;
                case SpecialType.System_Int32:
                    return SyntaxKind.IntKeyword;
                case SpecialType.System_UInt32:
                    return SyntaxKind.UIntKeyword;
                case SpecialType.System_Int16:
                    return SyntaxKind.ShortKeyword;
                case SpecialType.System_UInt16:
                    return SyntaxKind.UShortKeyword;
                case SpecialType.System_Int64:
                    return SyntaxKind.LongKeyword;
                case SpecialType.System_UInt64:
                    return SyntaxKind.ULongKeyword;
                case SpecialType.System_Single:
                    return SyntaxKind.FloatKeyword;
                case SpecialType.System_Double:
                    return SyntaxKind.DoubleKeyword;
                case SpecialType.System_Decimal:
                    return SyntaxKind.DecimalKeyword;
                case SpecialType.System_String:
                    return SyntaxKind.StringKeyword;
                case SpecialType.System_Char:
                    return SyntaxKind.CharKeyword;
                case SpecialType.System_Object:
                    return SyntaxKind.ObjectKeyword;
                case SpecialType.System_Void:
                    return SyntaxKind.VoidKeyword;
                default:
                    return SyntaxKind.None;
            }
        }

        public static ArgumentListSyntax CreateArgList(params ExpressionSyntax[] args)
        {
            return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(args.Select(SyntaxFactory.Argument)));
        }

        public static VBSyntax.ArgumentListSyntax CreateArgList(params VBSyntax.ExpressionSyntax[] args)
        {
            return VBasic.SyntaxFactory.ArgumentList(VBasic.SyntaxFactory.SeparatedList(args.Select(e => (VBSyntax.ArgumentSyntax) VBasic.SyntaxFactory.SimpleArgument(e))));
        }

        public static bool HasOperandOfUnconvertedType(this Microsoft.CodeAnalysis.VisualBasic.Syntax.BinaryExpressionSyntax node, string operandType, SemanticModel semanticModel)
        {
            return new[] {node.Left, node.Right}.Any(e => UnconvertedIsType(e, operandType, semanticModel));
        }

        public static bool UnconvertedIsType(Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax e, string fullTypeName, SemanticModel semanticModel)
        {
            return semanticModel.GetTypeInfo(e).Type?.GetFullMetadataName() == fullTypeName;
        }
    }
}
