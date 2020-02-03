// DO NOT REORDER DOCUMENT Tokens must be defined BEFORE they are used
using Microsoft.VisualBasic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

public static class VisualBasicSyntaxFactory
{
    public const string Quote = "\"";
    public const string DoubleQuote = "\"\"";
    public const SpecialType SystemString = SpecialType.System_String;
    public const char UnicodeOpenQuote = (char)0x201C;
    public const char UnicodeCloseQuote = (char)0x201D;
    public static string UnicodeDoubleOpenQuote = Conversions.ToString(UnicodeOpenQuote) + Conversions.ToString(UnicodeOpenQuote);
    public static string UnicodeDoubleCloseQuote = Conversions.ToString(UnicodeCloseQuote) + Conversions.ToString(UnicodeCloseQuote);
    public const char UnicodeFullWidthQuoationMark = (char)0xFF02;

    /* TODO ERROR: Skipped RegionDirectiveTrivia */
    public readonly static SyntaxToken AddressOfKeyword = SyntaxFactory.Token(SyntaxKind.AddressOfKeyword);
    public readonly static SyntaxToken AmpersandToken = SyntaxFactory.Token(SyntaxKind.AmpersandToken);
    public readonly static SyntaxToken AndAlsoKeyword = SyntaxFactory.Token(SyntaxKind.AndAlsoKeyword);
    public readonly static SyntaxToken AndKeyword = SyntaxFactory.Token(SyntaxKind.AndKeyword);
    public readonly static SyntaxToken AsKeyword = SyntaxFactory.Token(SyntaxKind.AsKeyword);
    public readonly static SyntaxToken AssemblyKeyword = SyntaxFactory.Token(SyntaxKind.AssemblyKeyword);
    public readonly static SyntaxToken AsterickToken = SyntaxFactory.Token(SyntaxKind.AsteriskToken);
    public readonly static SyntaxToken AsteriskEqualsToken = SyntaxFactory.Token(SyntaxKind.AsteriskEqualsToken);
    public readonly static SyntaxToken AsteriskToken = SyntaxFactory.Token(SyntaxKind.AsteriskToken);
    public readonly static SyntaxToken AsyncKeyword = SyntaxFactory.Token(SyntaxKind.AsyncKeyword);
    public readonly static SyntaxToken AwaitKeyword = SyntaxFactory.Token(SyntaxKind.AwaitKeyword);
    public readonly static SyntaxToken BeginCDataToken = SyntaxFactory.Token(SyntaxKind.BeginCDataToken);
    public readonly static SyntaxToken BlockKeyword = SyntaxFactory.Token(SyntaxKind.OperatorKeyword);
    public readonly static SyntaxToken BooleanKeyword = SyntaxFactory.Token(SyntaxKind.BooleanKeyword);
    public readonly static SyntaxToken ByRefKeyword = SyntaxFactory.Token(SyntaxKind.ByRefKeyword);
    public readonly static SyntaxToken ByteKeyword = SyntaxFactory.Token(SyntaxKind.ByteKeyword);
    public readonly static SyntaxToken ByValKeyword = SyntaxFactory.Token(SyntaxKind.ByValKeyword);
    public readonly static SyntaxToken CaseKeyword = SyntaxFactory.Token(SyntaxKind.CaseKeyword);
    public readonly static SyntaxToken CBoolKeyword = SyntaxFactory.Token(SyntaxKind.CBoolKeyword);
    public readonly static SyntaxToken CByteKeyword = SyntaxFactory.Token(SyntaxKind.CByteKeyword);
    public readonly static SyntaxToken CCharKeyword = SyntaxFactory.Token(SyntaxKind.CCharKeyword);
    public readonly static SyntaxToken CDateKeyword = SyntaxFactory.Token(SyntaxKind.CDateKeyword);
    public readonly static SyntaxToken CDblKeyword = SyntaxFactory.Token(SyntaxKind.CDblKeyword);
    public readonly static SyntaxToken CDecKeyword = SyntaxFactory.Token(SyntaxKind.CDecKeyword);
    public readonly static SyntaxToken CharKeyword = SyntaxFactory.Token(SyntaxKind.CharKeyword);
    public readonly static SyntaxToken CIntKeyword = SyntaxFactory.Token(SyntaxKind.CIntKeyword);
    public readonly static SyntaxToken ClassKeyWord = SyntaxFactory.Token(SyntaxKind.ClassKeyword);
    public readonly static SyntaxToken CLngKeyword = SyntaxFactory.Token(SyntaxKind.CLngKeyword);
    public readonly static SyntaxToken CloseBraceToken = SyntaxFactory.Token(SyntaxKind.CloseBraceToken);
    public readonly static SyntaxToken CloseParenToken = SyntaxFactory.Token(SyntaxKind.CloseParenToken);
    public readonly static SyntaxToken CObjKeyword = SyntaxFactory.Token(SyntaxKind.CObjKeyword);
    public readonly static SyntaxToken CommaToken = SyntaxFactory.Token(SyntaxKind.CommaToken);
    public readonly static SyntaxToken ConstKeyword = SyntaxFactory.Token(SyntaxKind.ConstKeyword);
    public readonly static SyntaxToken CSByteKeyword = SyntaxFactory.Token(SyntaxKind.CSByteKeyword);
    public readonly static SyntaxToken CShortKeyword = SyntaxFactory.Token(SyntaxKind.CShortKeyword);
    public readonly static SyntaxToken CSngKeyword = SyntaxFactory.Token(SyntaxKind.CSngKeyword);
    public readonly static SyntaxToken CStrKeyword = SyntaxFactory.Token(SyntaxKind.CStrKeyword);
    public readonly static SyntaxToken CTypeKeyword = SyntaxFactory.Token(SyntaxKind.CTypeKeyword);
    public readonly static SyntaxToken CUIntKeyword = SyntaxFactory.Token(SyntaxKind.CUIntKeyword);
    public readonly static SyntaxToken CULngKeyword = SyntaxFactory.Token(SyntaxKind.CULngKeyword);
    public readonly static SyntaxToken CUShortKeyword = SyntaxFactory.Token(SyntaxKind.CUShortKeyword);
    public readonly static SyntaxToken CustomKeyword = SyntaxFactory.Token(SyntaxKind.CustomKeyword);
    public readonly static SyntaxToken DateKeyword = SyntaxFactory.Token(SyntaxKind.DateKeyword);
    public readonly static SyntaxToken DecimalKeyword = SyntaxFactory.Token(SyntaxKind.DecimalKeyword);
    public readonly static SyntaxToken DefaultKeyword = SyntaxFactory.Token(SyntaxKind.DefaultKeyword);
    public readonly static SyntaxToken DimKeyword = SyntaxFactory.Token(SyntaxKind.DimKeyword);
    public readonly static SyntaxToken DoKeyword = SyntaxFactory.Token(SyntaxKind.DoKeyword);
    public readonly static SyntaxToken DotToken = SyntaxFactory.Token(SyntaxKind.DotToken);
    public readonly static SyntaxToken DoubleKeyword = SyntaxFactory.Token(SyntaxKind.DoubleKeyword);
    public readonly static SyntaxToken DoubleQuoteToken = SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken);
    public readonly static SyntaxToken ElseIfKeyword = SyntaxFactory.Token(SyntaxKind.ElseIfKeyword);
    public readonly static SyntaxToken ElseKeyword = SyntaxFactory.Token(SyntaxKind.ElseKeyword);
    public readonly static SyntaxToken EmptyToken = SyntaxFactory.Token(SyntaxKind.EmptyToken);
    public readonly static SyntaxToken EndCDataToken = SyntaxFactory.Token(SyntaxKind.EndCDataToken);
    public readonly static SyntaxToken EndKeyword = SyntaxFactory.Token(SyntaxKind.EndKeyword);
    public readonly static SyntaxToken EndOfFileToken = SyntaxFactory.Token(SyntaxKind.EndOfFileToken);
    public readonly static SyntaxToken EnumKeyword = SyntaxFactory.Token(SyntaxKind.EnumKeyword);
    public readonly static SyntaxToken EqualsToken = SyntaxFactory.Token(SyntaxKind.EqualsToken);
    public readonly static SyntaxToken ExternalChecksumKeyword = SyntaxFactory.Token(SyntaxKind.ExternalChecksumKeyword);
    public readonly static SyntaxToken ExternalSourceKeyword = SyntaxFactory.Token(SyntaxKind.ExternalSourceKeyword);
    public readonly static SyntaxToken FalseKeyword = SyntaxFactory.Token(SyntaxKind.FalseKeyword);
    public readonly static SyntaxToken ForKeyword = SyntaxFactory.Token(SyntaxKind.ForKeyword);
    public readonly static SyntaxToken FriendKeyword = SyntaxFactory.Token(SyntaxKind.FriendKeyword);
    public readonly static SyntaxToken FunctionKeyword = SyntaxFactory.Token(SyntaxKind.FunctionKeyword);
    public readonly static SyntaxToken GreaterThanEqualsToken = SyntaxFactory.Token(SyntaxKind.GreaterThanEqualsToken);
    public readonly static SyntaxToken GreaterThanGreaterThanEqualsToken = SyntaxFactory.Token(SyntaxKind.GreaterThanGreaterThanEqualsToken);
    public readonly static SyntaxToken GreaterThanGreaterThanToken = SyntaxFactory.Token(SyntaxKind.GreaterThanGreaterThanToken);
    public readonly static SyntaxToken GreaterThanToken = SyntaxFactory.Token(SyntaxKind.GreaterThanToken);
    public readonly static SyntaxToken HashToken = SyntaxFactory.Token(SyntaxKind.HashToken);
    public readonly static SyntaxToken IfKeyword = SyntaxFactory.Token(SyntaxKind.IfKeyword);
    public readonly static SyntaxToken InKeyword = SyntaxFactory.Token(SyntaxKind.InKeyword);
    public readonly static SyntaxToken IntegerKeyword = SyntaxFactory.Token(SyntaxKind.IntegerKeyword);
    public readonly static SyntaxToken InterfaceKeyword = SyntaxFactory.Token(SyntaxKind.InterfaceKeyword);
    public readonly static SyntaxToken IsFalse = SyntaxFactory.Token(SyntaxKind.IsFalseKeyword);
    public readonly static SyntaxToken IsTrueKeyword = SyntaxFactory.Token(SyntaxKind.IsTrueKeyword);
    public readonly static SyntaxToken IteratorKeyword = SyntaxFactory.Token(SyntaxKind.IteratorKeyword);
    public readonly static SyntaxToken KeyKeyword = SyntaxFactory.Token(SyntaxKind.KeyKeyword);
    public readonly static SyntaxToken LessThanEqualsToken = SyntaxFactory.Token(SyntaxKind.LessThanEqualsToken);
    public readonly static SyntaxToken LessThanGreaterThanToken = SyntaxFactory.Token(SyntaxKind.LessThanGreaterThanToken);
    public readonly static SyntaxToken LessThanLessThanEqualsToken = SyntaxFactory.Token(SyntaxKind.LessThanLessThanEqualsToken);
    public readonly static SyntaxToken LessThanLessThanToken = SyntaxFactory.Token(SyntaxKind.LessThanLessThanToken);
    public readonly static SyntaxToken LessThanToken = SyntaxFactory.Token(SyntaxKind.LessThanToken);
    public readonly static SyntaxToken LongKeyword = SyntaxFactory.Token(SyntaxKind.LongKeyword);
    public readonly static SyntaxToken MeKeyword = SyntaxFactory.Token(SyntaxKind.MeKeyword);
    public readonly static SyntaxToken MinusEqualsToken = SyntaxFactory.Token(SyntaxKind.MinusEqualsToken);
    public readonly static SyntaxToken MinusToken = SyntaxFactory.Token(SyntaxKind.MinusToken);
    public readonly static SyntaxToken ModKeyword = SyntaxFactory.Token(SyntaxKind.ModKeyword);
    public readonly static SyntaxToken ModuleKeyword = SyntaxFactory.Token(SyntaxKind.ModuleKeyword);
    public readonly static SyntaxToken MustInheritKeyword = SyntaxFactory.Token(SyntaxKind.MustInheritKeyword);
    public readonly static SyntaxToken MustOverrideKeyword = SyntaxFactory.Token(SyntaxKind.MustOverrideKeyword);
    public readonly static SyntaxToken MyBaseKeyword = SyntaxFactory.Token(SyntaxKind.MyBaseKeyword);
    public readonly static SyntaxToken NamespaceKeyword = SyntaxFactory.Token(SyntaxKind.NamespaceKeyword);
    public readonly static SyntaxToken NarrowingKeyword = SyntaxFactory.Token(SyntaxKind.NarrowingKeyword);
    public readonly static SyntaxToken NewKeyword = SyntaxFactory.Token(SyntaxKind.NewKeyword);
    public readonly static SyntaxToken NothingKeyword = SyntaxFactory.Token(SyntaxKind.NothingKeyword);
    public readonly static SyntaxToken NotInheritableKeyword = SyntaxFactory.Token(SyntaxKind.NotInheritableKeyword);
    public readonly static SyntaxToken NotKeyword = SyntaxFactory.Token(SyntaxKind.NotKeyword);
    public readonly static SyntaxToken NotOverridableKeyword = SyntaxFactory.Token(SyntaxKind.NotOverridableKeyword);
    public readonly static SyntaxToken ObjectKeyword = SyntaxFactory.Token(SyntaxKind.ObjectKeyword);
    public readonly static SyntaxToken OfKeyword = SyntaxFactory.Token(SyntaxKind.OfKeyword);
    public readonly static SyntaxToken OpenBraceToken = SyntaxFactory.Token(SyntaxKind.OpenBraceToken);
    public readonly static SyntaxToken OpenParenToken = SyntaxFactory.Token(SyntaxKind.OpenParenToken);
    public readonly static SyntaxToken OptionalKeyword = SyntaxFactory.Token(SyntaxKind.OptionalKeyword);
    public readonly static SyntaxToken OrElseKeyword = SyntaxFactory.Token(SyntaxKind.OrElseKeyword);
    public readonly static SyntaxToken OrKeyword = SyntaxFactory.Token(SyntaxKind.OrKeyword);
    public readonly static SyntaxToken OutKeyword = SyntaxFactory.Token(SyntaxKind.OutKeyword);
    public readonly static SyntaxToken OverridableKeyword = SyntaxFactory.Token(SyntaxKind.OverridableKeyword);
    public readonly static SyntaxToken OverridesKeyword = SyntaxFactory.Token(SyntaxKind.OverridesKeyword);
    public readonly static SyntaxToken ParamArrayKeyword = SyntaxFactory.Token(SyntaxKind.ParamArrayKeyword);
    public readonly static SyntaxToken PartialKeyword = SyntaxFactory.Token(SyntaxKind.PartialKeyword);
    public readonly static SyntaxToken PlusEqualsToken = SyntaxFactory.Token(SyntaxKind.PlusEqualsToken);
    public readonly static SyntaxToken PlusToken = SyntaxFactory.Token(SyntaxKind.PlusToken);
    public readonly static SyntaxToken PrivateKeyword = SyntaxFactory.Token(SyntaxKind.PrivateKeyword);
    public readonly static SyntaxToken PropertyKeyword = SyntaxFactory.Token(SyntaxKind.PropertyKeyword);
    public readonly static SyntaxToken ProtectedKeyword = SyntaxFactory.Token(SyntaxKind.ProtectedKeyword);
    public readonly static SyntaxToken PublicKeyword = SyntaxFactory.Token(SyntaxKind.PublicKeyword);
    public readonly static SyntaxToken QuestionToken = SyntaxFactory.Token(SyntaxKind.QuestionToken);
    public readonly static SyntaxToken ReadOnlyKeyword = SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword);
    public readonly static SyntaxToken RegionKeyword = SyntaxFactory.Token(SyntaxKind.RegionKeyword);
    public readonly static SyntaxToken SByteKeyword = SyntaxFactory.Token(SyntaxKind.SByteKeyword);
    public readonly static SyntaxToken SelectKeyword = SyntaxFactory.Token(SyntaxKind.SelectKeyword);
    public readonly static SyntaxToken ShadowsKeyword = SyntaxFactory.Token(SyntaxKind.ShadowsKeyword);
    public readonly static SyntaxToken SharedKeyword = SyntaxFactory.Token(SyntaxKind.SharedKeyword);
    public readonly static SyntaxToken ShortKeyword = SyntaxFactory.Token(SyntaxKind.ShortKeyword);
    public readonly static SyntaxToken SingleKeyword = SyntaxFactory.Token(SyntaxKind.SingleKeyword);
    public readonly static SyntaxToken SlashEqualsToken = SyntaxFactory.Token(SyntaxKind.SlashEqualsToken);
    public readonly static SyntaxToken SlashToken = SyntaxFactory.Token(SyntaxKind.SlashToken);
    public readonly static SyntaxToken StringKeyword = SyntaxFactory.Token(SyntaxKind.StringKeyword);
    public readonly static SyntaxToken StructureKeyword = SyntaxFactory.Token(SyntaxKind.StructureKeyword);
    public readonly static SyntaxToken SubKeyword = SyntaxFactory.Token(SyntaxKind.SubKeyword);
    public readonly static SyntaxToken ThenKeyword = SyntaxFactory.Token(SyntaxKind.ThenKeyword);
    public readonly static SyntaxToken ToKeyword = SyntaxFactory.Token(SyntaxKind.ToKeyword);
    public readonly static SyntaxToken TrueKeyword = SyntaxFactory.Token(SyntaxKind.TrueKeyword);
    public readonly static SyntaxToken TryCastKeyword = SyntaxFactory.Token(SyntaxKind.TryCastKeyword);
    public readonly static SyntaxToken TryKeyword = SyntaxFactory.Token(SyntaxKind.TryKeyword);
    public readonly static SyntaxToken TypeKeyword = SyntaxFactory.Token(SyntaxKind.TypeKeyword);
    public readonly static SyntaxToken UIntegerKeyword = SyntaxFactory.Token(SyntaxKind.UIntegerKeyword);
    public readonly static SyntaxToken ULongKeyword = SyntaxFactory.Token(SyntaxKind.ULongKeyword);
    public readonly static SyntaxToken WhileKeyword = SyntaxFactory.Token(SyntaxKind.WhileKeyword);
    public readonly static SyntaxToken WideningKeyword = SyntaxFactory.Token(SyntaxKind.WideningKeyword);
    public readonly static SyntaxToken WithKeyword = SyntaxFactory.Token(SyntaxKind.WithKeyword);
    public readonly static SyntaxToken WriteOnlyKeyword = SyntaxFactory.Token(SyntaxKind.WriteOnlyKeyword);
    public readonly static SyntaxToken XorKeyword = SyntaxFactory.Token(SyntaxKind.XorKeyword);
    public readonly static SyntaxToken UShortKeyword = SyntaxFactory.Token(SyntaxKind.UShortKeyword);

    /* TODO ERROR: Skipped RegionDirectiveTrivia */
    public readonly static SyntaxToken ExplicitToken = SyntaxFactory.Token(SyntaxKind.ExplicitKeyword);
    public readonly static SyntaxToken InferToken = SyntaxFactory.Token(SyntaxKind.InferKeyword);
    public readonly static SyntaxToken StrictToken = SyntaxFactory.Token(SyntaxKind.StrictKeyword);
    public readonly static SyntaxToken OnToken = SyntaxFactory.Token(SyntaxKind.OnKeyword);
    public readonly static SyntaxToken OffToken = SyntaxFactory.Token(SyntaxKind.OffKeyword);

    /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
    /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
    /* TODO ERROR: Skipped RegionDirectiveTrivia */
    public readonly static Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeSyntax HandleRefType = SyntaxFactory.ParseTypeName("HandleRefType");
    public readonly static Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeSyntax IntPtrType = SyntaxFactory.ParseTypeName("IntPtr");
    public readonly static Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeSyntax PredefinedTypeBoolean = SyntaxFactory.PredefinedType(BooleanKeyword);
    public readonly static Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeSyntax PredefinedTypeByte = SyntaxFactory.PredefinedType(ByteKeyword);
    public readonly static Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeSyntax PredefinedTypeChar = SyntaxFactory.PredefinedType(CharKeyword);
    public readonly static Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeSyntax PredefinedTypeDate = SyntaxFactory.PredefinedType(DateKeyword);
    public readonly static Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeSyntax PredefinedTypeDecimal = SyntaxFactory.PredefinedType(DecimalKeyword);
    public readonly static Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeSyntax PredefinedTypeDouble = SyntaxFactory.PredefinedType(DoubleKeyword);
    public readonly static Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeSyntax PredefinedTypeInteger = SyntaxFactory.PredefinedType(IntegerKeyword);
    public readonly static Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeSyntax PredefinedTypeLong = SyntaxFactory.PredefinedType(LongKeyword);
    public readonly static Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeSyntax PredefinedTypeObject = SyntaxFactory.PredefinedType(ObjectKeyword);
    public readonly static Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeSyntax PredefinedTypeSByte = SyntaxFactory.PredefinedType(SByteKeyword);
    public readonly static Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeSyntax PredefinedTypeShort = SyntaxFactory.PredefinedType(ShortKeyword);
    public readonly static Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeSyntax PredefinedTypeSingle = SyntaxFactory.PredefinedType(SingleKeyword);
    public readonly static Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeSyntax PredefinedTypeString = SyntaxFactory.PredefinedType(StringKeyword);
    public readonly static Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeSyntax PredefinedTypeUInteger = SyntaxFactory.PredefinedType(UIntegerKeyword);
    public readonly static Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeSyntax PredefinedTypeULong = SyntaxFactory.PredefinedType(ULongKeyword);
    public readonly static Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeSyntax PredefinedTypeUShort = SyntaxFactory.PredefinedType(UShortKeyword);

    /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
    /* TODO ERROR: Skipped RegionDirectiveTrivia */
    public readonly static SyntaxTrivia ElasticMarker = SyntaxFactory.ElasticWhitespace(string.Empty);
    public readonly static SyntaxTrivia LineContinuation = SyntaxFactory.LineContinuationTrivia("_");
    public readonly static SyntaxTrivia SpaceTrivia = SyntaxFactory.Space;
    public readonly static SyntaxTrivia VBEOLTrivia = SyntaxFactory.EndOfLineTrivia(Constants.vbCrLf);

    /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
    /* TODO ERROR: Skipped RegionDirectiveTrivia */
    public readonly static Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax IntPrtSizeExpression = SyntaxFactory.ParseExpression("IntPrt.Size");
    public readonly static Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax ExpressionD1 = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1));
    public readonly static Microsoft.CodeAnalysis.VisualBasic.Syntax.LiteralExpressionSyntax NothingExpression = SyntaxFactory.NothingLiteralExpression(NothingKeyword);
    public readonly static Microsoft.CodeAnalysis.VisualBasic.Syntax.IdentifierNameSyntax VBCrLfExpression = SyntaxFactory.IdentifierName("vbCrLf");

    /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
    public readonly static SyntaxTokenList DimModifier = SyntaxFactory.TokenList(DimKeyword);
    public readonly static Microsoft.CodeAnalysis.VisualBasic.Syntax.ModifiedIdentifierSyntax ValueModifiedIdentifier = SyntaxFactory.ModifiedIdentifier("Value");
    public readonly static SyntaxTokenList PublicModifier = SyntaxFactory.TokenList(PublicKeyword);
    public readonly static Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeSyntax RuntimeInteropServicesOut = SyntaxFactory.ParseTypeName("Runtime.InteropServices.Out");
}

