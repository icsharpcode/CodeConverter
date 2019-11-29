using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class OperationExtensions
    {
        public static IOperation GetParentIgnoringConversions(this IOperation operation)
        {
            var parent = operation?.Parent;
            do {
                parent = parent?.Parent;
            } while (parent is IConversionOperation || parent is IParenthesizedOperation);

            return parent;
        }
    }
}