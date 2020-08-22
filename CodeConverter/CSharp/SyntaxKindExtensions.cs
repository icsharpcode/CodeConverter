using System;
using ICSharpCode.CodeConverter.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ICSharpCode.CodeConverter.CSharp
{
    public static class SyntaxKindExtensions
    {

        public static SyntaxToken ConvertToken(this SyntaxToken t, TokenContext context = TokenContext.Global)
        {
            Microsoft.CodeAnalysis.VisualBasic.SyntaxKind vbSyntaxKind = Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions.Kind(t);
            return SyntaxFactory.Token(ConvertToken(vbSyntaxKind, context));
        }

        public static SyntaxKind ConvertToken(this Microsoft.CodeAnalysis.VisualBasic.SyntaxKind t, TokenContext context = TokenContext.Global)
        {
            switch (t) {
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.None:
                    return SyntaxKind.None;
                // built-in types
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.BooleanKeyword:
                    return SyntaxKind.BoolKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.ByteKeyword:
                    return SyntaxKind.ByteKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.SByteKeyword:
                    return SyntaxKind.SByteKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.ShortKeyword:
                    return SyntaxKind.ShortKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.UShortKeyword:
                    return SyntaxKind.UShortKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.IntegerKeyword:
                    return SyntaxKind.IntKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.UIntegerKeyword:
                    return SyntaxKind.UIntKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.LongKeyword:
                    return SyntaxKind.LongKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.ULongKeyword:
                    return SyntaxKind.ULongKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.DoubleKeyword:
                    return SyntaxKind.DoubleKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.SingleKeyword:
                    return SyntaxKind.FloatKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.DecimalKeyword:
                    return SyntaxKind.DecimalKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.StringKeyword:
                    return SyntaxKind.StringKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.CharKeyword:
                    return SyntaxKind.CharKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.ObjectKeyword:
                    return SyntaxKind.ObjectKeyword;
                // literals
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.NothingKeyword:
                    return SyntaxKind.NullKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.TrueKeyword:
                    return SyntaxKind.TrueKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.FalseKeyword:
                    return SyntaxKind.FalseKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.MeKeyword:
                    return SyntaxKind.ThisKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.MyBaseKeyword:
                    return SyntaxKind.BaseKeyword;
                // modifiers
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.PublicKeyword:
                    return SyntaxKind.PublicKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.FriendKeyword:
                    return SyntaxKind.InternalKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.ProtectedKeyword:
                    return SyntaxKind.ProtectedKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.PrivateKeyword:
                    return SyntaxKind.PrivateKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.ByRefKeyword:
                    return SyntaxKind.RefKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.ParamArrayKeyword:
                    return SyntaxKind.ParamsKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.ReadOnlyKeyword:
                    return SyntaxKind.ReadOnlyKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.OverridesKeyword:
                    return SyntaxKind.OverrideKeyword;
                //New isn't as restrictive as shadows, but it will behave the same for all existing programs
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.ShadowsKeyword:
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.OverloadsKeyword:
                    return SyntaxKind.NewKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.OverridableKeyword:
                    return SyntaxKind.VirtualKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.SharedKeyword:
                    return SyntaxKind.StaticKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.ConstKeyword:
                    return SyntaxKind.ConstKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.PartialKeyword:
                    return SyntaxKind.PartialKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.MustInheritKeyword:
                    return SyntaxKind.AbstractKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.MustOverrideKeyword:
                    return SyntaxKind.AbstractKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.NotOverridableKeyword:
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.NotInheritableKeyword:
                    return SyntaxKind.SealedKeyword;
                // unary operators
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.UnaryMinusExpression:
                    return SyntaxKind.UnaryMinusExpression;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.UnaryPlusExpression:
                    return SyntaxKind.UnaryPlusExpression;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.NotExpression:
                    return SyntaxKind.LogicalNotExpression;
                // binary operators
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.ConcatenateExpression:
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.AddExpression:
                    return SyntaxKind.AddExpression;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.SubtractExpression:
                    return SyntaxKind.SubtractExpression;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.MultiplyExpression:
                    return SyntaxKind.MultiplyExpression;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.DivideExpression:
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.IntegerDivideExpression:
                    return SyntaxKind.DivideExpression;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.ModuloExpression:
                    return SyntaxKind.ModuloExpression;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.AndAlsoExpression:
                    return SyntaxKind.LogicalAndExpression;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.OrElseExpression:
                    return SyntaxKind.LogicalOrExpression;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.OrExpression:
                    return SyntaxKind.BitwiseOrExpression;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.AndExpression:
                    return SyntaxKind.BitwiseAndExpression;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.ExclusiveOrExpression:
                    return SyntaxKind.ExclusiveOrExpression;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.EqualsExpression:
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.CaseEqualsClause:
                    return SyntaxKind.EqualsExpression;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.NotEqualsExpression:
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.CaseNotEqualsClause:
                    return SyntaxKind.NotEqualsExpression;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.GreaterThanExpression:
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.CaseGreaterThanClause:
                    return SyntaxKind.GreaterThanExpression;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.GreaterThanOrEqualExpression:
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.CaseGreaterThanOrEqualClause:
                    return SyntaxKind.GreaterThanOrEqualExpression;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.LessThanExpression:
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.CaseLessThanClause:
                    return SyntaxKind.LessThanExpression;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.LessThanOrEqualExpression:
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.CaseLessThanOrEqualClause:
                    return SyntaxKind.LessThanOrEqualExpression;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.IsExpression:
                    return SyntaxKind.EqualsExpression;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.IsNotExpression:
                    return SyntaxKind.NotEqualsExpression;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.LeftShiftExpression:
                    return SyntaxKind.LeftShiftExpression;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.RightShiftExpression:
                    return SyntaxKind.RightShiftExpression;
                // assignment
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.SimpleAssignmentStatement:
                    return SyntaxKind.SimpleAssignmentExpression;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.ConcatenateAssignmentStatement:
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.AddAssignmentStatement:
                    return SyntaxKind.AddAssignmentExpression;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.SubtractAssignmentStatement:
                    return SyntaxKind.SubtractAssignmentExpression;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.MultiplyAssignmentStatement:
                    return SyntaxKind.MultiplyAssignmentExpression;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.IntegerDivideAssignmentStatement:
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.DivideAssignmentStatement:
                    return SyntaxKind.DivideAssignmentExpression;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.LeftShiftAssignmentStatement:
                    return SyntaxKind.LeftShiftAssignmentExpression;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.RightShiftAssignmentStatement:
                    return SyntaxKind.RightShiftAssignmentExpression;
                // statements
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.AddHandlerAccessorStatement:
                    return SyntaxKind.AddAccessorDeclaration;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.RemoveHandlerAccessorStatement:
                    return SyntaxKind.RemoveAccessorDeclaration;
                // Casts
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.CObjKeyword:
                    return SyntaxKind.ObjectKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.CBoolKeyword:
                    return SyntaxKind.BoolKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.CCharKeyword:
                    return SyntaxKind.CharKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.CSByteKeyword:
                    return SyntaxKind.SByteKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.CByteKeyword:
                    return SyntaxKind.ByteKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.CShortKeyword:
                    return SyntaxKind.ShortKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.CUShortKeyword:
                    return SyntaxKind.UShortKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.CIntKeyword:
                    return SyntaxKind.IntKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.CUIntKeyword:
                    return SyntaxKind.UIntKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.CLngKeyword:
                    return SyntaxKind.LongKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.CULngKeyword:
                    return SyntaxKind.ULongKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.CDecKeyword:
                    return SyntaxKind.DecimalKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.CSngKeyword:
                    return SyntaxKind.FloatKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.CDblKeyword:
                    return SyntaxKind.DoubleKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.CStrKeyword:
                    return SyntaxKind.StringKeyword;
                // Converts
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.NarrowingKeyword:
                    return SyntaxKind.ExplicitKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.WideningKeyword:
                    return SyntaxKind.ImplicitKeyword;
                // Operator overloads
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.PlusToken:
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.AmpersandToken:
                    return SyntaxKind.PlusToken;  //Problematic clash if two operator overloads defined
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.MinusToken:
                    return SyntaxKind.MinusToken;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.NotKeyword:
                    return SyntaxKind.ExclamationToken;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.AsteriskToken:
                    return SyntaxKind.AsteriskToken;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.SlashToken:
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.BackslashToken:
                    return SyntaxKind.SlashToken;  //Problematic clash if two operator overloads defined
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.ModKeyword:
                    return SyntaxKind.PercentToken;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.LessThanLessThanToken:
                    return SyntaxKind.LessThanLessThanToken;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.GreaterThanGreaterThanToken:
                    return SyntaxKind.GreaterThanGreaterThanToken;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.EqualsToken:
                    return SyntaxKind.EqualsEqualsToken;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.LessThanGreaterThanToken:
                    return SyntaxKind.ExclamationEqualsToken;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.GreaterThanToken:
                    return SyntaxKind.GreaterThanToken;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.LessThanToken:
                    return SyntaxKind.LessThanToken;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.GreaterThanEqualsToken:
                    return SyntaxKind.GreaterThanEqualsToken;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.LessThanEqualsToken:
                    return SyntaxKind.LessThanEqualsToken;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.AndKeyword:
                    return SyntaxKind.AmpersandToken;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.OrKeyword:
                    return SyntaxKind.BarToken;
                //
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.AssemblyKeyword:
                    return SyntaxKind.AssemblyKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.AsyncKeyword:
                    return SyntaxKind.AsyncKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.AscendingOrdering:
                    return SyntaxKind.AscendingOrdering;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.DescendingOrdering:
                    return SyntaxKind.DescendingOrdering;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.AscendingKeyword:
                    return SyntaxKind.AscendingKeyword;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.DescendingKeyword:
                    return SyntaxKind.DescendingKeyword;

                // Not direct conversions
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.ExponentiateAssignmentStatement:
                    return SyntaxKind.SimpleAssignmentExpression;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.ExponentiateExpression:
                    break;
            }
            throw new NotSupportedException(t + " not supported!");
        }
    }
}
