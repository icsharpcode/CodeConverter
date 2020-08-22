using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ICSharpCode.CodeConverter.Util;
using ICSharpCode.CodeConverter.Util.FromRoslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Operations;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    /// <remarks>See https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/conversions </remarks>
    internal class TypeConversionAnalyzer
    {
        private readonly CSharpCompilation _csCompilation;
        private readonly SemanticModel _semanticModel;
        private readonly HashSet<string> _extraUsingDirectives;
        private readonly SyntaxGenerator _csSyntaxGenerator;
        private readonly ExpressionEvaluator _expressionEvaluator;

        public TypeConversionAnalyzer(SemanticModel semanticModel, CSharpCompilation csCompilation,
            HashSet<string> extraUsingDirectives, SyntaxGenerator csSyntaxGenerator, ExpressionEvaluator expressionEvaluator)
        {
            _semanticModel = semanticModel;
            _csCompilation = csCompilation;
            _extraUsingDirectives = extraUsingDirectives;
            _csSyntaxGenerator = csSyntaxGenerator;
            _expressionEvaluator = expressionEvaluator;
        }

        public ExpressionSyntax AddExplicitConversion(Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax vbNode, ExpressionSyntax csNode, bool addParenthesisIfNeeded = true, bool defaultToCast = false, bool isConst = false, ITypeSymbol forceSourceType = null, ITypeSymbol forceTargetType = null)
        {
            if (csNode == null) return null;
            var conversionKind = AnalyzeConversion(vbNode, defaultToCast, isConst, forceSourceType, forceTargetType);
            csNode = addParenthesisIfNeeded && (conversionKind == TypeConversionKind.DestructiveCast || conversionKind == TypeConversionKind.NonDestructiveCast)
                ? VbSyntaxNodeExtensions.ParenthesizeIfPrecedenceCouldChange(vbNode, csNode)
                : csNode;
            return AddExplicitConversion(vbNode, csNode, conversionKind, addParenthesisIfNeeded, isConst, forceSourceType: forceSourceType, forceTargetType: forceTargetType).Expr;
        }

        public (ExpressionSyntax Expr, bool IsConst) AddExplicitConversion(Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax vbNode, ExpressionSyntax csNode, TypeConversionKind conversionKind, bool addParenthesisIfNeeded = false, bool requiresConst = false, ITypeSymbol forceSourceType = null, ITypeSymbol forceTargetType = null)
        {
            var typeInfo = _semanticModel.GetTypeInfo(vbNode);
            var vbType = forceSourceType ?? typeInfo.Type;
            var vbConvertedType = forceTargetType ?? typeInfo.ConvertedType;
            bool resultConst = false;

            if (requiresConst) {
                var (constExpression, isCorrectType) = _expressionEvaluator.GetConstantOrNull(vbNode, vbConvertedType, conversionKind, csNode);
                if (isCorrectType) {
                    return (constExpression, true);
                }
                if (constExpression != null) {
                    csNode = constExpression ?? csNode;
                    resultConst = true;
                }
            }

            var typeConvertedResult = AddTypeConversion(vbNode, csNode, conversionKind, addParenthesisIfNeeded, vbType, vbConvertedType);
            return (typeConvertedResult, resultConst);
        }

        private ExpressionSyntax AddTypeConversion(VBSyntax.ExpressionSyntax vbNode, ExpressionSyntax csNode, TypeConversionKind conversionKind, bool addParenthesisIfNeeded, ITypeSymbol vbType, ITypeSymbol vbConvertedType)
        {
            switch (conversionKind) {
                case TypeConversionKind.EnumConversionThenCast:
                    var underlyingType = ((INamedTypeSymbol) vbConvertedType).EnumUnderlyingType;
                    csNode = AddTypeConversion(vbNode, csNode, TypeConversionKind.Conversion, addParenthesisIfNeeded, vbType, underlyingType);
                    return AddTypeConversion(vbNode, csNode, TypeConversionKind.NonDestructiveCast, addParenthesisIfNeeded, underlyingType, vbConvertedType);
                case TypeConversionKind.EnumCastThenConversion:
                    var enumUnderlyingType = ((INamedTypeSymbol) vbType).EnumUnderlyingType;
                    csNode = AddTypeConversion(vbNode, csNode, TypeConversionKind.NonDestructiveCast, addParenthesisIfNeeded, vbType, enumUnderlyingType);
                    return AddTypeConversion(vbNode, csNode, TypeConversionKind.Conversion, addParenthesisIfNeeded, enumUnderlyingType, vbConvertedType);
                case TypeConversionKind.Unknown:
                case TypeConversionKind.Identity:
                    return addParenthesisIfNeeded ? VbSyntaxNodeExtensions.ParenthesizeIfPrecedenceCouldChange(vbNode, csNode) : csNode;
                case TypeConversionKind.DestructiveCast:
                case TypeConversionKind.NonDestructiveCast:
                    return CreateCast(csNode, vbConvertedType);
                case TypeConversionKind.Conversion:
                    return AddExplicitConvertTo(vbNode, csNode, vbType, vbConvertedType); ;
                case TypeConversionKind.NullableBool:
                    return SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, csNode,
                        LiteralConversions.GetLiteralExpression(true));
                case TypeConversionKind.StringToCharArray:
                    var memberAccessExpressionSyntax = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, csNode, SyntaxFactory.IdentifierName(nameof(string.ToCharArray)));
                    return SyntaxFactory.InvocationExpression(memberAccessExpressionSyntax,
                        SyntaxFactory.ArgumentList());
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

        public TypeConversionKind AnalyzeConversion(Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax vbNode, bool alwaysExplicit = false, bool isConst = false, ITypeSymbol forceSourceType = null, ITypeSymbol forceTargetType = null)
        {
            var typeInfo = ModelExtensions.GetTypeInfo(_semanticModel, vbNode);
            var vbType = forceSourceType ?? typeInfo.Type;
            var vbConvertedType = forceTargetType ?? typeInfo.ConvertedType;
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
                } else if (vbConvertedType.SpecialType == SpecialType.System_String) {
                    return TypeConversionKind.EnumCastThenConversion;
                }else {
                    return TypeConversionKind.Conversion;
                }
            }

            var vbCompilation = (VBasic.VisualBasicCompilation) _semanticModel.Compilation;
            var vbConversion = vbCompilation.ClassifyConversion(vbType, vbConvertedType);
            var csType = GetCSType(vbType, vbNode);
            var csConvertedType = GetCSType(vbConvertedType);

            if (csType != null && csConvertedType != null &&
                TryAnalyzeCsConversion(vbNode, csType, csConvertedType, vbConversion, vbConvertedType, vbType, vbCompilation, isConst, out TypeConversionKind analyzeConversion)) {
                return analyzeConversion;
            }

            return AnalyzeVbConversion(alwaysExplicit, vbType, vbConvertedType, vbConversion);
        }

        private ITypeSymbol GetCSType(ITypeSymbol vbType, VBSyntax.ExpressionSyntax vbNode = null)
        {
            // C# does not have literals for short/ushort, so the actual type here is integer
            if (vbNode is VBSyntax.LiteralExpressionSyntax literal &&
                literal.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.NumericLiteralExpression) &&
                literal.Token.Text.EndsWith("S")) {
                return _csCompilation.GetSpecialType(SpecialType.System_Int32);
            }

            var csType = SymbolFinder.FindSimilarSymbols(vbType, _csCompilation).FirstOrDefault() ?? _csCompilation.GetTypeByMetadataName(vbType.GetFullMetadataName());

            return csType;
        }

        private bool TryAnalyzeCsConversion(Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax vbNode, ITypeSymbol csType,
            ITypeSymbol csConvertedType, Conversion vbConversion, ITypeSymbol vbConvertedType, ITypeSymbol vbType,
            VBasic.VisualBasicCompilation vbCompilation, bool isConst, out TypeConversionKind typeConversionKind)
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
                if (ConvertStringToCharLiteral(vbNode, vbType, vbConvertedType, out _)) {
                    typeConversionKind =
                        TypeConversionKind.Identity; // Already handled elsewhere by other usage of method
                    return true;
                }

                if (vbType.SpecialType == SpecialType.System_String && vbConvertedType.IsArrayOf(SpecialType.System_Char)) {
                    typeConversionKind = TypeConversionKind.StringToCharArray;
                    return true;
                }
                if (isConvertToString || vbConversion.IsNarrowing) {
                    typeConversionKind = vbConvertedType.IsEnumType() && !csConversion.Exists ? TypeConversionKind.EnumConversionThenCast : TypeConversionKind.Conversion;
                    return true;
                }
            } else if (vbConversion.IsWidening && vbConversion.IsNumeric && csConversion.IsImplicit &&
                       csConversion.IsNumeric) {
                // Safe overapproximation: A cast is really only needed to help resolve the overload for the operator/method used.
                // e.g. When VB "&" changes to C# "+", there are lots more overloads available that implicit casts could match.
                // e.g. sbyte * ulong uses the decimal * operator in VB. In C# it's ambiguous - see ExpressionTests.vb "TestMul".
                typeConversionKind = TypeConversionKind.NonDestructiveCast;
                return true;
            } else if (csConversion.IsExplicit && csConversion.IsEnumeration || csConversion.IsBoxing) {
                typeConversionKind = TypeConversionKind.NonDestructiveCast;
                return true;
            } else if (isArithmetic) {
                var arithmeticConversion =
                    vbCompilation.ClassifyConversion(vbConvertedType,
                        vbCompilation.GetTypeByMetadataName("System.Int32"));
                if (arithmeticConversion.IsWidening && !arithmeticConversion.IsIdentity) {
                    typeConversionKind = TypeConversionKind.Conversion;
                    return true;
                }
            } else if (csConversion.IsExplicit && csConversion.IsNumeric && vbConversion.IsNarrowing && isConst) {
                typeConversionKind = IsImplicitConstantConversion(vbNode) ? TypeConversionKind.Identity : TypeConversionKind.NonDestructiveCast;
                return true;
            } else if (csConversion.IsExplicit && vbConversion.IsNumeric && vbType.TypeKind != TypeKind.Enum) {
                typeConversionKind = IsImplicitConstantConversion(vbNode) ? TypeConversionKind.Identity :
                    TypeConversionKind.Conversion;
                return true;
            } else if (csConversion.IsExplicit && vbConversion.IsIdentity && csConversion.IsNumeric && vbType.TypeKind != TypeKind.Enum) {
                typeConversionKind = TypeConversionKind.Conversion;
                return true;
            } else if (isConvertToString && vbType.SpecialType == SpecialType.System_Object) {
                typeConversionKind = TypeConversionKind.Conversion;
                return true;
            } else if (csConversion.IsNullable && csConvertedType.SpecialType == SpecialType.System_Boolean) {
                typeConversionKind = TypeConversionKind.NullableBool;
                return true;
            } else if (csConversion.IsExplicit) {
                typeConversionKind = TypeConversionKind.DestructiveCast;
                return true;
            }

            typeConversionKind = csConversion.IsIdentity ? TypeConversionKind.Identity : TypeConversionKind.Unknown;
            return false;
        }

        private bool IsImplicitConstantConversion(VBSyntax.ExpressionSyntax vbNode)
        {
            return _semanticModel.GetOperation(vbNode).Parent is IConversionOperation co && co.IsImplicit && co.Operand.ConstantValue.HasValue;
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
            if (vbConversion.IsNarrowing) {
                return TypeConversionKind.DestructiveCast;
            }
            if (alwaysExplicit) {
                return TypeConversionKind.NonDestructiveCast;
            }

            return TypeConversionKind.Unknown;
        }

        public ExpressionSyntax AddExplicitConvertTo(Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax vbNode, ExpressionSyntax csNode, ITypeSymbol currentType, ITypeSymbol targetType)
        {
            var displayType = targetType.ToMinimalDisplayString(_semanticModel, vbNode.SpanStart);
            if (csNode is InvocationExpressionSyntax invoke &&
                invoke.Expression is MemberAccessExpressionSyntax expr &&
                expr.Expression is IdentifierNameSyntax name && name.Identifier.ValueText == "Conversions" &&
                expr.Name.Identifier.ValueText == $"To{displayType}") {
                return csNode;
            }

            if (GetToStringConversionOrNull(csNode, currentType, targetType) is { } csNodeToString) return csNodeToString;

            if (!ExpressionEvaluator.ConversionsTypeFullNames.TryGetValue(targetType.GetFullMetadataName(), out var methodId)) {
                return CreateCast(csNode, targetType);
            }

            // Need to use Conversions rather than Convert to match what VB does, eg. Conversions.ToInteger(True) -> -1
            _extraUsingDirectives.Add("Microsoft.VisualBasic.CompilerServices");
            var memberAccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Conversions"), SyntaxFactory.IdentifierName(methodId.Name));
            var arguments = SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(csNode)));
            return SyntaxFactory.InvocationExpression(memberAccess, arguments);
        }

        /// <summary>
        /// For many types, Conversions.ToString is the same as ToString.
        /// I've done some checks on numeric types, could add more here in future. Any reference types will need a conditional to avoid nullref like Conversions does
        /// </summary>
        private static ExpressionSyntax GetToStringConversionOrNull(ExpressionSyntax csNode, ITypeSymbol currentType, ITypeSymbol targetType)
        {
            if (targetType.SpecialType != SpecialType.System_String) return null;

            const string toStringMethodName = "ToString";
            if (csNode is MemberAccessExpressionSyntax maes && maes.Name.Identifier.Text == toStringMethodName) {
                return csNode;
            }

            if (currentType.IsNumericType()) {
                var toString = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    csNode.AddParens(), SyntaxFactory.IdentifierName(toStringMethodName));
                return SyntaxFactory.InvocationExpression(toString, SyntaxFactory.ArgumentList());
            }
            return null;
        }

        public enum TypeConversionKind
        {
            Unknown,
            Identity,
            DestructiveCast,
            NonDestructiveCast,
            Conversion,
            EnumConversionThenCast,
            EnumCastThenConversion,
            NullableBool,
            StringToCharArray,
        }

        public static bool ConvertStringToCharLiteral(Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax node,
            ITypeSymbol type,
            ITypeSymbol convertedType,
            out char chr)
        {

            var preferChar = node.Parent is VBSyntax.PredefinedCastExpressionSyntax pces &&
                               pces.Keyword.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.CCharKeyword)
                || convertedType?.SpecialType == SpecialType.System_Char;
            if (preferChar && node.SkipIntoParens() is VBSyntax.LiteralExpressionSyntax les &&
                les.Token.Value is string str &&
                str.Length == 1) {
                chr = str.Single();
                return true;
            }

            chr = default;
            return false;
        }
    }
}