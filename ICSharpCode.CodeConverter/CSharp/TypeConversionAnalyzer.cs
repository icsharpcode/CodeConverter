using System;
using System.Collections.Generic;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class TypeConversionAnalyzer
    {
        private readonly CSharpCompilation _csCompilation;
        private readonly SemanticModel _semanticModel;
        private readonly HashSet<string> _extraUsingDirectives;

        public TypeConversionAnalyzer(SemanticModel semanticModel, CSharpCompilation csCompilation, HashSet<string> extraUsingDirectives)
        {
            _semanticModel = semanticModel;
            _csCompilation = csCompilation;
            _extraUsingDirectives = extraUsingDirectives;
        }

        public ExpressionSyntax AddExplicitConversion(Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax vbNode, ExpressionSyntax csNode, bool addParenthesisIfNeeded = false, bool alwaysExplicit = false, bool implicitCastFromIntToEnum = false)
        {
            var conversionKind = AnalyzeConversion(vbNode, alwaysExplicit, implicitCastFromIntToEnum, out var vbConvertedType);
            csNode = addParenthesisIfNeeded && (conversionKind == TypeConversionKind.DestructiveCast || conversionKind == TypeConversionKind.NonDestructiveCast)
                ? VbSyntaxNodeExtensions.ParenthesizeIfPrecedenceCouldChange(vbNode, csNode)
                : csNode;
            return AddExplicitConversion(vbNode, csNode, vbConvertedType, conversionKind, addParenthesisIfNeeded);
        }

        private ExpressionSyntax AddExplicitConversion(Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax vbNode, ExpressionSyntax csNode,
            ITypeSymbol vbConvertedType, TypeConversionKind conversionKind, bool addParenthesisIfNeeded)
        {
            switch (conversionKind)
            {
                case TypeConversionKind.Unknown:
                case TypeConversionKind.Identity:
                    return addParenthesisIfNeeded ? VbSyntaxNodeExtensions.ParenthesizeIfPrecedenceCouldChange(vbNode, csNode) : csNode;
                case TypeConversionKind.DestructiveCast:
                case TypeConversionKind.NonDestructiveCast:
                    var typeName = _semanticModel.GetCsTypeSyntax(vbConvertedType, vbNode);
                    if (csNode is CastExpressionSyntax cast && cast.Type.IsEquivalentTo(typeName)) {
                        return csNode;
                    }
                    return ValidSyntaxFactory.CastExpression(typeName, csNode);
                case TypeConversionKind.Conversion:
                    return AddExplicitConvertTo(vbNode, csNode, vbConvertedType);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private TypeConversionKind AnalyzeConversion(Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax vbNode, bool alwaysExplicit, bool implicitCastFromIntToEnum, out ITypeSymbol vbConvertedType)
        {
            var typeInfo = _semanticModel.GetTypeInfo(vbNode);
            var vbType = typeInfo.Type;
            vbConvertedType = typeInfo.ConvertedType;
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

            var vbCompilation = _semanticModel.Compilation as Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation;
            var vbConversion = vbCompilation.ClassifyConversion(vbType, vbConvertedType);

            var csType = _csCompilation.GetTypeByMetadataName(vbType.GetFullMetadataName());
            var csConvertedType = _csCompilation.GetTypeByMetadataName(vbConvertedType.GetFullMetadataName());

            if (csType == null || csConvertedType == null)
            {
                if (alwaysExplicit && vbType != vbConvertedType) {
                    return vbConversion.IsNarrowing ? TypeConversionKind.NonDestructiveCast : TypeConversionKind.DestructiveCast;
                }
                return TypeConversionKind.Unknown;
            }

            var csConversion = _csCompilation.ClassifyConversion(csType, csConvertedType);
            
            bool isConvertToString = vbConversion.IsString && vbConvertedType.SpecialType == SpecialType.System_String;
            bool isArithmetic = vbNode.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.AddExpression,
                Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.SubtractExpression,
                Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.MultiplyExpression,
                Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.DivideExpression,
                Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.IntegerDivideExpression);
            if (!csConversion.Exists || csConversion.IsUnboxing)
            {
                if (isConvertToString || vbConversion.IsNarrowing) return TypeConversionKind.Conversion;
            }
            else if (vbConversion.IsWidening && vbConversion.IsNumeric && csConversion.IsImplicit && csConversion.IsNumeric)
            {
                // Safe overapproximation: A cast is really only needed to help resolve the overload for the operator/method used.
                // e.g. When VB "&" changes to C# "+", there are lots more overloads available that implicit casts could match.
                // e.g. sbyte * ulong uses the decimal * operator in VB. In C# it's ambiguous - see ExpressionTests.vb "TestMul".
                return TypeConversionKind.NonDestructiveCast;
            }
            else if (csConversion.IsExplicit && csConversion.IsEnumeration)
            {
                return implicitCastFromIntToEnum ? TypeConversionKind.Identity : TypeConversionKind.NonDestructiveCast;
            }
            else if (csConversion.IsExplicit && vbConversion.IsNumeric && vbType.TypeKind != TypeKind.Enum)
            {
                return TypeConversionKind.Conversion;
            }
            else if (isArithmetic)
            {
                var arithmeticConversion =
                    vbCompilation.ClassifyConversion(vbConvertedType, vbCompilation.GetTypeByMetadataName("System.Int32"));
                if (arithmeticConversion.IsWidening && !arithmeticConversion.IsIdentity)
                {
                    return TypeConversionKind.Conversion;
                }
            } else if (alwaysExplicit && vbType != vbConvertedType) {
                return TypeConversionKind.DestructiveCast;
            }

            return TypeConversionKind.Identity;
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

            var method = typeof(Microsoft.VisualBasic.CompilerServices.Conversions).GetMethod($"To{displayType}");
            if (method == null) {
                throw new NotImplementedException($"Unimplemented conversion for {displayType}");
            }

            // Need to use Conversions rather than Convert to match what VB does, eg. True -> -1
            _extraUsingDirectives.Add("Microsoft.VisualBasic.CompilerServices");
            var memberAccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Conversions"), SyntaxFactory.IdentifierName($"To{displayType}"));
            var arguments = SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(csNode)));
            return SyntaxFactory.InvocationExpression(memberAccess, arguments);
        }

        public enum TypeConversionKind
        {
            Unknown,
            Identity,
            DestructiveCast,
            NonDestructiveCast,
            Conversion
        }
    }
}