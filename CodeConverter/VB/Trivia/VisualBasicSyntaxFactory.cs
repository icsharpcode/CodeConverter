// DO NOT REORDER DOCUMENT Tokens must be defined BEFORE they are used

using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Constants = Microsoft.VisualBasic.Constants;

namespace ICSharpCode.CodeConverter.VB.Trivia;

public static class VisualBasicSyntaxFactory
{
    public static readonly SyntaxToken NothingKeyword = SyntaxFactory.Token(SyntaxKind.NothingKeyword);
    /* TODO ERROR: Skipped RegionDirectiveTrivia */
    public static readonly Microsoft.CodeAnalysis.VisualBasic.Syntax.LiteralExpressionSyntax NothingExpression = SyntaxFactory.NothingLiteralExpression(NothingKeyword);
    public static readonly SyntaxToken AndAlsoKeyword = SyntaxFactory.Token(SyntaxKind.AndAlsoKeyword);
    public static readonly SyntaxToken AndKeyword = SyntaxFactory.Token(SyntaxKind.AndKeyword);
    public static readonly SyntaxToken AsteriskToken = SyntaxFactory.Token(SyntaxKind.AsteriskToken);
    public static readonly SyntaxToken BeginCDataToken = SyntaxFactory.Token(SyntaxKind.BeginCDataToken);
    public static readonly SyntaxToken BooleanKeyword = SyntaxFactory.Token(SyntaxKind.BooleanKeyword);
    public static readonly SyntaxToken ByteKeyword = SyntaxFactory.Token(SyntaxKind.ByteKeyword);
    public static readonly SyntaxToken CharKeyword = SyntaxFactory.Token(SyntaxKind.CharKeyword);
    public static readonly SyntaxToken CloseParenToken = SyntaxFactory.Token(SyntaxKind.CloseParenToken);
    public static readonly SyntaxToken CommaToken = SyntaxFactory.Token(SyntaxKind.CommaToken);
    public static readonly SyntaxToken DecimalKeyword = SyntaxFactory.Token(SyntaxKind.DecimalKeyword);
    public static readonly SyntaxToken DotToken = SyntaxFactory.Token(SyntaxKind.DotToken);
    public static readonly SyntaxToken DoubleKeyword = SyntaxFactory.Token(SyntaxKind.DoubleKeyword);
    public static readonly SyntaxToken DoubleQuoteToken = SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken);
    public static readonly SyntaxToken ElseIfKeyword = SyntaxFactory.Token(SyntaxKind.ElseIfKeyword);
    public static readonly SyntaxToken EmptyToken = SyntaxFactory.Token(SyntaxKind.EmptyToken);
    public static readonly SyntaxToken EndCDataToken = SyntaxFactory.Token(SyntaxKind.EndCDataToken);
    public static readonly SyntaxToken EndKeyword = SyntaxFactory.Token(SyntaxKind.EndKeyword);
    public static readonly SyntaxToken EqualsToken = SyntaxFactory.Token(SyntaxKind.EqualsToken);
    public static readonly SyntaxToken ExternalChecksumKeyword = SyntaxFactory.Token(SyntaxKind.ExternalChecksumKeyword);
    public static readonly SyntaxToken GreaterThanEqualsToken = SyntaxFactory.Token(SyntaxKind.GreaterThanEqualsToken);
    public static readonly SyntaxToken GreaterThanToken = SyntaxFactory.Token(SyntaxKind.GreaterThanToken);
    public static readonly SyntaxToken HashToken = SyntaxFactory.Token(SyntaxKind.HashToken);
    public static readonly SyntaxToken IfKeyword = SyntaxFactory.Token(SyntaxKind.IfKeyword);
    public static readonly SyntaxToken IntegerKeyword = SyntaxFactory.Token(SyntaxKind.IntegerKeyword);
    public static readonly SyntaxToken LessThanEqualsToken = SyntaxFactory.Token(SyntaxKind.LessThanEqualsToken);
    public static readonly SyntaxToken LessThanGreaterThanToken = SyntaxFactory.Token(SyntaxKind.LessThanGreaterThanToken);
    public static readonly SyntaxToken LessThanToken = SyntaxFactory.Token(SyntaxKind.LessThanToken);
    public static readonly SyntaxToken LongKeyword = SyntaxFactory.Token(SyntaxKind.LongKeyword);
    public static readonly SyntaxToken MinusEqualsToken = SyntaxFactory.Token(SyntaxKind.MinusEqualsToken);
    public static readonly SyntaxToken MinusToken = SyntaxFactory.Token(SyntaxKind.MinusToken);
    public static readonly SyntaxToken ModKeyword = SyntaxFactory.Token(SyntaxKind.ModKeyword);
    public static readonly SyntaxToken NotKeyword = SyntaxFactory.Token(SyntaxKind.NotKeyword);
    public static readonly SyntaxToken ObjectKeyword = SyntaxFactory.Token(SyntaxKind.ObjectKeyword);
    public static readonly SyntaxToken OpenParenToken = SyntaxFactory.Token(SyntaxKind.OpenParenToken);
    public static readonly SyntaxToken OrElseKeyword = SyntaxFactory.Token(SyntaxKind.OrElseKeyword);
    public static readonly SyntaxToken OrKeyword = SyntaxFactory.Token(SyntaxKind.OrKeyword);
    public static readonly SyntaxToken PlusEqualsToken = SyntaxFactory.Token(SyntaxKind.PlusEqualsToken);
    public static readonly SyntaxToken PlusToken = SyntaxFactory.Token(SyntaxKind.PlusToken);
    public static readonly SyntaxToken RegionKeyword = SyntaxFactory.Token(SyntaxKind.RegionKeyword);
    public static readonly SyntaxToken SByteKeyword = SyntaxFactory.Token(SyntaxKind.SByteKeyword);
    public static readonly SyntaxToken ShortKeyword = SyntaxFactory.Token(SyntaxKind.ShortKeyword);
    public static readonly SyntaxToken SingleKeyword = SyntaxFactory.Token(SyntaxKind.SingleKeyword);
    public static readonly SyntaxToken SlashToken = SyntaxFactory.Token(SyntaxKind.SlashToken);
    public static readonly SyntaxToken StringKeyword = SyntaxFactory.Token(SyntaxKind.StringKeyword);
    public static readonly SyntaxToken TrueKeyword = SyntaxFactory.Token(SyntaxKind.TrueKeyword);
    public static readonly SyntaxToken UIntegerKeyword = SyntaxFactory.Token(SyntaxKind.UIntegerKeyword);
    public static readonly SyntaxToken ULongKeyword = SyntaxFactory.Token(SyntaxKind.ULongKeyword);
    public static readonly SyntaxToken UShortKeyword = SyntaxFactory.Token(SyntaxKind.UShortKeyword);
    
    public static readonly SyntaxTrivia SpaceTrivia = SyntaxFactory.Space;
    public static readonly SyntaxTrivia VBEOLTrivia = SyntaxFactory.EndOfLineTrivia(Constants.vbCrLf);
}