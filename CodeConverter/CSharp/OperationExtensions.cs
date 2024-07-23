using ICSharpCode.CodeConverter.Util.FromRoslyn;
using Microsoft.CodeAnalysis.Operations;


namespace ICSharpCode.CodeConverter.CSharp;

internal static class OperationExtensions
{
    public static IOperation GetParentIgnoringConversions(this IOperation operation)
    {
        var parent = operation?.Parent;
        while (parent is IConversionOperation || parent is IParenthesizedOperation) {
            parent = parent.Parent;
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

    public static bool IsAssignableExpression(this IOperation operation)
    {
        switch (operation?.Kind) {
            case OperationKind.ArrayElementReference:
            case OperationKind.LocalReference:
            case OperationKind.ParameterReference:
            case OperationKind.FieldReference:
            case OperationKind.MethodReference:
            case OperationKind.EventReference:
            case OperationKind.InstanceReference:
            case OperationKind.DynamicMemberReference:
                return true;

            case OperationKind.PropertyReference:
                //a property might be RefReturn, if it's defined in a referenced C# assembly
                var prop = ((IPropertyReferenceOperation)operation).Property;
                return prop.ReturnsByRef || prop.ReturnsByRefReadonly;
        }

        return false;
    }
}