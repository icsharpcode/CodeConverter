using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using ICSharpCode.CodeConverter.Util.FromRoslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualBasic.CompilerServices;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using VBSyntaxKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;
using BinaryOperatorKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Operations;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class VbOperatorConversion
    {
        public static IOperatorConverter Create(CommentConvertingVisitorWrapper expressionVisitor, SemanticModel semanticModel, VisualBasicEqualityComparison visualBasicEqualityComparison)
        {
            return new BuiltInVisualBasicOperatorSubsitutions(expressionVisitor, semanticModel, visualBasicEqualityComparison);
        }

        private class BuiltInVisualBasicOperatorSubsitutions : IOperatorConverter
        {
            private readonly SemanticModel _semanticModel;
            private readonly VisualBasicEqualityComparison _visualBasicEqualityComparison;
            private readonly CommentConvertingVisitorWrapper _triviaConvertingVisitor;

            public BuiltInVisualBasicOperatorSubsitutions(CommentConvertingVisitorWrapper triviaConvertingVisitor, SemanticModel semanticModel, VisualBasicEqualityComparison visualBasicEqualityComparison)
            {
                _semanticModel = semanticModel;
                _visualBasicEqualityComparison = visualBasicEqualityComparison;
                _triviaConvertingVisitor = triviaConvertingVisitor;
            }

            public async Task<ExpressionSyntax> ConvertNothingComparisonOrNullAsync(VBSyntax.ExpressionSyntax exprNode, bool negateExpression = false)
            {
                if (!(exprNode is VBSyntax.BinaryExpressionSyntax node) || !node.IsKind(VBasic.SyntaxKind.IsExpression, VBasic.SyntaxKind.EqualsExpression, VBasic.SyntaxKind.IsNotExpression, VBasic.SyntaxKind.NotEqualsExpression)) {
                    return null;
                }
                ExpressionSyntax otherArgument;
                if (node.Left.IsKind(VBasic.SyntaxKind.NothingLiteralExpression)) {
                    otherArgument = (ExpressionSyntax)await ConvertIsOrIsNotExpressionArgAsync(node.Right);
                } else if (node.Right.IsKind(VBasic.SyntaxKind.NothingLiteralExpression)) {
                    otherArgument = (ExpressionSyntax)await ConvertIsOrIsNotExpressionArgAsync(node.Left);
                } else {
                    return null;
                }

                var isReference = node.IsKind(VBasic.SyntaxKind.IsExpression, VBasic.SyntaxKind.IsNotExpression);
                var notted = node.IsKind(VBasic.SyntaxKind.IsNotExpression, VBasic.SyntaxKind.NotEqualsExpression) || negateExpression;
                return notted ? CommonConversions.NotNothingComparison(otherArgument, isReference) : CommonConversions.NothingComparison(otherArgument, isReference);
            }

            private async Task<CSharpSyntaxNode> ConvertIsOrIsNotExpressionArgAsync(VBSyntax.ExpressionSyntax binaryExpressionArg)
            {
                return await ConvertMyGroupCollectionPropertyGetWithUnderlyingFieldAsync(binaryExpressionArg)
                       ?? await binaryExpressionArg.AcceptAsync(_triviaConvertingVisitor);
            }

            private async Task<ExpressionSyntax> ConvertMyGroupCollectionPropertyGetWithUnderlyingFieldAsync(SyntaxNode node)
            {
                var operation = _semanticModel.GetOperation(node);
                switch (operation) {
                    case IConversionOperation co:
                        return await ConvertMyGroupCollectionPropertyGetWithUnderlyingFieldAsync(co.Operand.Syntax);
                    case IPropertyReferenceOperation pro when pro.Property.IsMyGroupCollectionProperty():
                        var associatedField = pro.Property.GetAssociatedField();
                        var propertyReferenceOperation = ((IPropertyReferenceOperation)pro.Instance);
                        var qualification = (ExpressionSyntax)await propertyReferenceOperation.Syntax.AcceptAsync(_triviaConvertingVisitor);
                        return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, qualification, SyntaxFactory.IdentifierName(associatedField.Name));
                    default:
                        return null;
                }
            }

            private Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax RewriteLikeOperator(VBSyntax.BinaryExpressionSyntax node, WellKnownMember member)
            {
                return null;
            }

            private Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax RewriteConcatenateOperator(VBSyntax.BinaryExpressionSyntax node)
            {
                return null;
            }

            private Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax RewritePowOperator(VBSyntax.BinaryExpressionSyntax node)
            {
                return null;
            }

            private Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax RewriteObjectShortCircuitOperator(VBSyntax.BinaryExpressionSyntax node)
            {
                return null;
            }

            private Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax RewriteDecimalBinaryOperator(VBSyntax.BinaryExpressionSyntax node, SpecialMember member)
            {
                return null;
            }

            private Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax RewriteObjectBinaryOperator(VBSyntax.BinaryExpressionSyntax node, WellKnownMember member)
            {
                return null;
            }

            private Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax RewriteDateComparisonOperator(VBSyntax.BinaryExpressionSyntax node)
            {
                return null;
            }

            private Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax RewriteDecimalComparisonOperator(VBSyntax.BinaryExpressionSyntax node)
            {
                return null;
            }

            private Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax RewriteStringComparisonOperator(VBSyntax.BinaryExpressionSyntax node)
            {
                return null;
            }

            private Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax RewriteObjectComparisonOperator(VBSyntax.BinaryExpressionSyntax node, WellKnownMember member)
            {
                return null;
            }

            /// <summary>
            /// Started as a paste of:
            /// https://github.com/dotnet/roslyn/blob/master/src/Compilers/VisualBasic/Portable/Lowering/LocalRewriter/LocalRewriter_BinaryOperators.vb#L233-L464
            /// See file history to understand any changes
            /// </summary>
            public async Task<ExpressionSyntax> ConvertRewrittenBinaryOperatorOrNullAsync(VBSyntax.BinaryExpressionSyntax node, bool inExpressionLambda = false)
            {
                var opKind = node.Kind();
                var nodeType = _semanticModel.GetTypeInfo(node).Type;
                var leftType = _semanticModel.GetTypeInfo(node.Left).Type;
                switch (opKind) {
                    case BinaryOperatorKind.IsExpression:
                    case BinaryOperatorKind.IsNotExpression: {
                            if (await ConvertNothingComparisonOrNullAsync(node) is CSharpSyntaxNode nothingComparison) return (ExpressionSyntax) nothingComparison;
                            break;
                        }

                    case BinaryOperatorKind.ConcatenateExpression:  // Concat needs to be done before expr trees, so in LocalRewriter instead of VBSemanticsRewriter
                        {
                            if (nodeType.IsObjectType()) {
                                return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ConcatenateObjectObjectObject);
                            } else {
                                return RewriteConcatenateOperator(node);
                            }
                        }

                    case BinaryOperatorKind.LikeExpression: {
                            if (leftType.IsObjectType()) {
                                return RewriteLikeOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_LikeOperator__LikeObjectObjectObjectCompareMethod);
                            } else {
                                return RewriteLikeOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_LikeOperator__LikeStringStringStringCompareMethod);
                            }
                        }

                    case BinaryOperatorKind.EqualsExpression: {
                            // NOTE: For some reason Dev11 seems to still ignore inside the expression tree the fact that the target 
                            // type of the binary operator is Boolean and used Object op Object => Object helpers even in this case 
                            // despite what is said in comments in RuntimeMembers CodeGenerator::GetHelperForObjRelOp
                            // TODO: Recheck

                            if (nodeType.IsObjectType() || inExpressionLambda && leftType.IsObjectType()) {
                                return RewriteObjectComparisonOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectEqualObjectObjectBoolean);
                            } else if (nodeType.IsBooleanType()) {
                                if (leftType.IsObjectType()) {
                                    return RewriteObjectComparisonOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectEqualObjectObjectBoolean);
                                } else if (leftType.IsStringType()) {
                                    return RewriteStringComparisonOperator(node);
                                } else if (leftType.IsDecimalType()) {
                                    return RewriteDecimalComparisonOperator(node);
                                } else if (leftType.IsDateTimeType()) {
                                    return RewriteDateComparisonOperator(node);
                                }
                            }

                            break;
                        }

                    case BinaryOperatorKind.NotEqualsExpression: {
                            // NOTE: See comment above

                            if (nodeType.IsObjectType() || inExpressionLambda && leftType.IsObjectType()) {
                                return RewriteObjectComparisonOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectNotEqualObjectObjectBoolean);
                            } else if (nodeType.IsBooleanType()) {
                                if (leftType.IsObjectType()) {
                                    return RewriteObjectComparisonOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectNotEqualObjectObjectBoolean);
                                } else if (leftType.IsStringType()) {
                                    return RewriteStringComparisonOperator(node);
                                } else if (leftType.IsDecimalType()) {
                                    return RewriteDecimalComparisonOperator(node);
                                } else if (leftType.IsDateTimeType()) {
                                    return RewriteDateComparisonOperator(node);
                                }
                            }

                            break;
                        }

                    case BinaryOperatorKind.LessThanOrEqualExpression: {
                            // NOTE: See comment above

                            if (nodeType.IsObjectType() || inExpressionLambda && leftType.IsObjectType()) {
                                return RewriteObjectComparisonOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectLessEqualObjectObjectBoolean);
                            } else if (nodeType.IsBooleanType()) {
                                if (leftType.IsObjectType()) {
                                    return RewriteObjectComparisonOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectLessEqualObjectObjectBoolean);
                                } else if (leftType.IsStringType()) {
                                    return RewriteStringComparisonOperator(node);
                                } else if (leftType.IsDecimalType()) {
                                    return RewriteDecimalComparisonOperator(node);
                                } else if (leftType.IsDateTimeType()) {
                                    return RewriteDateComparisonOperator(node);
                                }
                            }

                            break;
                        }

                    case BinaryOperatorKind.GreaterThanOrEqualExpression: {
                            // NOTE: See comment above

                            if (nodeType.IsObjectType() || inExpressionLambda && leftType.IsObjectType()) {
                                return RewriteObjectComparisonOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectGreaterEqualObjectObjectBoolean);
                            } else if (nodeType.IsBooleanType()) {
                                if (leftType.IsObjectType()) {
                                    return RewriteObjectComparisonOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectGreaterEqualObjectObjectBoolean);
                                } else if (leftType.IsStringType()) {
                                    return RewriteStringComparisonOperator(node);
                                } else if (leftType.IsDecimalType()) {
                                    return RewriteDecimalComparisonOperator(node);
                                } else if (leftType.IsDateTimeType()) {
                                    return RewriteDateComparisonOperator(node);
                                }
                            }

                            break;
                        }

                    case BinaryOperatorKind.LessThanExpression: {
                            // NOTE: See comment above

                            if (nodeType.IsObjectType() || inExpressionLambda && leftType.IsObjectType()) {
                                return RewriteObjectComparisonOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectLessObjectObjectBoolean);
                            } else if (nodeType.IsBooleanType()) {
                                if (leftType.IsObjectType()) {
                                    return RewriteObjectComparisonOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectLessObjectObjectBoolean);
                                } else if (leftType.IsStringType()) {
                                    return RewriteStringComparisonOperator(node);
                                } else if (leftType.IsDecimalType()) {
                                    return RewriteDecimalComparisonOperator(node);
                                } else if (leftType.IsDateTimeType()) {
                                    return RewriteDateComparisonOperator(node);
                                }
                            }

                            break;
                        }

                    case BinaryOperatorKind.GreaterThanExpression: {
                            // NOTE: See comment above

                            if (nodeType.IsObjectType() || inExpressionLambda && leftType.IsObjectType()) {
                                return RewriteObjectComparisonOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectGreaterObjectObjectBoolean);
                            } else if (nodeType.IsBooleanType()) {
                                if (leftType.IsObjectType()) {
                                    return RewriteObjectComparisonOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ConditionalCompareObjectGreaterObjectObjectBoolean);
                                } else if (leftType.IsStringType()) {
                                    return RewriteStringComparisonOperator(node);
                                } else if (leftType.IsDecimalType()) {
                                    return RewriteDecimalComparisonOperator(node);
                                } else if (leftType.IsDateTimeType()) {
                                    return RewriteDateComparisonOperator(node);
                                }
                            }

                            break;
                        }

                    case BinaryOperatorKind.AddExpression: {
                            if (nodeType.IsObjectType() && !inExpressionLambda) {
                                return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__AddObjectObjectObject);
                            } else if (nodeType.IsDecimalType()) {
                                return RewriteDecimalBinaryOperator(node, SpecialMember.System_Decimal__AddDecimalDecimal);
                            }

                            break;
                        }

                    case BinaryOperatorKind.SubtractExpression: {
                            if (nodeType.IsObjectType() && !inExpressionLambda) {
                                return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__SubtractObjectObjectObject);
                            } else if (nodeType.IsDecimalType()) {
                                return RewriteDecimalBinaryOperator(node, SpecialMember.System_Decimal__SubtractDecimalDecimal);
                            }

                            break;
                        }

                    case BinaryOperatorKind.MultiplyExpression: {
                            if (nodeType.IsObjectType() && !inExpressionLambda) {
                                return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__MultiplyObjectObjectObject);
                            } else if (nodeType.IsDecimalType()) {
                                return RewriteDecimalBinaryOperator(node, SpecialMember.System_Decimal__MultiplyDecimalDecimal);
                            }

                            break;
                        }

                    case BinaryOperatorKind.ModuloExpression: {
                            if (nodeType.IsObjectType() && !inExpressionLambda) {
                                return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ModObjectObjectObject);
                            } else if (nodeType.IsDecimalType()) {
                                return RewriteDecimalBinaryOperator(node, SpecialMember.System_Decimal__RemainderDecimalDecimal);
                            }

                            break;
                        }

                    case BinaryOperatorKind.DivideExpression: {
                            if (nodeType.IsObjectType() && !inExpressionLambda) {
                                return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__DivideObjectObjectObject);
                            } else if (nodeType.IsDecimalType()) {
                                return RewriteDecimalBinaryOperator(node, SpecialMember.System_Decimal__DivideDecimalDecimal);
                            }

                            break;
                        }

                    case BinaryOperatorKind.IntegerDivideExpression: {
                            if (nodeType.IsObjectType() && !inExpressionLambda) {
                                return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__IntDivideObjectObjectObject);
                            }

                            break;
                        }

                    case BinaryOperatorKind.ExponentiateExpression: {
                            if (nodeType.IsObjectType() && !inExpressionLambda) {
                                return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ExponentObjectObjectObject);
                            } else {
                                return RewritePowOperator(node);
                            }
                        }

                    case BinaryOperatorKind.LeftShiftExpression: {
                            if (nodeType.IsObjectType() && !inExpressionLambda) {
                                return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__LeftShiftObjectObjectObject);
                            }

                            break;
                        }

                    case BinaryOperatorKind.RightShiftExpression: {
                            if (nodeType.IsObjectType() && !inExpressionLambda) {
                                return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__RightShiftObjectObjectObject);
                            }

                            break;
                        }

                    case BinaryOperatorKind.OrElseExpression:
                    case BinaryOperatorKind.AndAlsoExpression: {
                            if (nodeType.IsObjectType() && !inExpressionLambda) {
                                return RewriteObjectShortCircuitOperator(node);
                            }

                            break;
                        }

                    case var _ when node.OperatorToken.IsKind(BinaryOperatorKind.XorKeyword): {
                            if (nodeType.IsObjectType() && !inExpressionLambda) {
                                return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__XorObjectObjectObject);
                            }

                            break;
                        }

                    case BinaryOperatorKind.OrExpression: {
                            if (nodeType.IsObjectType() && !inExpressionLambda) {
                                return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__OrObjectObjectObject);
                            }

                            break;
                        }

                    case BinaryOperatorKind.AndExpression: {
                            if (nodeType.IsObjectType() && !inExpressionLambda) {
                                return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__AndObjectObjectObject);
                            }

                            break;
                        }
                }

                return null;
            }
        }

        private static bool IsBooleanType(this ITypeSymbol @this)
        {
            return @this.SpecialType == SpecialType.System_Boolean;
        }

        private static bool IsStringType(this ITypeSymbol @this)
        {
            return @this.SpecialType == SpecialType.System_String;
        }

        private static bool IsObjectType(this ITypeSymbol @this)
        {
            return @this.SpecialType == SpecialType.System_Object;
        }

        private static bool IsDecimalType(this ITypeSymbol @this)
        {
            return @this.SpecialType == SpecialType.System_Decimal;
        }

        private static bool IsDateTimeType(this ITypeSymbol @this)
        {
            return @this.SpecialType == SpecialType.System_DateTime;
        }
    }
}
