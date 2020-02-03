// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Text;

public enum UnicodeNewline
{
    Unknown,

    /// <summary>
    /// Line Feed, U+000A
    /// </summary>
    LF = 0xA,
    CRLF = 0xD0A,

    /// <summary>
    /// Carriage Return, U+000D
    /// </summary>
    CR = 0xD,

    /// <summary>
    /// Next Line, U+0085
    /// </summary>
    NEL = 0x85,

    /// <summary>
    /// Vertical Tab, U+000B
    /// </summary>
    VT = 0xB,

    /// <summary>
    /// Form Feed, U+000C
    /// </summary>
    FF = 0xC,

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

/// Defines Unicode new lines according to Unicode Technical Report #13

/// http://www.unicode.org/standard/reports/tr13/tr13-5.html

/// </summary>

public static class NewLine
{

    /// <summary>
    /// Carriage Return, U+000D
    /// </summary>
    public const char CR = (char)0xD;

    /// <summary>
    /// Line Feed, U+000A
    /// </summary>
    public const char LF = (char)0xA;

    /// <summary>
    /// Next Line, U+0085
    /// </summary>
    public const char NEL = (char)0x85;

    /// <summary>
    /// Vertical Tab, U+000B
    /// </summary>
    public const char VT = (char)0xB;

    /// <summary>
    /// Form Feed, U+000C
    /// </summary>
    public const char FF = (char)0xC;

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
    /// <param name="nextChar">A callback getting the next character (may be null).</param>
    public static int GetDelimiterLength(char curChar, Func<char> nextChar = null)
    {
        if (curChar == CR)
        {
            if (nextChar != null && nextChar() == LF)
            {
                return 2;
            }
            return 1;
        }

        if (curChar == LF || curChar == NEL || curChar == VT || curChar == FF || curChar == LS || curChar == PS)
        {
            return 1;
        }
        return 0;
    }

    /// <summary>
    /// Determines if a char is a new line delimiter.
    /// </summary>
    /// <returns>0 == no new line, otherwise it returns either 1 or 2 depending of the length of the delimiter.</returns>
    /// <param name="curChar">The current character.</param>
    /// <param name="nextChar">The next character (if != LF then length will always be 0 or 1).</param>
    public static int GetDelimiterLength(char curChar, char nextChar)
    {
        if (curChar == CR)
        {
            if (nextChar == LF)
            {
                return 2;
            }
            return 1;
        }

        if (curChar == LF || curChar == NEL || curChar == VT || curChar == FF || curChar == LS || curChar == PS)
        {
            return 1;
        }
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
        if (curChar == CR)
        {
            if (nextChar != null && nextChar() == LF)
            {
                length = 2;
                type = UnicodeNewline.CRLF;
            }
            else
            {
                length = 1;
                type = UnicodeNewline.CR;
            }
            return true;
        }

        switch (curChar)
        {
            case LF:
                {
                    type = UnicodeNewline.LF;
                    length = 1;
                    return true;
                }

            case NEL:
                {
                    type = UnicodeNewline.NEL;
                    length = 1;
                    return true;
                }

            case VT:
                {
                    type = UnicodeNewline.VT;
                    length = 1;
                    return true;
                }

            case FF:
                {
                    type = UnicodeNewline.FF;
                    length = 1;
                    return true;
                }

            case LS:
                {
                    type = UnicodeNewline.LS;
                    length = 1;
                    return true;
                }

            case PS:
                {
                    type = UnicodeNewline.PS;
                    length = 1;
                    return true;
                }
        }
        length = -1;
        type = UnicodeNewline.Unknown;
        return false;
    }

    /// <summary>
    /// Determines if a char is a new line delimiter.
    /// </summary>
    /// <returns>0 == no new line, otherwise it returns either 1 or 2 depending of the length of the delimiter.</returns>
    /// <param name="curChar">The current character.</param>
    /// <param name = "length">The length of the delimiter</param>
    /// <param name = "type">The type of the delimiter</param>
    /// <param name="nextChar">The next character (if != LF then length will always be 0 or 1).</param>
    public static bool TryGetDelimiterLengthAndType(char curChar, out int length, out UnicodeNewline type, char nextChar)
    {
        if (curChar == CR)
        {
            if (nextChar == LF)
            {
                length = 2;
                type = UnicodeNewline.CRLF;
            }
            else
            {
                length = 1;
                type = UnicodeNewline.CR;
            }
            return true;
        }

        switch (curChar)
        {
            case LF:
                {
                    type = UnicodeNewline.LF;
                    length = 1;
                    return true;
                }

            case NEL:
                {
                    type = UnicodeNewline.NEL;
                    length = 1;
                    return true;
                }

            case VT:
                {
                    type = UnicodeNewline.VT;
                    length = 1;
                    return true;
                }

            case FF:
                {
                    type = UnicodeNewline.FF;
                    length = 1;
                    return true;
                }

            case LS:
                {
                    type = UnicodeNewline.LS;
                    length = 1;
                    return true;
                }

            case PS:
                {
                    type = UnicodeNewline.PS;
                    length = 1;
                    return true;
                }
        }
        length = -1;
        type = UnicodeNewline.Unknown;
        return false;
    }

    /// <summary>
    /// Gets the new line type of a given char/next char.
    /// </summary>
    /// <returns>0 == no new line, otherwise it returns either 1 or 2 depending of the length of the delimiter.</returns>
    /// <param name="curChar">The current character.</param>
    /// <param name="nextChar">A callback getting the next character (may be null).</param>
    public static UnicodeNewline GetDelimiterType(char curChar, Func<char> nextChar = null)
    {
        switch (curChar)
        {
            case CR:
                {
                    if (nextChar != null && nextChar() == LF)
                    {
                        return UnicodeNewline.CRLF;
                    }
                    return UnicodeNewline.CR;
                }

            case LF:
                {
                    return UnicodeNewline.LF;
                }

            case NEL:
                {
                    return UnicodeNewline.NEL;
                }

            case VT:
                {
                    return UnicodeNewline.VT;
                }

            case FF:
                {
                    return UnicodeNewline.FF;
                }

            case LS:
                {
                    return UnicodeNewline.LS;
                }

            case PS:
                {
                    return UnicodeNewline.PS;
                }
        }
        return UnicodeNewline.Unknown;
    }

    /// <summary>
    /// Gets the new line type of a given char/next char.
    /// </summary>
    /// <returns>0 == no new line, otherwise it returns either 1 or 2 depending of the length of the delimiter.</returns>
    /// <param name="curChar">The current character.</param>
    /// <param name="nextChar">The next character (if != LF then length will always be 0 or 1).</param>
    public static UnicodeNewline GetDelimiterType(char curChar, char nextChar)
    {
        switch (curChar)
        {
            case CR:
                {
                    if (nextChar == LF)
                    {
                        return UnicodeNewline.CRLF;
                    }
                    return UnicodeNewline.CR;
                }

            case LF:
                {
                    return UnicodeNewline.LF;
                }

            case NEL:
                {
                    return UnicodeNewline.NEL;
                }

            case VT:
                {
                    return UnicodeNewline.VT;
                }

            case FF:
                {
                    return UnicodeNewline.FF;
                }

            case LS:
                {
                    return UnicodeNewline.LS;
                }

            case PS:
                {
                    return UnicodeNewline.PS;
                }
        }
        return UnicodeNewline.Unknown;
    }

    /// <summary>
    /// Determines if a char is a new line delimiter.
    /// 
    /// Note that the only 2 char wide new line is CR LF and both chars are new line
    /// chars on their own. For most cases <see cref="GetDelimiterLength"/> is the better choice.
    /// </summary>
    public static bool IsNewLine(this char ch)
    {
        return ch == CR || ch == LF || ch == NEL || ch == VT || ch == FF || ch == LS || ch == PS;
    }

    /// <summary>
    /// Determines if a string is a new line delimiter.
    /// 
    /// Note that the only 2 char wide new line is CR LF
    /// </summary>
    public static bool IsNewLine(this string str)
    {
        if (str == null || str.Length == 0)
        {
            return false;
        }
        char ch = str[0];
        var switchExpr = str.Length;
        switch (switchExpr)
        {
            case 0:
                {
                    return false;
                }

            case 1:
            case 2:
                {
                    return ch == CR || ch == LF || ch == NEL || ch == VT || ch == FF || ch == LS || ch == PS;
                }

            default:
                {
                    return false;
                }
        }
    }

    /// <summary>
    /// Gets the new line as a string.
    /// </summary>
    public static string GetString(UnicodeNewline newLine)
    {
        switch (newLine)
        {
            case UnicodeNewline.Unknown:
                {
                    return "";
                }

            case UnicodeNewline.LF:
                {
                    return Constants.vbLf;
                }

            case UnicodeNewline.CRLF:
                {
                    return Constants.vbCrLf;
                }

            case UnicodeNewline.CR:
                {
                    return Constants.vbCr;
                }

            case UnicodeNewline.NEL:
                {
                    return (char)0x85.ToString(System.Globalization.CultureInfo.CurrentCulture);
                }

            case UnicodeNewline.VT:
                {
                    return Constants.vbVerticalTab;
                }

            case UnicodeNewline.FF:
                {
                    return Constants.vbFormFeed;
                }

            case UnicodeNewline.LS:
                {
                    return (char)0x2028.ToString(System.Globalization.CultureInfo.CurrentCulture);
                }

            case UnicodeNewline.PS:
                {
                    return (char)0x2029.ToString(System.Globalization.CultureInfo.CurrentCulture);
                }

            default:
                {
                    throw new ArgumentOutOfRangeException(nameof(newLine));
                    break;
                }
        }
    }

    public static string[] SplitLines(this string text)
    {
        var result = new List<string>();
        if (text == null)
        {
            return result.ToArray();
        }
        var sb = new StringBuilder();

        int length = default(int);
        UnicodeNewline type = default(UnicodeNewline);

        for (int i = 0, loopTo = text.Length - 1; i <= loopTo; i++)
        {
            char ch = text[i];
            // Do not delete the next line
            int j = i;
            if (TryGetDelimiterLengthAndType(ch, out length, out type, () => j < text.Length - 1 ? text[j + 1] : ControlChars.NullChar))
            {
                result.Add(sb.ToString());
                sb.Length = 0;
                i += length - 1;
                continue;
            }
            sb.Append(ch);
        }
        if (sb.Length > 0)
        {
            result.Add(sb.ToString());
        }

        return result.ToArray();
    }

    public static string JoinLines(this string[] Lines, string Delimiter)
    {
        return string.Join(separator: Delimiter, values: Lines);
    }

    internal static string NormalizeLineEndings(this string Lines, string Delimiter = Constants.vbCrLf)
    {
        return Lines.SplitLines().JoinLines(Delimiter);
    }

    /// <summary>
    /// Replace Unicode NewLines with ControlChars.NullChar or Specified Character
    /// </summary>
    /// <param name="text">Source Test</param>
    /// <param name="SubstituteChar">Default is vbNullChar</param>
    /// <returns>String with Unicode NewLines replaced with SubstituteChar</returns>
    public static string WithoutNewLines(this string text, char SubstituteChar = ControlChars.NullChar)
    {
        System.Diagnostics.Contracts.Contract.Requires(text != null);
        var sb = new StringBuilder();
        int length = default(int);
        UnicodeNewline type = default(UnicodeNewline);

        for (int i = 0, loopTo = text.Length - 1; i <= loopTo; i++)
        {
            char ch = text[i];
            // Do not delete the next line
            int j = i;
            if (TryGetDelimiterLengthAndType(ch, out length, out type, () => j < text.Length - 1 ? text[j + 1] : SubstituteChar))
            {
                i += length - 1;
                continue;
            }
            sb.Append(ch);
        }
        return sb.ToString();
    }
}

