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

        public TypeConversionAnalyzer(SemanticModel semanticModel, CSharpCompilation csCompilation,
            CommonConversions commonConversions, HashSet<string> extraUsingDirectives)
        {
            _semanticModel = semanticModel;
            _csCompilation = csCompilation;
            _extraUsingDirectives = extraUsingDirectives;
            CommonConversions = commonConversions;
        }

        private CommonConversions CommonConversions { get; }

        public ExpressionSyntax AddExplicitConversion(Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax vbNode, ExpressionSyntax csNode, bool addParenthesisIfNeeded = false)
        {
            var typeInfo = _semanticModel.GetTypeInfo(vbNode);
            var vbType = typeInfo.Type;
            var vbConvertedType = typeInfo.ConvertedType;
            if (vbType is null || vbConvertedType is null) {
                return csNode;
            }

            var vbCompilation = _semanticModel.Compilation as Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation;
            var vbConversion = vbCompilation.ClassifyConversion(vbType, vbConvertedType);

            var csType = _csCompilation.GetTypeByMetadataName(vbType.GetFullMetadataName());
            var csConvertedType = _csCompilation.GetTypeByMetadataName(vbConvertedType.GetFullMetadataName());

            if (csType is null || csConvertedType is null) {
                return csNode;
            }

            var csConversion = _csCompilation.ClassifyConversion(csType, csConvertedType);

            bool insertConvertTo = false;
            bool insertCast = false;

            bool isConvertToString = vbConversion.IsString && vbConvertedType.SpecialType == SpecialType.System_String;
            bool isArithmetic = vbNode.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.AddExpression, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.SubtractExpression,
                Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.MultiplyExpression, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.DivideExpression,
                Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.IntegerDivideExpression);
            if (!csConversion.Exists) {
                insertConvertTo = isConvertToString || vbConversion.IsNarrowing;
            } else if (vbConversion.IsWidening && vbConversion.IsNumeric && csConversion.IsImplicit && csConversion.IsNumeric) {
                // Safe overapproximation: A cast is really only needed to help resolve the overload for the operator/method used.
                // e.g. When VB "&" changes to C# "+", there are lots more overloads available that implicit casts could match.
                // e.g. sbyte * ulong uses the decimal * operator in VB. In C# it's ambiguous - see ExpressionTests.vb "TestMul".
                insertCast = true;
            } else if (csConversion.IsExplicit && vbConversion.IsNumeric && vbType.TypeKind != TypeKind.Enum) {
                insertConvertTo = true;
            } else if (isArithmetic) {
                var arithmeticConversion = vbCompilation.ClassifyConversion(vbConvertedType, vbCompilation.GetTypeByMetadataName("System.Int32"));
                if (arithmeticConversion.IsWidening && !arithmeticConversion.IsIdentity) {
                    insertConvertTo = true;
                }
            }

            if (insertConvertTo) {
                return AddExplicitConvertTo(vbNode, csNode, vbConvertedType);
            } else if (insertCast) {
                var typeName = CommonConversions.ToCsTypeSyntax(vbConvertedType, vbNode);
                if (csNode is CastExpressionSyntax cast && cast.Type.IsEquivalentTo(typeName)) {
                    return csNode;
                }

                csNode = addParenthesisIfNeeded ? (ExpressionSyntax)CommonConversions.ParenthesizeIfPrecedenceCouldChange(vbNode, csNode) : csNode;
                return SyntaxFactory.CastExpression(typeName, csNode);
            }

            return csNode;
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

            // Need to use Conversions rather than Convert to match what VB does, eg. True -> -1
            _extraUsingDirectives.Add("Microsoft.VisualBasic.CompilerServices");
            var memberAccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Conversions"), SyntaxFactory.IdentifierName($"To{displayType}"));
            var arguments = SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(csNode)));
            return SyntaxFactory.InvocationExpression(memberAccess, arguments);
        }
    }
}