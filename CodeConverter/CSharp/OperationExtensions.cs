using ICSharpCode.CodeConverter.Util.FromRoslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class OperationExtensions
    {
        public static IOperation GetParentIgnoringConversions(this IOperation operation)
        {
            var parent = operation?.Parent; //TODO This appears to skip up two layers, rename or change logic
            do {
                parent = parent?.Parent;
            } while (parent is IConversionOperation || parent is IParenthesizedOperation);

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
    }
}