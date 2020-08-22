using System;
using System.Collections.Generic;
using System.Text;

namespace ICSharpCode.CodeConverter.Util
{
    internal enum UnicodeNewline
    {
        Unknown,

        /// <summary>
        /// Line Feed, U+000A
        /// </summary>
        LF = 0x0A,

        CRLF = 0x0D0A,

        /// <summary>
        /// Carriage Return, U+000D
        /// </summary>
        CR = 0x0D,

        /// <summary>
        /// Next Line, U+0085
        /// </summary>
        NEL = 0x85,

        /// <summary>
        /// Vertical Tab, U+000B
        /// </summary>
        VT = 0x0B,

        /// <summary>
        /// Form Feed, U+000C
        /// </summary>
        FF = 0x0C,

        /// <summary>
        /// Line Separator, U+2028
        /// </summary>
        LS = 0x2028,

        /// <summary>
        /// Paragraph Separator, U+2029
        /// </summary>
        PS = 0x2029
    }

    /// <summary>
    /// Defines unicode new lines according to  Unicode Technical Report #13
    /// http://www.unicode.org/standard/reports/tr13/tr13-5.html
    /// </summary>
    internal static class NewLine
    {
        /// <summary>
        /// Carriage Return, U+000D
        /// </summary>
        public const char CR = (char)0x0D;

        /// <summary>
        /// Line Feed, U+000A
        /// </summary>
        public const char LF = (char)0x0A;

        /// <summary>
        /// Next Line, U+0085
        /// </summary>
        public const char NEL = (char)0x85;

        /// <summary>
        /// Vertical Tab, U+000B
        /// </summary>
        public const char VT = (char)0x0B;

        /// <summary>
        /// Form Feed, U+000C
        /// </summary>
        public const char FF = (char)0x0C;

        /// <summary>
        /// Line Separator, U+2028
        /// </summary>
        public const char LS = (char)0x2028;

        /// <summary>
        /// Paragraph Separator, U+2029
        /// </summary>
        public const char PS = (char)0x2029;

        /// <summary>
        /// Determines if a char is a new line delimiter.
        /// </summary>
        /// <returns>0 == no new line, otherwise it returns either 1 or 2 depending of the length of the delimiter.</returns>
        /// <param name="curChar">The current character.</param>
        /// <param name="nextChar">The next character (if != LF then length will always be 0 or 1).</param>
        public static int GetDelimiterLength(char curChar, char nextChar)
        {
            if (curChar == CR) {
                if (nextChar == LF)
                    return 2;
                return 1;
            }

            if (curChar == LF || curChar == NEL || curChar == VT || curChar == FF || curChar == LS || curChar == PS)
                return 1;
            return 0;
        }

        /// <summary>
        /// Determines if a char is a new line delimiter.
        /// </summary>
        /// <returns>0 == no new line, otherwise it returns either 1 or 2 depending of the length of the delimiter.</returns>
        /// <param name="curChar">The current character.</param>
        /// <param name = "length">The length of the delimiter</param>
        /// <param name = "type">The type of the delimiter</param>
        /// <param name="nextChar">A callback getting the next character (may be null).</param>
        public static bool TryGetDelimiterLengthAndType(char curChar, out int length, out UnicodeNewline type, Func<char> nextChar = null)
        {
            if (curChar == CR) {
                if (nextChar != null && nextChar() == LF) {
                    length = 2;
                    type = UnicodeNewline.CRLF;
                } else {
                    length = 1;
                    type = UnicodeNewline.CR;
                }
                return true;
            }

            switch (curChar) {
                case LF:
                    type = UnicodeNewline.LF;
                    length = 1;
                    return true;
                case NEL:
                    type = UnicodeNewline.NEL;
                    length = 1;
                    return true;
                case VT:
                    type = UnicodeNewline.VT;
                    length = 1;
                    return true;
                case FF:
                    type = UnicodeNewline.FF;
                    length = 1;
                    return true;
                case LS:
                    type = UnicodeNewline.LS;
                    length = 1;
                    return true;
                case PS:
                    type = UnicodeNewline.PS;
                    length = 1;
                    return true;
            }
            length = -1;
            type = UnicodeNewline.Unknown;
            return false;
        }


        /// <summary>
        /// Determines if a string is a new line delimiter.
        /// 
        /// Note that the only 2 char wide new line is CR LF
        /// </summary>
        public static bool IsNewLine(this string str)
        {
            if (str == null || str.Length == 0) {
                return false;
            }
            char ch = str[0];
            var switchExpr = str.Length;
            switch (switchExpr) {
                case 0: {
                        return false;
                    }

                case 1:
                case 2: {
                        return ch == CR || ch == LF || ch == NEL || ch == VT || ch == FF || ch == LS || ch == PS;
                    }

                default: {
                        return false;
                    }
            }
        }

        /// <summary>
        /// Replace Unicode NewLines with ControlChars.NullChar or Specified Character
        /// </summary>
        /// <param name="text">Source Test</param>
        /// <param name="SubstituteChar">Default is vbNullChar</param>
        /// <returns>String with Unicode NewLines replaced with SubstituteChar</returns>
        public static string WithoutNewLines(this string text, char SubstituteChar = default)
        {
            System.Diagnostics.Contracts.Contract.Requires(text != null);
            var sb = new StringBuilder();
            int length = default(int);
            UnicodeNewline type = default(UnicodeNewline);

            for (int i = 0, loopTo = text.Length - 1; i <= loopTo; i++) {
                char ch = text[i];
                // Do not delete the next line
                int j = i;
                if (TryGetDelimiterLengthAndType(ch, out length, out type, () => j < text.Length - 1 ? text[j + 1] : SubstituteChar)) {
                    i += length - 1;
                    continue;
                }
                sb.Append(ch);
            }
            return sb.ToString();
        }
    }
}

