using System;
using ICSharpCode.CodeConverter.Shared;
using Microsoft.CodeAnalysis.VisualBasic;

namespace ICSharpCode.CodeConverter.VB
{
    public static class SyntaxKindExtensions
    {
        public static SyntaxKind ConvertToken(this Microsoft.CodeAnalysis.CSharp.SyntaxKind t, TokenContext context = TokenContext.Global)
        {
            switch (t)
            {
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.None:
                    return SyntaxKind.None;
                // built-in types
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.BoolKeyword:
                    return SyntaxKind.BooleanKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.ByteKeyword:
                    return SyntaxKind.ByteKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.SByteKeyword:
                    return SyntaxKind.SByteKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.ShortKeyword:
                    return SyntaxKind.ShortKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.UShortKeyword:
                    return SyntaxKind.UShortKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.IntKeyword:
                    return SyntaxKind.IntegerKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.UIntKeyword:
                    return SyntaxKind.UIntegerKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.LongKeyword:
                    return SyntaxKind.LongKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.ULongKeyword:
                    return SyntaxKind.ULongKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.DoubleKeyword:
                    return SyntaxKind.DoubleKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.FloatKeyword:
                    return SyntaxKind.SingleKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.DecimalKeyword:
                    return SyntaxKind.DecimalKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.StringKeyword:
                    return SyntaxKind.StringKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.CharKeyword:
                    return SyntaxKind.CharKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.VoidKeyword:
                    // not supported
                    return SyntaxKind.None;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.ObjectKeyword:
                    return SyntaxKind.ObjectKeyword;
                // literals
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.NullKeyword:
                    return SyntaxKind.NothingKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.TrueKeyword:
                    return SyntaxKind.TrueKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.FalseKeyword:
                    return SyntaxKind.FalseKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.ThisKeyword:
                    return SyntaxKind.MeKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.BaseKeyword:
                    return SyntaxKind.MyBaseKeyword;
                // modifiers
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.PublicKeyword:
                    return SyntaxKind.PublicKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.PrivateKeyword:
                    return SyntaxKind.PrivateKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.InternalKeyword:
                    return SyntaxKind.FriendKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.ProtectedKeyword:
                    return SyntaxKind.ProtectedKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.StaticKeyword:
                    return context == TokenContext.Global ? SyntaxKind.NotInheritableKeyword : SyntaxKind.SharedKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.ReadOnlyKeyword:
                    return SyntaxKind.ReadOnlyKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.SealedKeyword:
                    return context == TokenContext.Global ? SyntaxKind.NotInheritableKeyword : SyntaxKind.NotOverridableKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.ConstKeyword:
                    return SyntaxKind.ConstKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.OverrideKeyword:
                    return SyntaxKind.OverridesKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.AbstractKeyword:
                    return context == TokenContext.Global ? SyntaxKind.MustInheritKeyword : SyntaxKind.MustOverrideKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.VirtualKeyword:
                    return SyntaxKind.OverridableKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.RefKeyword:
                    return SyntaxKind.ByRefKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.OutKeyword:
                    return SyntaxKind.ByRefKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword:
                    return SyntaxKind.PartialKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.AsyncKeyword:
                    return SyntaxKind.AsyncKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.ExternKeyword:
                    // not supported
                    return SyntaxKind.None;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.NewKeyword:
                    return SyntaxKind.OverloadsKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.ParamsKeyword:
                    return SyntaxKind.ParamArrayKeyword;
                // others
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.AscendingKeyword:
                    return SyntaxKind.AscendingKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.DescendingKeyword:
                    return SyntaxKind.DescendingKeyword;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.AwaitKeyword:
                    return SyntaxKind.AwaitKeyword;
                // expressions
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.AddExpression:
                    return SyntaxKind.AddExpression;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.SubtractExpression:
                    return SyntaxKind.SubtractExpression;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.MultiplyExpression:
                    return SyntaxKind.MultiplyExpression;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.DivideExpression:
                    return SyntaxKind.DivideExpression;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.ModuloExpression:
                    return SyntaxKind.ModuloExpression;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.LeftShiftExpression:
                    return SyntaxKind.LeftShiftExpression;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.RightShiftExpression:
                    return SyntaxKind.RightShiftExpression;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.LogicalOrExpression:
                    return SyntaxKind.OrElseExpression;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.LogicalAndExpression:
                    return SyntaxKind.AndAlsoExpression;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.BitwiseOrExpression:
                    return SyntaxKind.OrExpression;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.BitwiseAndExpression:
                    return SyntaxKind.AndExpression;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.ExclusiveOrExpression:
                    return SyntaxKind.ExclusiveOrExpression;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.EqualsExpression:
                    return SyntaxKind.EqualsExpression;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.NotEqualsExpression:
                    return SyntaxKind.NotEqualsExpression;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.LessThanExpression:
                    return SyntaxKind.LessThanExpression;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.LessThanOrEqualExpression:
                    return SyntaxKind.LessThanOrEqualExpression;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.GreaterThanExpression:
                    return SyntaxKind.GreaterThanExpression;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.GreaterThanOrEqualExpression:
                    return SyntaxKind.GreaterThanOrEqualExpression;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleAssignmentExpression:
                    return SyntaxKind.SimpleAssignmentStatement;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.AddAssignmentExpression:
                    return SyntaxKind.AddAssignmentStatement;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.SubtractAssignmentExpression:
                    return SyntaxKind.SubtractAssignmentStatement;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.MultiplyAssignmentExpression:
                    return SyntaxKind.MultiplyAssignmentStatement;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.DivideAssignmentExpression:
                    return SyntaxKind.DivideAssignmentStatement;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.ModuloAssignmentExpression:
                    return SyntaxKind.ModuloExpression;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.AndAssignmentExpression:
                    return SyntaxKind.AndExpression;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.ExclusiveOrAssignmentExpression:
                    return SyntaxKind.ExclusiveOrExpression;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.OrAssignmentExpression:
                    return SyntaxKind.OrExpression;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.LeftShiftAssignmentExpression:
                    return SyntaxKind.LeftShiftAssignmentStatement;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.RightShiftAssignmentExpression:
                    return SyntaxKind.RightShiftAssignmentStatement;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.UnaryPlusExpression:
                    return SyntaxKind.UnaryPlusExpression;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.UnaryMinusExpression:
                    return SyntaxKind.UnaryMinusExpression;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.BitwiseNotExpression:
                    return SyntaxKind.NotExpression;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.LogicalNotExpression:
                    return SyntaxKind.NotExpression;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.PreIncrementExpression:
                    return SyntaxKind.AddAssignmentStatement;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.PreDecrementExpression:
                    return SyntaxKind.SubtractAssignmentStatement;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.PostIncrementExpression:
                    return SyntaxKind.AddAssignmentStatement;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.PostDecrementExpression:
                    return SyntaxKind.SubtractAssignmentStatement;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.PlusPlusToken:
                    return SyntaxKind.PlusToken;
                case Microsoft.CodeAnalysis.CSharp.SyntaxKind.MinusMinusToken:
                    return SyntaxKind.MinusToken;
            }

            throw new NotSupportedException(t + " is not supported!");
        }
    }
}
