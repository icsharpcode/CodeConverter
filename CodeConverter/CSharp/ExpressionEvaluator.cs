using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.VisualBasic.CompilerServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class ExpressionEvaluator
    {
        public static IReadOnlyDictionary<string, MethodInfo> ConversionsTypeFullNames { get; } = GetConversionsMethodsByTypeFullName();
        public static IReadOnlyDictionary<string, MethodInfo> ConversionsMethodNames { get; } = ConversionsTypeFullNames.Values.Concat(GetStringsMethods()).ToDictionary(m => m.Name, m => m);
        private readonly VisualBasicEqualityComparison _visualBasicEqualityComparison;
        private readonly SemanticModel _semanticModel;

        public ExpressionEvaluator(SemanticModel semanticModel, VisualBasicEqualityComparison visualBasicEqualityComparison)
        {
            _semanticModel = semanticModel;
            _visualBasicEqualityComparison = visualBasicEqualityComparison;
        }

        private static Dictionary<string, MethodInfo> GetConversionsMethodsByTypeFullName()
        {
            return typeof(Conversions).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name.StartsWith("To") && m.GetParameters().Length == 1 && m.ReturnType?.FullName != null)
                .ToLookup(m => m.ReturnType.FullName, m => m)
                .ToDictionary(kvp => kvp.Key, GetMostGeneralOverload);
        }
        private static MethodInfo GetMostGeneralOverload(IGrouping<string, MethodInfo> kvp)
        {
            return kvp.OrderByDescending(mi => mi.GetParameters().First().ParameterType == typeof(object)).First();
        }

        private static IEnumerable<MethodInfo> GetStringsMethods()
        {
            return typeof(Strings).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .ToLookup(m => m.Name, m => m)
                .Where(g => g.Count() == 1).SelectMany(ms => ms);
        }

        public (ExpressionSyntax Expr, bool IsCorrectType) GetConstantOrNull(VBSyntax.ExpressionSyntax vbNode, ITypeSymbol type, TypeConversionAnalyzer.TypeConversionKind analyzedConversionKind, ExpressionSyntax csNode)
        {
            var vbOperation = _semanticModel.GetOperation(vbNode).SkipParens(true);

            // Guideline tradeoff: Usually would aim for erring on the side of correct runtime behaviour. But making lots of simple constants unreadable for the sake of an edge case that will turn into an easily fixed compile error seems overkill.
            // See https://github.com/icsharpcode/CodeConverter/blob/master/.github/CONTRIBUTING.md#deciding-what-the-output-should-be
            bool isExactType = analyzedConversionKind == TypeConversionAnalyzer.TypeConversionKind.Identity;
            if ((isExactType || analyzedConversionKind == TypeConversionAnalyzer.TypeConversionKind.NonDestructiveCast)
                && IsProbablyConstExpression(vbOperation)) return (csNode, isExactType);

            if (TryCompileTimeEvaluate(vbOperation, out var result)) {
                if (type.Name == "Char" && result is int resultInt) {
                    result = Strings.ChrW(resultInt);
                } else if (ConversionsTypeFullNames.TryGetValue(type.GetFullMetadataName(), out var method)) {
                    result = method.Invoke(null, new[] { result });
                }
                return (LiteralConversions.GetLiteralExpression(result, convertedType: type), true);
            }

            return (null, false);
        }

        /// <remarks>Deal with cases like "2*PI" without inlining the const</remarks>
        private bool IsProbablyConstExpression(IOperation op)
        {
            op = op.SkipParens(true);

            if (op is IFieldReferenceOperation fro && fro.Field.IsConst || op is ILocalReferenceOperation lro && lro.Local.IsConst || op is ILiteralOperation) {
                return true;
            }

            if (op is IBinaryOperation bo) {
                return IsProbablyConstExpression(bo.LeftOperand) && IsProbablyConstExpression(bo.RightOperand);
            }

            if (op is IUnaryOperation uo) {
                return IsProbablyConstExpression(uo.Operand);
            }

            return false;
        }

        private bool TryCompileTimeEvaluate(IOperation vbOperation, out object result)
        {
            if (vbOperation.ConstantValue.HasValue) {
                result = vbOperation.ConstantValue.Value;
                return true;
            }
            return TryCompileTimeEvaluateInvocation(vbOperation, out result) || TryCompileTimeEvaluateBinaryExpression(vbOperation, out result);
        }

        private bool TryCompileTimeEvaluateInvocation(IOperation vbOperation, out object result)
        {
            if (vbOperation is IInvocationOperation invocationOperation &&
                ConversionsMethodNames.TryGetValue(invocationOperation.TargetMethod.Name,
                    out var conversionMethodInfo) && invocationOperation.Arguments.Length == 1 &&
                invocationOperation.Arguments.Single().Value.ConstantValue.HasValue) {
                result = conversionMethodInfo.Invoke(null,
                    new[] { invocationOperation.Arguments.Single().Value.ConstantValue.Value });

                return true;
            }

            result = null;
            return false;
        }

        private bool TryCompileTimeEvaluateBinaryExpression(IOperation vbOperation, out object result)
        {
            result = null;
            if (vbOperation is IBinaryOperation binaryOperation && TryCompileTimeEvaluate(binaryOperation.LeftOperand, out var leftResult) && TryCompileTimeEvaluate(binaryOperation.RightOperand, out var rightResult)) {
                var textComparisonCaseInsensitive = _visualBasicEqualityComparison.OptionCompareTextCaseInsensitive;
                switch (binaryOperation.OperatorKind) {
                    case BinaryOperatorKind.Add:
                        result = Operators.AddObject(leftResult, rightResult);
                        break;
                    case BinaryOperatorKind.Subtract:
                        result = Operators.SubtractObject(leftResult, rightResult);
                        break;
                    case BinaryOperatorKind.Multiply:
                        result = Operators.MultiplyObject(leftResult, rightResult);
                        break;
                    case BinaryOperatorKind.Divide:
                        result = Operators.DivideObject(leftResult, rightResult);
                        break;
                    case BinaryOperatorKind.IntegerDivide:
                        result = Operators.IntDivideObject(leftResult, rightResult);
                        break;
                    case BinaryOperatorKind.Remainder:
                        result = Operators.ModObject(leftResult, rightResult);
                        break;
                    case BinaryOperatorKind.Power:
                        result = Operators.ExponentObject(leftResult, rightResult);
                        break;
                    case BinaryOperatorKind.LeftShift:
                        result = Operators.LeftShiftObject(leftResult, rightResult);
                        break;
                    case BinaryOperatorKind.RightShift:
                        result = Operators.RightShiftObject(leftResult, rightResult);
                        break;
                    case BinaryOperatorKind.And:
                        result = Operators.AndObject(leftResult, rightResult);
                        break;
                    case BinaryOperatorKind.Or:
                        result = Operators.OrObject(leftResult, rightResult);
                        break;
                    case BinaryOperatorKind.ExclusiveOr:
                        result = Operators.XorObject(leftResult, rightResult);
                        break;
                    case BinaryOperatorKind.Concatenate:
                        result = Operators.ConcatenateObject(leftResult, rightResult);
                        break;
                    case BinaryOperatorKind.Equals:
                    case BinaryOperatorKind.ObjectValueEquals:
                        result = Operators.CompareObjectEqual(leftResult, rightResult, textComparisonCaseInsensitive);
                        break;
                    case BinaryOperatorKind.NotEquals:
                    case BinaryOperatorKind.ObjectValueNotEquals:
                        result = Operators.CompareObjectNotEqual(leftResult, rightResult, textComparisonCaseInsensitive);
                        break;
                    case BinaryOperatorKind.LessThan:
                        result = Operators.CompareObjectLess(leftResult, rightResult, textComparisonCaseInsensitive);
                        break;
                    case BinaryOperatorKind.LessThanOrEqual:
                        result = Operators.CompareObjectLessEqual(leftResult, rightResult, textComparisonCaseInsensitive);
                        break;
                    case BinaryOperatorKind.GreaterThanOrEqual:
                        result = Operators.CompareObjectGreaterEqual(leftResult, rightResult, textComparisonCaseInsensitive);
                        break;
                    case BinaryOperatorKind.GreaterThan:
                        result = Operators.CompareObjectGreater(leftResult, rightResult, textComparisonCaseInsensitive);
                        break;
                    default:
                        return false;

                }

                return true;
            }
            return false;
        }
    }
}