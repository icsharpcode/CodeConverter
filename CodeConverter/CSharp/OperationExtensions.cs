using System.Linq;
using ICSharpCode.CodeConverter.Util.FromRoslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;


namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class OperationExtensions
    {
        public static IOperation GetParentIgnoringConversions(this IOperation operation)
        {
            var parent = operation?.Parent;
            while (parent is IConversionOperation || parent is IParenthesizedOperation) {
                parent = parent?.Parent;
            }

            return parent;
        }

        public static IOperation SkipParens(this IOperation operation, bool skipImplicitNumericConvert = false)
        {
            while (true) {
                switch (operation)
                {
                    case IConversionOperation co when skipImplicitNumericConvert && co.IsImplicit && co.Conversion.IsNumeric && !co.Conversion.IsUserDefined && co.Operand.Type.IsNumericType() && co.Type.IsNumericType():
                        operation = co.Operand;
                        break;
                    case IParenthesizedOperation po:
                        operation = po.Operand;
                        break;
                    default:
                        return operation;
                }
            }
        }

        public static bool IsPropertyElementAccess(this IOperation operation)
        {
            return operation is IPropertyReferenceOperation pro && pro.Arguments.Any() && VBasic.VisualBasicExtensions.IsDefault(pro.Property);
        }

        public static bool IsArrayElementAccess(this IOperation operation)
        {
            return operation != null && operation.Kind == OperationKind.ArrayElementReference;
        }
    }
}