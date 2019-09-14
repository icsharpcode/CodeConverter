using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
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
        private static readonly Dictionary<string, string> ConversionsTypeFullNames = GetConversionsMethodsByTypeFullName();

        private static Dictionary<string, string> GetConversionsMethodsByTypeFullName()
        {
            return typeof(Conversions).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(n => n.Name.StartsWith("To") && n.GetParameters().Length == 1 && n.ReturnType?.FullName != null)
                .ToLookup(n => n.ReturnType.FullName, n => n.Name)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.First());
        }

        public TypeConversionAnalyzer(SemanticModel semanticModel, CSharpCompilation csCompilation,
            HashSet<string> extraUsingDirectives, SyntaxGenerator csSyntaxGenerator)
        {
            _semanticModel = semanticModel;
            _csCompilation = csCompilation;
            _extraUsingDirectives = extraUsingDirectives;
            _csSyntaxGenerator = csSyntaxGenerator;
        }

        public ExpressionSyntax AddExplicitConversion(Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax vbNode, ExpressionSyntax csNode, bool addParenthesisIfNeeded = true, bool alwaysExplicit = false)
        {
            var conversionKind = AnalyzeConversion(vbNode, alwaysExplicit);
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

        public TypeConversionKind AnalyzeConversion(Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax vbNode, bool alwaysExplicit = false)
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
            var csType = _csCompilation.GetTypeByMetadataName(vbType.GetFullMetadataName());
            var csConvertedType = _csCompilation.GetTypeByMetadataName(vbConvertedType.GetFullMetadataName());

            if (csType != null && csConvertedType != null && 
                TryAnalyzeCsConversion(vbNode, csType, csConvertedType, vbConversion, vbConvertedType, vbType, vbCompilation, out TypeConversionKind analyzeConversion)) {
                return analyzeConversion;
            }

            return AnalyzeVbConversion(alwaysExplicit, vbType, vbConvertedType, vbConversion);
        }

        private bool TryAnalyzeCsConversion(Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax vbNode, INamedTypeSymbol csType,
            INamedTypeSymbol csConvertedType, Conversion vbConversion, ITypeSymbol vbConvertedType, ITypeSymbol vbType,
            VisualBasicCompilation vbCompilation, out TypeConversionKind typeConversionKind)
        {
            var csConversion = _csCompilation.ClassifyConversion(csType, csConvertedType);

            bool isConvertToString =
                        vbConversion.IsString && vbConvertedType.SpecialType == SpecialType.System_String;
            bool isArithmetic = vbNode.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.AddExpression,
                Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.SubtractExpression,
                Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.MultiplyExpression,
                Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.DivideExpression,
                Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.IntegerDivideExpression);
            if (!csConversion.Exists || csConversion.IsUnboxing) {
                if (ConvertStringToCharLiteral(vbNode as LiteralExpressionSyntax, vbConvertedType, out _)) {
                    typeConversionKind = TypeConversionKind.Identity; // Already handled elsewhere by other usage of method
                    return true;
                }
                if (isConvertToString || vbConversion.IsNarrowing) {
                    typeConversionKind = TypeConversionKind.Conversion;
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
            } else if (csConversion.IsExplicit && vbConversion.IsNumeric && vbType.TypeKind != TypeKind.Enum) {
                typeConversionKind = TypeConversionKind.Conversion;
                return true;
            } else if (isArithmetic) {
                var arithmeticConversion =
                    vbCompilation.ClassifyConversion(vbConvertedType,
                        vbCompilation.GetTypeByMetadataName("System.Int32"));
                if (arithmeticConversion.IsWidening && !arithmeticConversion.IsIdentity) {
                    typeConversionKind = TypeConversionKind.Conversion;
                    return true;
                }
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
                SyntaxFactory.IdentifierName("Conversions"), SyntaxFactory.IdentifierName(methodId));
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