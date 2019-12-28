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

        public static IOperation GetIgnoringParentheses(this IOperation operation)
        {
            while (operation is IParenthesizedOperation) {
                operation = operation?.Parent;
            }

            return operation;
        }

        public static IOperation GetNonConversionOperation(this IOperation operation)
        {
            while (true) {
                switch (operation)
                {
                    case IConversionOperation co:
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