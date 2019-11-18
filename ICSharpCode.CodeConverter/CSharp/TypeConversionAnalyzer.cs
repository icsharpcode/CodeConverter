using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.VisualBasic.CompilerServices;
using CastExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.CastExpressionSyntax;
using Conversion = Microsoft.CodeAnalysis.VisualBasic.Conversion;
using ExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax;
using IdentifierNameSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax;
using InvocationExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax;
using MemberAccessExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using TypeSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class TypeConversionAnalyzer
    {
        private readonly CSharpCompilation _csCompilation;
        private readonly SemanticModel _semanticModel;
        private readonly HashSet<string> _extraUsingDirectives;
        private readonly SyntaxGenerator _csSyntaxGenerator;
        private static readonly Dictionary<string, MethodInfo> ConversionsTypeFullNames = GetConversionsMethodsByTypeFullName();
        private static readonly Dictionary<string, MethodInfo> ConversionsMethodNames = ConversionsTypeFullNames.Values.Concat(GetStringsMethods()).ToDictionary(m => m.Name, m => m);
        private readonly VisualBasicEqualityComparison _visualBasicEqualityComparison;

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

        public TypeConversionAnalyzer(SemanticModel semanticModel, CSharpCompilation csCompilation,
            HashSet<string> extraUsingDirectives, SyntaxGenerator csSyntaxGenerator, VisualBasicEqualityComparison visualBasicEqualityComparison)
        {
            _semanticModel = semanticModel;
            _csCompilation = csCompilation;
            _extraUsingDirectives = extraUsingDirectives;
            _csSyntaxGenerator = csSyntaxGenerator;
            _visualBasicEqualityComparison = visualBasicEqualityComparison;
        }

        public ExpressionSyntax AddExplicitConversion(Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax vbNode, ExpressionSyntax csNode, bool addParenthesisIfNeeded = true, bool alwaysExplicit = false, bool isConst = false)
        {
            var conversionKind = AnalyzeConversion(vbNode, alwaysExplicit, isConst);
            csNode = addParenthesisIfNeeded && (conversionKind == TypeConversionKind.DestructiveCast || conversionKind == TypeConversionKind.NonDestructiveCast)
                ? VbSyntaxNodeExtensions.ParenthesizeIfPrecedenceCouldChange(vbNode, csNode)
                : csNode;
            return AddExplicitConversion(vbNode, csNode, conversionKind, addParenthesisIfNeeded);
        }

        public ExpressionSyntax AddExplicitConversion(Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax vbNode, ExpressionSyntax csNode, TypeConversionKind conversionKind, bool addParenthesisIfNeeded = false)
        {
            var vbConvertedType = ModelExtensions.GetTypeInfo(_semanticModel, vbNode).ConvertedType;
            switch (conversionKind)
            {
                case TypeConversionKind.Unknown:
                case TypeConversionKind.Identity:
                    return addParenthesisIfNeeded ? VbSyntaxNodeExtensions.ParenthesizeIfPrecedenceCouldChange(vbNode, csNode) : csNode;
                case TypeConversionKind.DestructiveCast:
                case TypeConversionKind.NonDestructiveCast:
                    return CreateCast(csNode, vbConvertedType);
                case TypeConversionKind.Conversion:
                    return AddExplicitConvertTo(vbNode, csNode, vbConvertedType);
                case TypeConversionKind.ConstConversion:
                    return ConstantFold(vbNode, vbConvertedType);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private ExpressionSyntax CreateCast(ExpressionSyntax csNode, ITypeSymbol vbConvertedType)
        {
            var typeName = (TypeSyntax) _csSyntaxGenerator.TypeExpression(vbConvertedType);
            if (csNode is CastExpressionSyntax cast && cast.Type.IsEquivalentTo(typeName)) {
                return csNode;
            }

            return ValidSyntaxFactory.CastExpression(typeName, csNode);
        }

        public TypeConversionKind AnalyzeConversion(Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax vbNode, bool alwaysExplicit = false, bool isConst = false)
        {
            var typeInfo = ModelExtensions.GetTypeInfo(_semanticModel, vbNode);
            var vbType = typeInfo.Type;
            var vbConvertedType = typeInfo.ConvertedType;
            if (vbType is null || vbConvertedType is null)
            {
                return TypeConversionKind.Unknown;
            }

            if (vbType.IsEnumType()) {
                if (vbConvertedType.IsNumericType()) {
                    return TypeConversionKind.NonDestructiveCast;
                } else if (vbType.Equals(vbConvertedType) ||
                            (vbConvertedType.IsNullable() && vbType.Equals(vbConvertedType.GetNullableUnderlyingType())) ||
                            vbConvertedType.SpecialType == SpecialType.System_Object) {
                    return TypeConversionKind.Identity;
                } else {
                    return TypeConversionKind.Conversion;
                }
            }

            var vbCompilation = (VisualBasicCompilation) _semanticModel.Compilation;
            var vbConversion = vbCompilation.ClassifyConversion(vbType, vbConvertedType);
            var csType = GetCSType(vbNode, vbType);
            var csConvertedType = _csCompilation.GetTypeByMetadataName(vbConvertedType.GetFullMetadataName());

            if (csType != null && csConvertedType != null &&
                TryAnalyzeCsConversion(vbNode, csType, csConvertedType, vbConversion, vbConvertedType, vbType, vbCompilation, isConst, out TypeConversionKind analyzeConversion)) {
                return analyzeConversion;
            }

            return AnalyzeVbConversion(alwaysExplicit, vbType, vbConvertedType, vbConversion);
        }

        private INamedTypeSymbol GetCSType(VBSyntax.ExpressionSyntax vbNode, ITypeSymbol vbType)
        {
            // C# does not have literals for short/ushort, so the actual type here is integer
            if (vbNode is VBSyntax.LiteralExpressionSyntax literal &&
                literal.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.NumericLiteralExpression) &&
                literal.Token.Text.EndsWith("S")) {
                return _csCompilation.GetSpecialType(SpecialType.System_Int32);
            }

            var csType = _csCompilation.GetTypeByMetadataName(vbType.GetFullMetadataName());

            return csType;
        }

        private bool TryAnalyzeCsConversion(Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax vbNode, INamedTypeSymbol csType,
            INamedTypeSymbol csConvertedType, Conversion vbConversion, ITypeSymbol vbConvertedType, ITypeSymbol vbType,
            VisualBasicCompilation vbCompilation, bool isConst, out TypeConversionKind typeConversionKind)
        {
            var csConversion = _csCompilation.ClassifyConversion(csType, csConvertedType);

            bool isConvertToString =
                        (vbConversion.IsString || vbConversion.IsReference && vbConversion.IsNarrowing)  && vbConvertedType.SpecialType == SpecialType.System_String;
            bool isArithmetic = vbNode.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.AddExpression,
                Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.SubtractExpression,
                Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.MultiplyExpression,
                Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.DivideExpression,
                Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.IntegerDivideExpression);
            if (!csConversion.Exists || csConversion.IsUnboxing) {
                if (ConvertStringToCharLiteral(vbNode as VBSyntax.LiteralExpressionSyntax, vbConvertedType, out _)) {
                    typeConversionKind = TypeConversionKind.Identity; // Already handled elsewhere by other usage of method
                    return true;
                }
                if (isConvertToString || vbConversion.IsNarrowing) {
                    typeConversionKind = isConst ? TypeConversionKind.ConstConversion : TypeConversionKind.Conversion;
                    return true;
                }
            } else if (vbConversion.IsWidening && vbConversion.IsNumeric && csConversion.IsImplicit &&
                       csConversion.IsNumeric) {
                // Safe overapproximation: A cast is really only needed to help resolve the overload for the operator/method used.
                // e.g. When VB "&" changes to C# "+", there are lots more overloads available that implicit casts could match.
                // e.g. sbyte * ulong uses the decimal * operator in VB. In C# it's ambiguous - see ExpressionTests.vb "TestMul".
                typeConversionKind = TypeConversionKind.NonDestructiveCast;
                return true;
            } else if (csConversion.IsExplicit && csConversion.IsEnumeration) {
                typeConversionKind = TypeConversionKind.NonDestructiveCast;
                return true;
            } else if (csConversion.IsExplicit && csConversion.IsNumeric && vbConversion.IsNarrowing && isConst) {
                typeConversionKind = TypeConversionKind.NonDestructiveCast;
                return true;
            } else if (csConversion.IsExplicit && vbConversion.IsNumeric && vbType.TypeKind != TypeKind.Enum) {
                typeConversionKind = isConst ? TypeConversionKind.ConstConversion : TypeConversionKind.Conversion;
                return true;
            } else if (csConversion.IsExplicit && vbConversion.IsIdentity && csConversion.IsNumeric && vbType.TypeKind != TypeKind.Enum) {
                typeConversionKind = isConst ? TypeConversionKind.ConstConversion : TypeConversionKind.Conversion;
                return true;
            } else if (isArithmetic) {
                var arithmeticConversion =
                    vbCompilation.ClassifyConversion(vbConvertedType,
                        vbCompilation.GetTypeByMetadataName("System.Int32"));
                if (arithmeticConversion.IsWidening && !arithmeticConversion.IsIdentity) {
                    typeConversionKind = isConst ? TypeConversionKind.ConstConversion : TypeConversionKind.Conversion;
                    return true;
                }
            } else if (isConvertToString && vbType.SpecialType ==  SpecialType.System_Object) {
                typeConversionKind = isConst ? TypeConversionKind.ConstConversion : TypeConversionKind.Conversion;
                return true;
            }

            typeConversionKind = TypeConversionKind.Unknown;
            return false;
        }

        private static TypeConversionKind AnalyzeVbConversion(bool alwaysExplicit, ITypeSymbol vbType,
            ITypeSymbol vbConvertedType, Conversion vbConversion)
        {
            if (vbType.Equals(vbConvertedType) || vbConversion.IsIdentity) {
                return TypeConversionKind.Identity;
            }

            if (vbConversion.IsNumeric && (vbType.IsEnumType() || vbConvertedType.IsEnumType())) {
                return TypeConversionKind.NonDestructiveCast;
            }
            if (alwaysExplicit) {
                return vbConversion.IsNarrowing ? TypeConversionKind.NonDestructiveCast : TypeConversionKind.DestructiveCast;
            }

            return TypeConversionKind.Unknown;
        }

        private ExpressionSyntax ConstantFold(VBSyntax.ExpressionSyntax vbNode, ITypeSymbol type)
        {
            var vbOperation = _semanticModel.GetOperation(vbNode);

            if (TryCompileTimeEvaluate(vbOperation, out var result) && ConversionsTypeFullNames.TryGetValue(type.GetFullMetadataName(), out var method)) {
                result = method.Invoke(null, new[] { result });
                return LiteralConversions.GetLiteralExpression(result);
            }

            throw new NotImplementedException("Cannot generate constant C# expression");
        }

        private bool TryCompileTimeEvaluate(IOperation vbOperation, out object result)
        {
            result = null;
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
                invocationOperation.Arguments.Single().Value.ConstantValue.HasValue)
            {
                result = conversionMethodInfo.Invoke(null,
                    new[] {invocationOperation.Arguments.Single().Value.ConstantValue.Value});

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


        public ExpressionSyntax AddExplicitConvertTo(Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax vbNode, ExpressionSyntax csNode, ITypeSymbol type)
        {
            var displayType = type.ToMinimalDisplayString(_semanticModel, vbNode.SpanStart);
            if (csNode is InvocationExpressionSyntax invoke &&
                invoke.Expression is MemberAccessExpressionSyntax expr &&
                expr.Expression is IdentifierNameSyntax name && name.Identifier.ValueText == "Conversions" &&
                expr.Name.Identifier.ValueText == $"To{displayType}") {
                return csNode;
            }

            if (!ConversionsTypeFullNames.TryGetValue(type.GetFullMetadataName(), out var methodId)) {
                return CreateCast(csNode, type);
            }

            // Need to use Conversions rather than Convert to match what VB does, eg. True -> -1
            _extraUsingDirectives.Add("Microsoft.VisualBasic.CompilerServices");
            var memberAccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Conversions"), SyntaxFactory.IdentifierName(methodId.Name));
            var arguments = SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(csNode)));
            return SyntaxFactory.InvocationExpression(memberAccess, arguments);
        }

        public enum TypeConversionKind
        {
            Unknown,
            Identity,
            DestructiveCast,
            NonDestructiveCast,
            Conversion,
            ConstConversion,
        }

        public static bool ConvertStringToCharLiteral(Microsoft.CodeAnalysis.VisualBasic.Syntax.LiteralExpressionSyntax node, ITypeSymbol convertedType,
            out char chr)
        {
            if (convertedType?.SpecialType == SpecialType.System_Char &&
                node?.Token.Value is string str &&
                str.Length == 1) {
                chr = str.Single();
                return true;
            }

            chr = default;
            return false;
        }
    }
}