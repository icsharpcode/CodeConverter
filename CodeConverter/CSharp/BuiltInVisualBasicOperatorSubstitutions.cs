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
        public static IOperatorConverter Create(CommentConvertingVisitorWrapper expressionVisitor, SemanticModel semanticModel, VisualBasicEqualityComparison visualBasicEqualityComparison, TypeConversionAnalyzer typeConversionAnalyzer)
        {
            return new BuiltInVisualBasicOperatorSubstitutions(expressionVisitor, semanticModel, visualBasicEqualityComparison, typeConversionAnalyzer);
        }

        private class BuiltInVisualBasicOperatorSubstitutions : IOperatorConverter
        {
            private const string _compilerServices = nameof(Microsoft) + "." + nameof(Microsoft.VisualBasic) + "." + nameof(Microsoft.VisualBasic.CompilerServices);
            private const string _operators = nameof(Operators);
            private readonly SemanticModel _semanticModel;
            private readonly VisualBasicEqualityComparison _visualBasicEqualityComparison;
            private readonly CommentConvertingVisitorWrapper _triviaConvertingVisitor;
            private readonly TypeConversionAnalyzer _typeConversionAnalyzer;

            public BuiltInVisualBasicOperatorSubstitutions(CommentConvertingVisitorWrapper triviaConvertingVisitor, SemanticModel semanticModel, VisualBasicEqualityComparison visualBasicEqualityComparison, TypeConversionAnalyzer typeConversionAnalyzer)
            {
                _semanticModel = semanticModel;
                _visualBasicEqualityComparison = visualBasicEqualityComparison;
                _triviaConvertingVisitor = triviaConvertingVisitor;
                _typeConversionAnalyzer = typeConversionAnalyzer;
            }

            public async Task<ExpressionSyntax> ConvertReferenceOrNothingComparisonOrNullAsync(VBSyntax.ExpressionSyntax exprNode, bool negateExpression = false)
            {
                if (!(exprNode is VBSyntax.BinaryExpressionSyntax node) ||
                    !node.IsKind(VBasic.SyntaxKind.IsExpression, VBasic.SyntaxKind.EqualsExpression, VBasic.SyntaxKind.IsNotExpression, VBasic.SyntaxKind.NotEqualsExpression)) {
                    return null;
                }
                
                var notted =
                    node.IsKind(VBasic.SyntaxKind.IsNotExpression, VBasic.SyntaxKind.NotEqualsExpression) ||
                    negateExpression;
                var isReferenceComparison = node.IsKind(VBasic.SyntaxKind.IsExpression, VBasic.SyntaxKind.IsNotExpression);

                if (ArgComparedToNull(node) is {} vbOtherArg) {
                    var csOtherArg = await ConvertIsOrIsNotExpressionArgAsync(vbOtherArg);
                    return notted
                        ? CommonConversions.NotNothingComparison(csOtherArg, isReferenceComparison)
                        : CommonConversions.NothingComparison(csOtherArg, isReferenceComparison);
                }

                if (isReferenceComparison) {

                    var lhs = await ConvertIsOrIsNotExpressionArgAsync(node.Left);
                    var rhs = await ConvertIsOrIsNotExpressionArgAsync(node.Right);

                    var equalityCheck = new KnownMethod(nameof(System), nameof(Object), nameof(object.ReferenceEquals))
                        .Invoke(_visualBasicEqualityComparison.ExtraUsingDirectives,
                            ConvertTo(node.Left, lhs, SpecialType.System_Object), rhs);
                    return notted
                        ? SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, equalityCheck)
                        : equalityCheck;
                }
                return null;
            }

            private static VBSyntax.ExpressionSyntax ArgComparedToNull(VBSyntax.BinaryExpressionSyntax node)
            {
                if (node.Left.IsKind(VBasic.SyntaxKind.NothingLiteralExpression))
                {
                    return node.Right;
                }
                else if (node.Right.IsKind(VBasic.SyntaxKind.NothingLiteralExpression))
                {
                    return node.Left;
                }

                return null;
            }

            private async Task<ExpressionSyntax> ConvertIsOrIsNotExpressionArgAsync(VBSyntax.ExpressionSyntax binaryExpressionArg)
            {
                return (ExpressionSyntax) (await ConvertMyGroupCollectionPropertyGetWithUnderlyingFieldAsync(binaryExpressionArg)
                                           ?? await binaryExpressionArg.AcceptAsync(_triviaConvertingVisitor));
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

            private async Task<ExpressionSyntax> ConvertToLikeOperatorAsync(VBSyntax.BinaryExpressionSyntax node, KnownMethod member)
            {
                var (lhs, rhs) = await AcceptSidesAsync(node);
                var compareText = ValidSyntaxFactory.MemberAccess("CompareMethod", _visualBasicEqualityComparison.OptionCompareTextCaseInsensitive ? "Text" : "Binary");
                _visualBasicEqualityComparison.ExtraUsingDirectives.Add("Microsoft.VisualBasic");
                return member.Invoke(_visualBasicEqualityComparison.ExtraUsingDirectives, lhs, rhs, compareText);
            }

            private async Task<ExpressionSyntax> ConvertToPowOperatorAsync(VBSyntax.BinaryExpressionSyntax node)
            {
                var (lhs, rhs) = await AcceptSidesAsync(node);
                lhs = ConvertTo(node.Left, lhs, SpecialType.System_Double);
                rhs = ConvertTo(node.Right, rhs, SpecialType.System_Double);
                return new KnownMethod(nameof(System), nameof(Math), nameof(Math.Pow))
                    .Invoke(_visualBasicEqualityComparison.ExtraUsingDirectives, lhs, rhs);
            }

            private ExpressionSyntax ConvertTo(VBSyntax.ExpressionSyntax vbNode, ExpressionSyntax csNode, SpecialType targetType)
            {
                return _typeConversionAnalyzer.AddExplicitConversion(vbNode, csNode, forceTargetType: _semanticModel.Compilation.GetSpecialType(targetType));
            }

            /// <remarks>No need to implement these since this is only called for things that are already decimal and hence will resolve operator in C#</remarks>
            private async Task<ExpressionSyntax> ConvertToDecimalBinaryOperatorAsync(VBSyntax.BinaryExpressionSyntax node, KnownMethod member) =>
                default;

            private async Task<ExpressionSyntax> ConvertToObjectBinaryOperatorAsync(VBSyntax.BinaryExpressionSyntax node, KnownMethod member) =>
                await ConvertToMethodAsync(node, member);

            private async Task<ExpressionSyntax> ConvertToObjectComparisonOperatorAsync(VBSyntax.BinaryExpressionSyntax node, KnownMethod member)
            {
                var (lhs, rhs) = await AcceptSidesAsync(node);
                member = (member.Import, member.TypeName, "Conditional" + member.MethodName); //The VB compiler would late bind, but this should provide identical results in most cases I think
                var optionaCompareTextBoolLiteralExpression = _visualBasicEqualityComparison.OptionCompareTextCaseInsensitiveBoolExpression;
                return member.Invoke(_visualBasicEqualityComparison.ExtraUsingDirectives, lhs, rhs, optionaCompareTextBoolLiteralExpression);
            }

            private async Task<ExpressionSyntax> ConvertToConcatenateOperatorAsync(VBSyntax.BinaryExpressionSyntax node)
            {
                return null;
            }

            private async Task<ExpressionSyntax> ConvertToObjectShortCircuitOperatorAsync(VBSyntax.BinaryExpressionSyntax node)
            {
                return null;
            }

            private async Task<ExpressionSyntax> ConvertToDateComparisonOperatorAsync(VBSyntax.BinaryExpressionSyntax node)
            {
                return null;
            }

            private async Task<ExpressionSyntax> ConvertToDecimalComparisonOperatorAsync(VBSyntax.BinaryExpressionSyntax node)
            {
                return null;
            }

            private async Task<ExpressionSyntax> ConvertToStringComparisonOperatorAsync(VBSyntax.BinaryExpressionSyntax node)
            {
                return null;
            }

            private async Task<ExpressionSyntax> ConvertToMethodAsync(VBSyntax.BinaryExpressionSyntax node, KnownMethod member)
            {
                var (lhs, rhs) = await AcceptSidesAsync(node);
                return member.Invoke(_visualBasicEqualityComparison.ExtraUsingDirectives, lhs, rhs);
            }

            private async Task<(ExpressionSyntax, ExpressionSyntax)> AcceptSidesAsync(VBSyntax.BinaryExpressionSyntax node)
            {
                return (await _triviaConvertingVisitor.AcceptAsync<ExpressionSyntax>(node.Left, SourceTriviaMapKind.All), await _triviaConvertingVisitor.AcceptAsync<ExpressionSyntax>(node.Right, SourceTriviaMapKind.All));
            }

            /// <summary>
            /// Started as a paste of:
            /// https://github.com/dotnet/roslyn/blob/master/src/Compilers/VisualBasic/Portable/Lowering/LocalConvertTor/LocalConvertTor_BinaryOperators.vb#L233-L464
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
                            if (await ConvertReferenceOrNothingComparisonOrNullAsync(node) is { } nothingComparison) return nothingComparison;
                            
                            break;
                        }

                    case BinaryOperatorKind.ConcatenateExpression:  // Concat needs to be done before expr trees, so in LocalConvertTor instead of VBSemanticsConvertTor
                        {
                            if (nodeType.IsObjectType()) {
                                return await ConvertToObjectBinaryOperatorAsync(node, (_compilerServices, _operators, "ConcatenateObject"));
                            } else {
                                return await ConvertToConcatenateOperatorAsync(node);
                            }
                        }

                    case BinaryOperatorKind.LikeExpression: {
                            if (leftType.IsObjectType()) {
                                return await ConvertToLikeOperatorAsync(node, (_compilerServices, "LikeOperator", "LikeObject"));
                            } else {
                                return await ConvertToLikeOperatorAsync(node, (_compilerServices, "LikeOperator", "LikeString"));
                            }
                        }

                    case BinaryOperatorKind.EqualsExpression: {
                            // NOTE: For some reason Dev11 seems to still ignore inside the expression tree the fact that the target 
                            // type of the binary operator is Boolean and used Object op Object => Object helpers even in this case 
                            // despite what is said in comments in RuntimeMembers CodeGenerator::GetHelperForObjRelOp
                            // TODO: Recheck

                            if (nodeType.IsObjectType() || inExpressionLambda && leftType.IsObjectType()) {
                                return await ConvertToObjectComparisonOperatorAsync(node, (_compilerServices, _operators, "CompareObjectEqual"));
                            } else if (nodeType.IsBooleanType()) {
                                if (leftType.IsObjectType()) {
                                    return await ConvertToObjectComparisonOperatorAsync(node, (_compilerServices, _operators, "ConditionalCompareObjectEqual"));
                                } else if (leftType.IsStringType()) {
                                    return await ConvertToStringComparisonOperatorAsync(node);
                                } else if (leftType.IsDecimalType()) {
                                    return await ConvertToDecimalComparisonOperatorAsync(node);
                                } else if (leftType.IsDateTimeType()) {
                                    return await ConvertToDateComparisonOperatorAsync(node);
                                }
                            }

                            break;
                        }

                    case BinaryOperatorKind.NotEqualsExpression: {
                            // NOTE: See comment above

                            if (nodeType.IsObjectType() || inExpressionLambda && leftType.IsObjectType()) {
                                return await ConvertToObjectComparisonOperatorAsync(node, (_compilerServices, _operators, "CompareObjectNotEqual"));
                            } else if (nodeType.IsBooleanType()) {
                                if (leftType.IsObjectType()) {
                                    return await ConvertToObjectComparisonOperatorAsync(node, (_compilerServices, _operators, "ConditionalCompareObjectNotEqual"));
                                } else if (leftType.IsStringType()) {
                                    return await ConvertToStringComparisonOperatorAsync(node);
                                } else if (leftType.IsDecimalType()) {
                                    return await ConvertToDecimalComparisonOperatorAsync(node);
                                } else if (leftType.IsDateTimeType()) {
                                    return await ConvertToDateComparisonOperatorAsync(node);
                                }
                            }

                            break;
                        }

                    case BinaryOperatorKind.LessThanOrEqualExpression: {
                            // NOTE: See comment above

                            if (nodeType.IsObjectType() || inExpressionLambda && leftType.IsObjectType()) {
                                return await ConvertToObjectComparisonOperatorAsync(node, (_compilerServices, _operators, "CompareObjectLessEqual"));
                            } else if (nodeType.IsBooleanType()) {
                                if (leftType.IsObjectType()) {
                                    return await ConvertToObjectComparisonOperatorAsync(node, (_compilerServices, _operators, "ConditionalCompareObjectLessEqual"));
                                } else if (leftType.IsStringType()) {
                                    return await ConvertToStringComparisonOperatorAsync(node);
                                } else if (leftType.IsDecimalType()) {
                                    return await ConvertToDecimalComparisonOperatorAsync(node);
                                } else if (leftType.IsDateTimeType()) {
                                    return await ConvertToDateComparisonOperatorAsync(node);
                                }
                            }

                            break;
                        }

                    case BinaryOperatorKind.GreaterThanOrEqualExpression: {
                            // NOTE: See comment above

                            if (nodeType.IsObjectType() || inExpressionLambda && leftType.IsObjectType()) {
                                return await ConvertToObjectComparisonOperatorAsync(node, (_compilerServices, _operators, "CompareObjectGreaterEqual"));
                            } else if (nodeType.IsBooleanType()) {
                                if (leftType.IsObjectType()) {
                                    return await ConvertToObjectComparisonOperatorAsync(node, (_compilerServices, _operators, "ConditionalCompareObjectGreaterEqual"));
                                } else if (leftType.IsStringType()) {
                                    return await ConvertToStringComparisonOperatorAsync(node);
                                } else if (leftType.IsDecimalType()) {
                                    return await ConvertToDecimalComparisonOperatorAsync(node);
                                } else if (leftType.IsDateTimeType()) {
                                    return await ConvertToDateComparisonOperatorAsync(node);
                                }
                            }

                            break;
                        }

                    case BinaryOperatorKind.LessThanExpression: {
                            // NOTE: See comment above

                            if (nodeType.IsObjectType() || inExpressionLambda && leftType.IsObjectType()) {
                                return await ConvertToObjectComparisonOperatorAsync(node, (_compilerServices, _operators, "CompareObjectLess"));
                            } else if (nodeType.IsBooleanType()) {
                                if (leftType.IsObjectType()) {
                                    return await ConvertToObjectComparisonOperatorAsync(node, (_compilerServices, _operators, "ConditionalCompareObjectLess"));
                                } else if (leftType.IsStringType()) {
                                    return await ConvertToStringComparisonOperatorAsync(node);
                                } else if (leftType.IsDecimalType()) {
                                    return await ConvertToDecimalComparisonOperatorAsync(node);
                                } else if (leftType.IsDateTimeType()) {
                                    return await ConvertToDateComparisonOperatorAsync(node);
                                }
                            }

                            break;
                        }

                    case BinaryOperatorKind.GreaterThanExpression: {
                            // NOTE: See comment above

                            if (nodeType.IsObjectType() || inExpressionLambda && leftType.IsObjectType()) {
                                return await ConvertToObjectComparisonOperatorAsync(node, (_compilerServices, _operators, "CompareObjectGreater"));
                            } else if (nodeType.IsBooleanType()) {
                                if (leftType.IsObjectType()) {
                                    return await ConvertToObjectComparisonOperatorAsync(node, (_compilerServices, _operators, "ConditionalCompareObjectGreater"));
                                } else if (leftType.IsStringType()) {
                                    return await ConvertToStringComparisonOperatorAsync(node);
                                } else if (leftType.IsDecimalType()) {
                                    return await ConvertToDecimalComparisonOperatorAsync(node);
                                } else if (leftType.IsDateTimeType()) {
                                    return await ConvertToDateComparisonOperatorAsync(node);
                                }
                            }

                            break;
                        }

                    case BinaryOperatorKind.AddExpression: {
                            if (nodeType.IsObjectType() && !inExpressionLambda) {
                                return await ConvertToObjectBinaryOperatorAsync(node, (_compilerServices, _operators, "AddObject"));
                            } else if (nodeType.IsDecimalType()) {
                                return await ConvertToDecimalBinaryOperatorAsync(node, (nameof(System), nameof(Decimal), nameof(Decimal.Add)));
                            }
                            break;
                        }

                    case BinaryOperatorKind.SubtractExpression: {
                            if (nodeType.IsObjectType() && !inExpressionLambda) {
                                return await ConvertToObjectBinaryOperatorAsync(node, (_compilerServices, _operators, "SubtractObject"));
                            } else if (nodeType.IsDecimalType()) {
                                return await ConvertToDecimalBinaryOperatorAsync(node, (nameof(System), nameof(Decimal), nameof(Decimal.Subtract)));
                            }

                            break;
                        }

                    case BinaryOperatorKind.MultiplyExpression: {
                            if (nodeType.IsObjectType() && !inExpressionLambda) {
                                return await ConvertToObjectBinaryOperatorAsync(node, (_compilerServices, _operators, "MultiplyObject"));
                            } else if (nodeType.IsDecimalType()) {
                                return await ConvertToDecimalBinaryOperatorAsync(node, (nameof(System), nameof(Decimal), nameof(Decimal.Multiply)));
                            }

                            break;
                        }

                    case BinaryOperatorKind.ModuloExpression: {
                            if (nodeType.IsObjectType() && !inExpressionLambda) {
                                return await ConvertToObjectBinaryOperatorAsync(node, (_compilerServices, _operators, "ModObject"));
                            } else if (nodeType.IsDecimalType()) {
                                return await ConvertToDecimalBinaryOperatorAsync(node, (nameof(System), nameof(Decimal), nameof(Decimal.Remainder)));
                            }

                            break;
                        }

                    case BinaryOperatorKind.DivideExpression: {
                            if (nodeType.IsObjectType() && !inExpressionLambda) {
                                return await ConvertToObjectBinaryOperatorAsync(node, (_compilerServices, _operators, "DivideObject"));
                            } else if (nodeType.IsDecimalType()) {
                                return await ConvertToDecimalBinaryOperatorAsync(node, (nameof(System), nameof(Decimal), nameof(Decimal.Divide)));
                            }

                            break;
                        }

                    case BinaryOperatorKind.IntegerDivideExpression: {
                            if (nodeType.IsObjectType() && !inExpressionLambda) {
                                return await ConvertToObjectBinaryOperatorAsync(node, (_compilerServices, _operators, "IntDivideObject"));
                            }

                            break;
                        }

                    case BinaryOperatorKind.ExponentiateExpression: {
                            if (nodeType.IsObjectType() && !inExpressionLambda) {
                                return await ConvertToObjectBinaryOperatorAsync(node, (_compilerServices, _operators, "ExponentObject"));
                            } else {
                                return await ConvertToPowOperatorAsync(node);
                            }
                        }

                    case BinaryOperatorKind.LeftShiftExpression: {
                            if (nodeType.IsObjectType() && !inExpressionLambda) {
                                return await ConvertToObjectBinaryOperatorAsync(node, (_compilerServices, _operators, "LeftShiftObject"));
                            }

                            break;
                        }

                    case BinaryOperatorKind.RightShiftExpression: {
                            if (nodeType.IsObjectType() && !inExpressionLambda) {
                                return await ConvertToObjectBinaryOperatorAsync(node, (_compilerServices, _operators, "RightShiftObject"));
                            }

                            break;
                        }

                    case BinaryOperatorKind.OrElseExpression:
                    case BinaryOperatorKind.AndAlsoExpression: {
                            if (nodeType.IsObjectType() && !inExpressionLambda) {
                                return await ConvertToObjectShortCircuitOperatorAsync(node);
                            }

                            break;
                        }

                    case var _ when node.OperatorToken.IsKind(BinaryOperatorKind.XorKeyword): {
                            if (nodeType.IsObjectType() && !inExpressionLambda) {
                                return await ConvertToObjectBinaryOperatorAsync(node, (_compilerServices, _operators, "XorObject"));
                            }

                            break;
                        }

                    case BinaryOperatorKind.OrExpression: {
                            if (nodeType.IsObjectType() && !inExpressionLambda) {
                                return await ConvertToObjectBinaryOperatorAsync(node, (_compilerServices, _operators, "OrObject"));
                            }

                            break;
                        }

                    case BinaryOperatorKind.AndExpression: {
                            if (nodeType.IsObjectType() && !inExpressionLambda) {
                                return await ConvertToObjectBinaryOperatorAsync(node, (_compilerServices, _operators, "AndObject"));
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
