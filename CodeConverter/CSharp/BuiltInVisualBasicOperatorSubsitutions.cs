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
using VBasic = Microsoft.CodeAnalysis.VisualBasic;

namespace ICSharpCode.CodeConverter.CSharp
{

    public class BuiltInVisualBasicOperatorSubsitutions
    {
        /// <summary>
        /// Started as a paste of:
        /// https://github.com/dotnet/roslyn/blob/master/src/Compilers/VisualBasic/Portable/Lowering/LocalRewriter/LocalRewriter_BinaryOperators.vb#L233-L464
        /// See file history to understand any changes
        /// </summary>
        private BoundExpression TransformRewrittenBinaryOperator(BoundBinaryOperator node)
        {
            var opKind = node.OperatorKind;
            Debug.Assert((opKind & BinaryOperatorKind.Lifted) == 0);
            var switchExpr = opKind & BinaryOperatorKind.OpMask;
            switch (switchExpr) {
                case BinaryOperatorKind.Is:
                case BinaryOperatorKind.IsNot: {
                        node = node.Update(node.OperatorKind, ReplaceMyGroupCollectionPropertyGetWithUnderlyingField(node.Left), ReplaceMyGroupCollectionPropertyGetWithUnderlyingField(node.Right), node.Checked, node.ConstantValueOpt, node.Type);
                        if (node.Left.Type is object && node.Left.Type.IsNullableType || node.Right.Type is object && node.Right.Type.IsNullableType) {
                            return RewriteNullableIsOrIsNotOperator(node);
                        }

                        break;
                    }

                case BinaryOperatorKind.Concatenate:  // Concat needs to be done before expr trees, so in LocalRewriter instead of VBSemanticsRewriter
                    {
                        if (node.Type.IsObjectType()) {
                            return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ConcatenateObjectObjectObject);
                        } else {
                            return RewriteConcatenateOperator(node);
                        }

                        break;
                    }

                case BinaryOperatorKind.Like: {
                        if (node.Left.Type.IsObjectType()) {
                            return RewriteLikeOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_LikeOperator__LikeObjectObjectObjectCompareMethod);
                        } else {
                            return RewriteLikeOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_LikeOperator__LikeStringStringStringCompareMethod);
                        }

                        break;
                    }

                case BinaryOperatorKind.Equals: {
                        var leftType = node.Left.Type;
                        // NOTE: For some reason Dev11 seems to still ignore inside the expression tree the fact that the target 
                        // type of the binary operator is Boolean and used Object op Object => Object helpers even in this case 
                        // despite what is said in comments in RuntimeMembers CodeGenerator::GetHelperForObjRelOp
                        // TODO: Recheck

                        if (node.Type.IsObjectType() || this._inExpressionLambda && leftType.IsObjectType()) {
                            return RewriteObjectComparisonOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectEqualObjectObjectBoolean);
                        } else if (node.Type.IsBooleanType()) {
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

                case BinaryOperatorKind.NotEquals: {
                        var leftType = node.Left.Type;
                        // NOTE: See comment above

                        if (node.Type.IsObjectType() || this._inExpressionLambda && leftType.IsObjectType()) {
                            return RewriteObjectComparisonOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectNotEqualObjectObjectBoolean);
                        } else if (node.Type.IsBooleanType()) {
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

                case BinaryOperatorKind.LessThanOrEqual: {
                        var leftType = node.Left.Type;
                        // NOTE: See comment above

                        if (node.Type.IsObjectType() || this._inExpressionLambda && leftType.IsObjectType()) {
                            return RewriteObjectComparisonOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectLessEqualObjectObjectBoolean);
                        } else if (node.Type.IsBooleanType()) {
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

                case BinaryOperatorKind.GreaterThanOrEqual: {
                        var leftType = node.Left.Type;
                        // NOTE: See comment above

                        if (node.Type.IsObjectType() || this._inExpressionLambda && leftType.IsObjectType()) {
                            return RewriteObjectComparisonOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectGreaterEqualObjectObjectBoolean);
                        } else if (node.Type.IsBooleanType()) {
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

                case BinaryOperatorKind.LessThan: {
                        var leftType = node.Left.Type;
                        // NOTE: See comment above

                        if (node.Type.IsObjectType() || this._inExpressionLambda && leftType.IsObjectType()) {
                            return RewriteObjectComparisonOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectLessObjectObjectBoolean);
                        } else if (node.Type.IsBooleanType()) {
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

                case BinaryOperatorKind.GreaterThan: {
                        var leftType = node.Left.Type;
                        // NOTE: See comment above

                        if (node.Type.IsObjectType() || this._inExpressionLambda && leftType.IsObjectType()) {
                            return RewriteObjectComparisonOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__CompareObjectGreaterObjectObjectBoolean);
                        } else if (node.Type.IsBooleanType()) {
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

                case BinaryOperatorKind.Add: {
                        if (node.Type.IsObjectType() && !_inExpressionLambda) {
                            return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__AddObjectObjectObject);
                        } else if (node.Type.IsDecimalType()) {
                            return RewriteDecimalBinaryOperator(node, SpecialMember.System_Decimal__AddDecimalDecimal);
                        }

                        break;
                    }

                case BinaryOperatorKind.Subtract: {
                        if (node.Type.IsObjectType() && !_inExpressionLambda) {
                            return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__SubtractObjectObjectObject);
                        } else if (node.Type.IsDecimalType()) {
                            return RewriteDecimalBinaryOperator(node, SpecialMember.System_Decimal__SubtractDecimalDecimal);
                        }

                        break;
                    }

                case BinaryOperatorKind.Multiply: {
                        if (node.Type.IsObjectType() && !_inExpressionLambda) {
                            return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__MultiplyObjectObjectObject);
                        } else if (node.Type.IsDecimalType()) {
                            return RewriteDecimalBinaryOperator(node, SpecialMember.System_Decimal__MultiplyDecimalDecimal);
                        }

                        break;
                    }

                case BinaryOperatorKind.Modulo: {
                        if (node.Type.IsObjectType() && !_inExpressionLambda) {
                            return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ModObjectObjectObject);
                        } else if (node.Type.IsDecimalType()) {
                            return RewriteDecimalBinaryOperator(node, SpecialMember.System_Decimal__RemainderDecimalDecimal);
                        }

                        break;
                    }

                case BinaryOperatorKind.Divide: {
                        if (node.Type.IsObjectType() && !_inExpressionLambda) {
                            return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__DivideObjectObjectObject);
                        } else if (node.Type.IsDecimalType()) {
                            return RewriteDecimalBinaryOperator(node, SpecialMember.System_Decimal__DivideDecimalDecimal);
                        }

                        break;
                    }

                case BinaryOperatorKind.IntegerDivide: {
                        if (node.Type.IsObjectType() && !_inExpressionLambda) {
                            return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__IntDivideObjectObjectObject);
                        }

                        break;
                    }

                case BinaryOperatorKind.Power: {
                        if (node.Type.IsObjectType() && !_inExpressionLambda) {
                            return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__ExponentObjectObjectObject);
                        } else {
                            return RewritePowOperator(node);
                        }

                        break;
                    }

                case BinaryOperatorKind.LeftShift: {
                        if (node.Type.IsObjectType() && !_inExpressionLambda) {
                            return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__LeftShiftObjectObjectObject);
                        }

                        break;
                    }

                case BinaryOperatorKind.RightShift: {
                        if (node.Type.IsObjectType() && !_inExpressionLambda) {
                            return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__RightShiftObjectObjectObject);
                        }

                        break;
                    }

                case BinaryOperatorKind.OrElse:
                case BinaryOperatorKind.AndAlso: {
                        if (node.Type.IsObjectType() && !_inExpressionLambda) {
                            return RewriteObjectShortCircuitOperator(node);
                        }

                        break;
                    }

                case BinaryOperatorKind.Xor: {
                        if (node.Type.IsObjectType() && !_inExpressionLambda) {
                            return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__XorObjectObjectObject);
                        }

                        break;
                    }

                case BinaryOperatorKind.Or: {
                        if (node.Type.IsObjectType() && !_inExpressionLambda) {
                            return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__OrObjectObjectObject);
                        }

                        break;
                    }

                case BinaryOperatorKind.And: {
                        if (node.Type.IsObjectType() && !_inExpressionLambda) {
                            return RewriteObjectBinaryOperator(node, WellKnownMember.Microsoft_VisualBasic_CompilerServices_Operators__AndObjectObjectObject);
                        }

                        break;
                    }
            }

            return node;
        }


    }
}
