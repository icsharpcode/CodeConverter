using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis.CSharp;
using ExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using CSSyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using Microsoft.CodeAnalysis;
using ICSharpCode.CodeConverter.Util.FromRoslyn;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class LiteralConversions
    {
        private static readonly char[] _vbTypeLiteralNonHexSpecifierCharacters = "%&@!#SILRUsilru".ToCharArray();
        private static readonly char[] _vbTypeLiteralSpecifierCharacters = "%&@!#SILDFRUsildfru".ToCharArray();

        public static ExpressionSyntax GetLiteralExpression(object value, string textForUser = null, ITypeSymbol convertedType = null)
        {
            if (value is string valueTextForCompiler) {
                textForUser = textForUser == null
                    ? SymbolDisplay.FormatLiteral(valueTextForCompiler, true)
                    : GetQuotedStringTextForUser(textForUser, valueTextForCompiler);
                return SyntaxFactory.LiteralExpression(CSSyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(textForUser, valueTextForCompiler));
            }

            if (value == null)
                return SyntaxFactory.LiteralExpression(CSSyntaxKind.NullLiteralExpression);
            if (value is bool)
                return SyntaxFactory.LiteralExpression((bool)value ? CSSyntaxKind.TrueLiteralExpression : CSSyntaxKind.FalseLiteralExpression);

            // The value is passed as an int from VB expression: "3"
            // Important to use value text, otherwise "10.0" gets coerced to and integer literal of 10 which can change semantics
            value = ConvertLiteralNumericValueOrNull(value, convertedType) ?? value;

            textForUser = ConvertNumericLiteralValueText(textForUser ?? value.ToString(), value);
            
            switch (value)
            {
                case byte b:
                    return NumericLiteral(SyntaxFactory.Literal(textForUser, b));
                case sbyte sb:
                    return NumericLiteral(SyntaxFactory.Literal(textForUser, sb));
                case short s:
                    return NumericLiteral(SyntaxFactory.Literal(textForUser, s));
                case ushort us:
                    return NumericLiteral(SyntaxFactory.Literal(textForUser, us));
                case int i:
                    return NumericLiteral(SyntaxFactory.Literal(textForUser, i));
                case uint u:
                    return NumericLiteral(SyntaxFactory.Literal(textForUser, u));
                case long l:
                    return NumericLiteral(SyntaxFactory.Literal(textForUser, l));
                case ulong ul:
                    return NumericLiteral(SyntaxFactory.Literal(textForUser, ul));
                case double d:
                    return NumericLiteral(SyntaxFactory.Literal(textForUser, d));
                case float f:
                    return NumericLiteral(SyntaxFactory.Literal(textForUser, f));
                case decimal dec:
                    return NumericLiteral(SyntaxFactory.Literal(textForUser, dec));
                case char c:
                    return SyntaxFactory.LiteralExpression(CSSyntaxKind.CharacterLiteralExpression, SyntaxFactory.Literal(c));
                case DateTime dt:
                {
                    var valueToOutput = dt.Date.Equals(dt) ? dt.ToString("yyyy-MM-dd") : dt.ToString("yyyy-MM-dd HH:mm:ss");
                    return SyntaxFactory.ParseExpression("DateTime.Parse(\"" + valueToOutput + "\")");
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        private static LiteralExpressionSyntax NumericLiteral(SyntaxToken literal) => SyntaxFactory.LiteralExpression(CSSyntaxKind.NumericLiteralExpression, literal);
        
        /// <summary>
        /// See LiteralConversions.GetLiteralExpression
        /// These are all the literals where the type will already be correct from the literal declaration
        /// </summary>
        public static object ConvertLiteralNumericValueOrNull(object value, ITypeSymbol vbConvertedType) =>
            vbConvertedType?.SpecialType switch {
                SpecialType.System_Int32 => Convert.ToInt32(value), //Special case since it's the C# default and doesn't need a suffix like the rest
                SpecialType.System_UInt32 => Convert.ToUInt32(value),
                SpecialType.System_Int64 => Convert.ToInt64(value),
                SpecialType.System_UInt64=> Convert.ToUInt64(value),
                SpecialType.System_Single => Convert.ToSingle(value),
                SpecialType.System_Double => Convert.ToDouble(value),
                SpecialType.System_Decimal => Convert.ToDecimal(value),
                _ => null
            };

        internal static string GetQuotedStringTextForUser(string textForUser, string valueTextForCompiler)
        {
            var sourceUnquotedTextForUser = Unquote(textForUser);
            var worthBeingAVerbatimString = IsWorthBeingAVerbatimString(valueTextForCompiler);
            var destQuotedTextForUser =
                $"\"{EscapeQuotes(sourceUnquotedTextForUser, valueTextForCompiler, worthBeingAVerbatimString)}\"";

            return worthBeingAVerbatimString ? "@" + destQuotedTextForUser : destQuotedTextForUser;

        }

        internal static string EscapeQuotes(string unquotedTextForUser, string valueTextForCompiler, bool isVerbatimString)
        {
            if (isVerbatimString) {
                return valueTextForCompiler.Replace("\"", "\"\"");
            } else {
                return unquotedTextForUser.Replace("\"\"", "\\\"");
            }
        }

        internal static string EscapeEscapeChar(string valueTextForCompiler)
        {
                return valueTextForCompiler.Replace("\\", "\\\\");
        }
        private static string Unquote(string quotedText)
        {
            quotedText = quotedText.Replace('”', '"');
            int firstQuoteIndex = quotedText.IndexOf("\"");
            int lastQuoteIndex = quotedText.LastIndexOf("\"");
            var unquoted = quotedText.Substring(firstQuoteIndex + 1, lastQuoteIndex - firstQuoteIndex - 1);
            return unquoted;
        }

        public static bool IsWorthBeingAVerbatimString(string s1)
        {
            return s1.IndexOfAny(new[] {'\r', '\n', '\\'}) > -1;
        }

        /// <summary>
        ///  https://docs.microsoft.com/en-us/dotnet/visual-basic/programming-guide/language-features/data-types/type-characters
        ///  https://stackoverflow.com/a/166762/1128762
        /// </summary>
        private static string ConvertNumericLiteralValueText(string textForUser, object value)
        {
            bool canBeBinaryOrHex = value switch {
                double _ => false,
                float _ => false,
                decimal _ => false,
                _ => true
            };

            textForUser = textForUser.TrimEnd(_vbTypeLiteralNonHexSpecifierCharacters);

            if (canBeBinaryOrHex && textForUser.StartsWith("&H", StringComparison.OrdinalIgnoreCase)) {
                textForUser = "0x" + textForUser.Substring(2);
            } else if (canBeBinaryOrHex && textForUser.StartsWith("&B", StringComparison.OrdinalIgnoreCase)) {
                textForUser = "0b" + textForUser.Substring(2);
            } else if (textForUser.StartsWith("&", StringComparison.OrdinalIgnoreCase)) {
                textForUser = value.ToString();
            } else {
                textForUser = textForUser.TrimEnd(_vbTypeLiteralSpecifierCharacters);
            }

            if (value switch {
                ulong _ => "UL",
                long _ => "L",
                uint _ => "U",
                int _ => "",
                ushort _ => "",
                short _ => "",
                double _ => "d",
                float _ => "f",
                decimal _ => "m",
                _ => default
            } is {} suffix ) {
                textForUser += suffix;
            }

            return textForUser;
        }
    }
}