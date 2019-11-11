// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualBasic.CompilerServices;

namespace Microsoft.VisualBasic
{
    [Embedded()]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Module | AttributeTargets.Assembly, Inherited = false)]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.CompilerServices.CompilerGenerated()]
    internal sealed class Embedded : Attribute
    {
    }
}

namespace Microsoft.VisualBasic
{
    namespace CompilerServices
    {
        [Embedded()]
        [DebuggerNonUserCode()]
        [System.Runtime.CompilerServices.CompilerGenerated()]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        internal sealed class EmbeddedOperators
        {
            private EmbeddedOperators()
            {
            }
            public static int CompareString(string Left, string Right, bool TextCompare)
            {
                if (Left == Right)
                    return 0;
                if (Left == null)
                {
                    if (Right.Length() == 0)
                        return 0;
                    return -1;
                }
                if (Right == null)
                {
                    if (Left.Length() == 0)
                        return 0;
                    return 1;
                }
                var Result = default(var);
                if (TextCompare)
                {
                    var OptionCompareTextFlags = System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreWidth | System.Globalization.CompareOptions.IgnoreKanaType;
                    Result = Conversions.GetCultureInfo().CompareInfo.Compare(Left, Right, OptionCompareTextFlags);
                }
                else
                    Result = string.CompareOrdinal(Left, Right);
                if (Result == 0)
                    return 0;
                else if (Result > 0)
                    return 1;
                else
                    return -1;
            }
        }
        [Embedded()]
        [DebuggerNonUserCode()]
        [System.Runtime.CompilerServices.CompilerGenerated()]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        internal sealed class Conversions
        {
            private Conversions()
            {
            }
            private static object GetEnumValue(object Value)
            {
                var underlyingType = Enum.GetUnderlyingType(Value.GetType());
                if (underlyingType.Equals(typeof(sbyte)))
                    return (sbyte)Value;
                else if (underlyingType.Equals(typeof(byte)))
                    return (byte)Value;
                else if (underlyingType.Equals(typeof(short)))
                    return (short)Value;
                else if (underlyingType.Equals(typeof(ushort)))
                    return (ushort)Value;
                else if (underlyingType.Equals(typeof(int)))
                    return (int)Value;
                else if (underlyingType.Equals(typeof(uint)))
                    return (uint)Value;
                else if (underlyingType.Equals(typeof(long)))
                    return (long)Value;
                else if (underlyingType.Equals(typeof(ulong)))
                    return (ulong)Value;
                else
                    throw new InvalidCastException();
            }
            public static bool ToBoolean(string Value)
            {
                if (Value == null)
                    Value = "";
                try
                {
                    var loc = GetCultureInfo();
                    if (loc.CompareInfo.Compare(Value, bool.FalseString, System.Globalization.CompareOptions.IgnoreCase) == 0)
                        return false;
                    else if (loc.CompareInfo.Compare(Value, bool.TrueString, System.Globalization.CompareOptions.IgnoreCase) == 0)
                        return true;
                    var i64Value = default(var);
                    if (IsHexOrOctValue(Value, i64Value))
                        return (bool)i64Value;
                    return (bool)ParseDouble(Value);
                }
                catch (FormatException e)
                {
                    throw new InvalidCastException(e.Message, e);
                }
            }
            public static bool ToBoolean(object Value)
            {
                if (Value == null)
                    return false;
                if (Value is Enum)
                    Value = GetEnumValue(Value);
                if (Value is bool)
                    return (bool)Value;
                else if (Value is sbyte)
                    return (bool)(sbyte)Value;
                else if (Value is byte)
                    return (bool)(byte)Value;
                else if (Value is short)
                    return (bool)(short)Value;
                else if (Value is ushort)
                    return (bool)(ushort)Value;
                else if (Value is int)
                    return (bool)(int)Value;
                else if (Value is uint)
                    return (bool)(uint)Value;
                else if (Value is long)
                    return (bool)(long)Value;
                else if (Value is ulong)
                    return (bool)(ulong)Value;
                else if (Value is decimal)
                    return (bool)(decimal)Value;
                else if (Value is float)
                    return (bool)(float)Value;
                else if (Value is double)
                    return (bool)(double)Value;
                else if (Value is string)
                    return (bool)(string)Value;
                throw new InvalidCastException();
            }
            public static byte ToByte(string Value)
            {
                if (Value == null)
                    return 0;
                try
                {
                    var i64Value = default(var);
                    if (IsHexOrOctValue(Value, i64Value))
                        return (byte)i64Value;
                    return (byte)ParseDouble(Value);
                }
                catch (FormatException e)
                {
                    throw new InvalidCastException(e.Message, e);
                }
            }
            public static byte ToByte(object Value)
            {
                if (Value == null)
                    return 0;
                if (Value is Enum)
                    Value = GetEnumValue(Value);
                if (Value is bool)
                    return (byte)(bool)Value;
                else if (Value is sbyte)
                    return (byte)(sbyte)Value;
                else if (Value is byte)
                    return (byte)Value;
                else if (Value is short)
                    return (byte)(short)Value;
                else if (Value is ushort)
                    return (byte)(ushort)Value;
                else if (Value is int)
                    return (byte)(int)Value;
                else if (Value is uint)
                    return (byte)(uint)Value;
                else if (Value is long)
                    return (byte)(long)Value;
                else if (Value is ulong)
                    return (byte)(ulong)Value;
                else if (Value is decimal)
                    return (byte)(decimal)Value;
                else if (Value is float)
                    return (byte)(float)Value;
                else if (Value is double)
                    return (byte)(double)Value;
                else if (Value is string)
                    return (byte)(string)Value;
                throw new InvalidCastException();
            }
            [CLSCompliant(false)]
            public static sbyte ToSByte(string Value)
            {
                if (Value == null)
                    return 0;
                try
                {
                    var i64Value = default(var);
                    if (IsHexOrOctValue(Value, i64Value))
                        return (sbyte)i64Value;
                    return (sbyte)ParseDouble(Value);
                }
                catch (FormatException e)
                {
                    throw new InvalidCastException(e.Message, e);
                }
            }
            [CLSCompliant(false)]
            public static sbyte ToSByte(object Value)
            {
                if (Value == null)
                    return 0;
                if (Value is Enum)
                    Value = GetEnumValue(Value);
                if (Value is bool)
                    return (sbyte)(bool)Value;
                else if (Value is sbyte)
                    return (sbyte)Value;
                else if (Value is byte)
                    return (sbyte)(byte)Value;
                else if (Value is short)
                    return (sbyte)(short)Value;
                else if (Value is ushort)
                    return (sbyte)(ushort)Value;
                else if (Value is int)
                    return (sbyte)(int)Value;
                else if (Value is uint)
                    return (sbyte)(uint)Value;
                else if (Value is long)
                    return (sbyte)(long)Value;
                else if (Value is ulong)
                    return (sbyte)(ulong)Value;
                else if (Value is decimal)
                    return (sbyte)(decimal)Value;
                else if (Value is float)
                    return (sbyte)(float)Value;
                else if (Value is double)
                    return (sbyte)(double)Value;
                else if (Value is string)
                    return (sbyte)(string)Value;
                throw new InvalidCastException();
            }
            public static short ToShort(string Value)
            {
                if (Value == null)
                    return 0;
                try
                {
                    var i64Value = default(var);
                    if (IsHexOrOctValue(Value, i64Value))
                        return (short)i64Value;
                    return (short)ParseDouble(Value);
                }
                catch (FormatException e)
                {
                    throw new InvalidCastException(e.Message, e);
                }
            }
            public static short ToShort(object Value)
            {
                if (Value == null)
                    return 0;
                if (Value is Enum)
                    Value = GetEnumValue(Value);
                if (Value is bool)
                    return (short)(bool)Value;
                else if (Value is sbyte)
                    return (sbyte)Value;
                else if (Value is byte)
                    return (byte)Value;
                else if (Value is short)
                    return (short)Value;
                else if (Value is ushort)
                    return (short)(ushort)Value;
                else if (Value is int)
                    return (short)(int)Value;
                else if (Value is uint)
                    return (short)(uint)Value;
                else if (Value is long)
                    return (short)(long)Value;
                else if (Value is ulong)
                    return (short)(ulong)Value;
                else if (Value is decimal)
                    return (short)(decimal)Value;
                else if (Value is float)
                    return (short)(float)Value;
                else if (Value is double)
                    return (short)(double)Value;
                else if (Value is string)
                    return (short)(string)Value;
                throw new InvalidCastException();
            }
            [CLSCompliant(false)]
            public static ushort ToUShort(string Value)
            {
                if (Value == null)
                    return 0;
                try
                {
                    var i64Value = default(var);
                    if (IsHexOrOctValue(Value, i64Value))
                        return (ushort)i64Value;
                    return (ushort)ParseDouble(Value);
                }
                catch (FormatException e)
                {
                    throw new InvalidCastException(e.Message, e);
                }
            }
            [CLSCompliant(false)]
            public static ushort ToUShort(object Value)
            {
                if (Value == null)
                    return 0;
                if (Value is Enum)
                    Value = GetEnumValue(Value);
                if (Value is bool)
                    return (ushort)(bool)Value;
                else if (Value is sbyte)
                    return (ushort)(sbyte)Value;
                else if (Value is byte)
                    return (byte)Value;
                else if (Value is short)
                    return (ushort)(short)Value;
                else if (Value is ushort)
                    return (ushort)Value;
                else if (Value is int)
                    return (ushort)(int)Value;
                else if (Value is uint)
                    return (ushort)(uint)Value;
                else if (Value is long)
                    return (ushort)(long)Value;
                else if (Value is ulong)
                    return (ushort)(ulong)Value;
                else if (Value is decimal)
                    return (ushort)(decimal)Value;
                else if (Value is float)
                    return (ushort)(float)Value;
                else if (Value is double)
                    return (ushort)(double)Value;
                else if (Value is string)
                    return (ushort)(string)Value;
                throw new InvalidCastException();
            }
            public static int ToInteger(string Value)
            {
                if (Value == null)
                    return 0;
                try
                {
                    var i64Value = default(var);
                    if (IsHexOrOctValue(Value, i64Value))
                        return (int)i64Value;
                    return (int)ParseDouble(Value);
                }
                catch (FormatException e)
                {
                    throw new InvalidCastException(e.Message, e);
                }
            }
            public static int ToInteger(object Value)
            {
                if (Value == null)
                    return 0;
                if (Value is Enum)
                    Value = GetEnumValue(Value);
                if (Value is bool)
                    return (int)(bool)Value;
                else if (Value is sbyte)
                    return (sbyte)Value;
                else if (Value is byte)
                    return (byte)Value;
                else if (Value is short)
                    return (short)Value;
                else if (Value is ushort)
                    return (ushort)Value;
                else if (Value is int)
                    return (int)Value;
                else if (Value is uint)
                    return (int)(uint)Value;
                else if (Value is long)
                    return (int)(long)Value;
                else if (Value is ulong)
                    return (int)(ulong)Value;
                else if (Value is decimal)
                    return (int)(decimal)Value;
                else if (Value is float)
                    return (int)(float)Value;
                else if (Value is double)
                    return (int)(double)Value;
                else if (Value is string)
                    return (int)(string)Value;
                throw new InvalidCastException();
            }
            [CLSCompliant(false)]
            public static uint ToUInteger(string Value)
            {
                if (Value == null)
                    return 0;
                try
                {
                    var i64Value = default(var);
                    if (IsHexOrOctValue(Value, i64Value))
                        return (uint)i64Value;
                    return (uint)ParseDouble(Value);
                }
                catch (FormatException e)
                {
                    throw new InvalidCastException(e.Message, e);
                }
            }
            [CLSCompliant(false)]
            public static uint ToUInteger(object Value)
            {
                if (Value == null)
                    return 0;
                if (Value is Enum)
                    Value = GetEnumValue(Value);
                if (Value is bool)
                    return (uint)(bool)Value;
                else if (Value is sbyte)
                    return (uint)(sbyte)Value;
                else if (Value is byte)
                    return (byte)Value;
                else if (Value is short)
                    return (uint)(short)Value;
                else if (Value is ushort)
                    return (ushort)Value;
                else if (Value is int)
                    return (uint)(int)Value;
                else if (Value is uint)
                    return (uint)Value;
                else if (Value is long)
                    return (uint)(long)Value;
                else if (Value is ulong)
                    return (uint)(ulong)Value;
                else if (Value is decimal)
                    return (uint)(decimal)Value;
                else if (Value is float)
                    return (uint)(float)Value;
                else if (Value is double)
                    return (uint)(double)Value;
                else if (Value is string)
                    return (uint)(string)Value;
                throw new InvalidCastException();
            }
            public static long ToLong(string Value)
            {
                if (Value == null)
                    return 0;
                try
                {
                    var i64Value = default(var);
                    if (IsHexOrOctValue(Value, i64Value))
                        return (long)i64Value;
                    return (long)ParseDecimal(Value, null);
                }
                catch (FormatException e)
                {
                    throw new InvalidCastException(e.Message, e);
                }
            }
            public static long ToLong(object Value)
            {
                if (Value == null)
                    return 0;
                if (Value is Enum)
                    Value = GetEnumValue(Value);
                if (Value is bool)
                    return (long)(bool)Value;
                else if (Value is sbyte)
                    return (sbyte)Value;
                else if (Value is byte)
                    return (byte)Value;
                else if (Value is short)
                    return (short)Value;
                else if (Value is ushort)
                    return (ushort)Value;
                else if (Value is int)
                    return (int)Value;
                else if (Value is uint)
                    return (uint)Value;
                else if (Value is long)
                    return (long)Value;
                else if (Value is ulong)
                    return (long)(ulong)Value;
                else if (Value is decimal)
                    return (long)(decimal)Value;
                else if (Value is float)
                    return (long)(float)Value;
                else if (Value is double)
                    return (long)(double)Value;
                else if (Value is string)
                    return (long)(string)Value;
                throw new InvalidCastException();
            }
            [CLSCompliant(false)]
            public static ulong ToULong(string Value)
            {
                if (Value == null)
                    return 0;
                try
                {
                    var ui64Value = default(var);
                    if (IsHexOrOctValue(Value, ui64Value))
                        return (ulong)ui64Value;
                    return (ulong)ParseDecimal(Value, null);
                }
                catch (FormatException e)
                {
                    throw new InvalidCastException(e.Message, e);
                }
            }
            [CLSCompliant(false)]
            public static ulong ToULong(object Value)
            {
                if (Value == null)
                    return 0;
                if (Value is Enum)
                    Value = GetEnumValue(Value);
                if (Value is bool)
                    return (ulong)(bool)Value;
                else if (Value is sbyte)
                    return (ulong)(sbyte)Value;
                else if (Value is byte)
                    return (byte)Value;
                else if (Value is short)
                    return (ulong)(short)Value;
                else if (Value is ushort)
                    return (ushort)Value;
                else if (Value is int)
                    return (ulong)(int)Value;
                else if (Value is uint)
                    return (uint)Value;
                else if (Value is long)
                    return (ulong)(long)Value;
                else if (Value is ulong)
                    return (ulong)Value;
                else if (Value is decimal)
                    return (ulong)(decimal)Value;
                else if (Value is float)
                    return (ulong)(float)Value;
                else if (Value is double)
                    return (ulong)(double)Value;
                else if (Value is string)
                    return (ulong)(string)Value;
                throw new InvalidCastException();
            }
            public static decimal ToDecimal(bool Value)
            {
                if (Value)
                    return -1M;
                else
                    return 0M;
            }
            public static decimal ToDecimal(string Value)
            {
                if (Value == null)
                    return 0M;
                try
                {
                    var i64Value = default(var);
                    if (IsHexOrOctValue(Value, i64Value))
                        return (decimal)i64Value;
                    return ParseDecimal(Value, null);
                }
                catch (OverflowException e1)
                {
                    throw e1;
                }
                catch (FormatException e2)
                {
                    throw new InvalidCastException(e2.Message, e2);
                }
            }
            public static decimal ToDecimal(object Value)
            {
                if (Value == null)
                    return 0M;
                if (Value is Enum)
                    Value = GetEnumValue(Value);
                if (Value is bool)
                    return (decimal)(bool)Value;
                else if (Value is sbyte)
                    return (sbyte)Value;
                else if (Value is byte)
                    return (byte)Value;
                else if (Value is short)
                    return (short)Value;
                else if (Value is ushort)
                    return (ushort)Value;
                else if (Value is int)
                    return (int)Value;
                else if (Value is uint)
                    return (uint)Value;
                else if (Value is long)
                    return (long)Value;
                else if (Value is ulong)
                    return (ulong)Value;
                else if (Value is decimal)
                    return (decimal)Value;
                else if (Value is float)
                    return (decimal)(float)Value;
                else if (Value is double)
                    return (decimal)(double)Value;
                else if (Value is string)
                    return (decimal)(string)Value;
                throw new InvalidCastException();
            }
            private static decimal ParseDecimal(string Value, System.Globalization.NumberFormatInfo NumberFormat)
            {
                var NormalizedNumberFormat = default(var);
                var culture = GetCultureInfo();
                if (NumberFormat == null)
                    NumberFormat = culture.NumberFormat;
                NormalizedNumberFormat = GetNormalizedNumberFormat(NumberFormat);
                const var flags = System.Globalization.NumberStyles.AllowDecimalPoint | System.Globalization.NumberStyles.AllowExponent | System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowLeadingWhite | System.Globalization.NumberStyles.AllowThousands | System.Globalization.NumberStyles.AllowTrailingSign | System.Globalization.NumberStyles.AllowParentheses | System.Globalization.NumberStyles.AllowTrailingWhite | System.Globalization.NumberStyles.AllowCurrencySymbol;
                Value = ToHalfwidthNumbers(Value, culture);
                try
                {
                    return decimal.Parse(Value, flags, NormalizedNumberFormat);
                }
                catch (FormatException FormatEx) when (!(NumberFormat == NormalizedNumberFormat))
                {
                    return decimal.Parse(Value, flags, NumberFormat);
                }
                catch (Exception Ex)
                {
                    throw Ex;
                }
            }
            private static System.Globalization.NumberFormatInfo GetNormalizedNumberFormat(System.Globalization.NumberFormatInfo InNumberFormat)
            {
                var OutNumberFormat = default(var);
                {
                    var withBlock = InNumberFormat;
                    if (!(withBlock.CurrencyDecimalSeparator == null) && !(withBlock.NumberDecimalSeparator == null) && !(withBlock.CurrencyGroupSeparator == null) && !(withBlock.NumberGroupSeparator == null) && withBlock.CurrencyDecimalSeparator.Length == 1 && withBlock.NumberDecimalSeparator.Length == 1 && withBlock.CurrencyGroupSeparator.Length == 1 && withBlock.NumberGroupSeparator.Length == 1 && withBlock.CurrencyDecimalSeparator.Chars(0) == withBlock.NumberDecimalSeparator.Chars(0) && withBlock.CurrencyGroupSeparator.Chars(0) == withBlock.NumberGroupSeparator.Chars(0) && withBlock.CurrencyDecimalDigits == withBlock.NumberDecimalDigits)
                        return InNumberFormat;
                }
                {
                    var withBlock1 = InNumberFormat;
                    if (!(withBlock1.CurrencyDecimalSeparator == null) && !(withBlock1.NumberDecimalSeparator == null) && withBlock1.CurrencyDecimalSeparator.Length == withBlock1.NumberDecimalSeparator.Length && !(withBlock1.CurrencyGroupSeparator == null) && !(withBlock1.NumberGroupSeparator == null) && withBlock1.CurrencyGroupSeparator.Length == withBlock1.NumberGroupSeparator.Length)
                    {
                        var i = default(var);
                        var loopTo = withBlock1.CurrencyDecimalSeparator.Length - 1;
                        for (i = 0; i <= loopTo; i++)
                        {
                            if (withBlock1.CurrencyDecimalSeparator.Chars(i) != withBlock1.NumberDecimalSeparator.Chars(i))
                                goto MisMatch;
                        }

                        var loopTo1 = withBlock1.CurrencyGroupSeparator.Length - 1;
                        for (i = 0; i <= loopTo1; i++)
                        {
                            if (withBlock1.CurrencyGroupSeparator.Chars(i) != withBlock1.NumberGroupSeparator.Chars(i))
                                goto MisMatch;
                        }
                        return InNumberFormat;
                    }
                }

            MisMatch:
                ;
                OutNumberFormat = (System.Globalization.NumberFormatInfo)InNumberFormat.Clone;
                {
                    var withBlock2 = OutNumberFormat;
                    withBlock2.CurrencyDecimalSeparator = withBlock2.NumberDecimalSeparator;
                    withBlock2.CurrencyGroupSeparator = withBlock2.NumberGroupSeparator;
                    withBlock2.CurrencyDecimalDigits = withBlock2.NumberDecimalDigits;
                }
                return OutNumberFormat;
            }
            public static float ToSingle(string Value)
            {
                if (Value == null)
                    return 0;
                try
                {
                    var i64Value = default(var);
                    if (IsHexOrOctValue(Value, i64Value))
                        return (float)i64Value;
                    var Result = ParseDouble(Value);
                    if ((Result < float.MinValue || Result > float.MaxValue) && !double.IsInfinity(Result))
                        throw new OverflowException();
                    return (float)Result;
                }
                catch (FormatException e)
                {
                    throw new InvalidCastException(e.Message, e);
                }
            }
            public static float ToSingle(object Value)
            {
                if (Value == null)
                    return 0;
                if (Value is Enum)
                    Value = GetEnumValue(Value);
                if (Value is bool)
                    return (float)(bool)Value;
                else if (Value is sbyte)
                    return (sbyte)Value;
                else if (Value is byte)
                    return (byte)Value;
                else if (Value is short)
                    return (short)Value;
                else if (Value is ushort)
                    return (ushort)Value;
                else if (Value is int)
                    return (int)Value;
                else if (Value is uint)
                    return (uint)Value;
                else if (Value is long)
                    return (long)Value;
                else if (Value is ulong)
                    return (ulong)Value;
                else if (Value is decimal)
                    return (float)(decimal)Value;
                else if (Value is float)
                    return (float)Value;
                else if (Value is double)
                    return (float)(double)Value;
                else if (Value is string)
                    return (float)(string)Value;
                throw new InvalidCastException();
            }
            public static double ToDouble(string Value)
            {
                if (Value == null)
                    return 0;
                try
                {
                    var i64Value = default(var);
                    if (IsHexOrOctValue(Value, i64Value))
                        return (double)i64Value;
                    return ParseDouble(Value);
                }
                catch (FormatException e)
                {
                    throw new InvalidCastException(e.Message, e);
                }
            }
            public static double ToDouble(object Value)
            {
                if (Value == null)
                    return 0;
                if (Value is Enum)
                    Value = GetEnumValue(Value);
                if (Value is bool)
                    return (double)(bool)Value;
                else if (Value is sbyte)
                    return (sbyte)Value;
                else if (Value is byte)
                    return (byte)Value;
                else if (Value is short)
                    return (short)Value;
                else if (Value is ushort)
                    return (ushort)Value;
                else if (Value is int)
                    return (int)Value;
                else if (Value is uint)
                    return (uint)Value;
                else if (Value is long)
                    return (long)Value;
                else if (Value is ulong)
                    return (ulong)Value;
                else if (Value is decimal)
                    return (double)(decimal)Value;
                else if (Value is float)
                    return (float)Value;
                else if (Value is double)
                    return (double)Value;
                else if (Value is string)
                    return (double)(string)Value;
                throw new InvalidCastException();
            }
            private static double ParseDouble(string Value)
            {
                var NormalizedNumberFormat = default(var);
                var culture = GetCultureInfo();
                var NumberFormat = culture.NumberFormat;
                NormalizedNumberFormat = GetNormalizedNumberFormat(NumberFormat);
                const var flags = System.Globalization.NumberStyles.AllowDecimalPoint | System.Globalization.NumberStyles.AllowExponent | System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowLeadingWhite | System.Globalization.NumberStyles.AllowThousands | System.Globalization.NumberStyles.AllowTrailingSign | System.Globalization.NumberStyles.AllowParentheses | System.Globalization.NumberStyles.AllowTrailingWhite | System.Globalization.NumberStyles.AllowCurrencySymbol;
                Value = ToHalfwidthNumbers(Value, culture);
                try
                {
                    return double.Parse(Value, flags, NormalizedNumberFormat);
                }
                catch (FormatException FormatEx) when (!(NumberFormat == NormalizedNumberFormat))
                {
                    return double.Parse(Value, flags, NumberFormat);
                }
                catch (Exception Ex)
                {
                    throw Ex;
                }
            }
            public static DateTime ToDate(string Value)
            {
                var ParsedDate = default(var);
                const var ParseStyle = System.Globalization.DateTimeStyles.AllowWhiteSpaces | System.Globalization.DateTimeStyles.NoCurrentDateDefault;
                var Culture = GetCultureInfo();
                var result = DateTime.TryParse(ToHalfwidthNumbers(Value, Culture), Culture, ParseStyle, ParsedDate);
                if (result)
                    return ParsedDate;
                else
                    throw new InvalidCastException();
            }
            public static DateTime ToDate(object Value)
            {
                if (Value == null)
                    return null;
                if (Value is DateTime)
                    return ToDate((DateTime)Value);
                else if (Value is string)
                    return ToDate((string)Value);
                throw new InvalidCastException();
            }
            public static char ToChar(string Value)
            {
                if (Value == null || Value.Length == 0)
                    return Convert.ToChar(0 & 0xFFFF);
                return Value.Chars(0);
            }
            public static char ToChar(object Value)
            {
                if (Value == null)
                    return Convert.ToChar(0 & 0xFFFF);
                if (Value is char)
                    return (char)Value;
                else if (Value is string)
                    return (char)(string)Value;
                throw new InvalidCastException();
            }
            public static char[] ToCharArrayRankOne(string Value)
            {
                if (Value == null)
                    Value = "";
                return Value.ToCharArray();
            }
            public static char[] ToCharArrayRankOne(object Value)
            {
                if (Value == null)
                    return "".ToCharArray();
                var ArrayValue = Value as char[];
                if (ArrayValue != null && ArrayValue.Rank == 1)
                    return ArrayValue;
                else if (Value is string)
                    return ((string)Value).ToCharArray();
                throw new InvalidCastException();
            }
            public static new string ToString(short Value)
            {
                return Value.ToString();
            }
            public static new string ToString(int Value)
            {
                return Value.ToString();
            }
            [CLSCompliant(false)]
            public static new string ToString(uint Value)
            {
                return Value.ToString();
            }
            public static new string ToString(long Value)
            {
                return Value.ToString();
            }
            [CLSCompliant(false)]
            public static new string ToString(ulong Value)
            {
                return Value.ToString();
            }
            public static new string ToString(float Value)
            {
                return Value.ToString();
            }
            public static new string ToString(double Value)
            {
                return Value.ToString("G");
            }
            public static new string ToString(DateTime Value)
            {
                var TimeTicks = Value.TimeOfDay.Ticks;
                if (TimeTicks == Value.Ticks || Value.Year == 1899 && Value.Month == 12 && Value.Day == 30)
                    return Value.ToString("T");
                else if (TimeTicks == 0)
                    return Value.ToString("d");
                else
                    return Value.ToString("G");
            }
            public static new string ToString(decimal Value)
            {
                return Value.ToString("G");
            }
            public static new string ToString(object Value)
            {
                if (Value == null)
                    return null;
                else
                {
                    var StringValue = Value as string;
                    if (StringValue != null)
                        return StringValue;
                }
                if (Value is Enum)
                    Value = GetEnumValue(Value);
                if (Value is bool)
                    return (string)(bool)Value;
                else if (Value is sbyte)
                    return (string)(sbyte)Value;
                else if (Value is byte)
                    return (string)(byte)Value;
                else if (Value is short)
                    return (string)(short)Value;
                else if (Value is ushort)
                    return (string)(ushort)Value;
                else if (Value is int)
                    return (string)(int)Value;
                else if (Value is uint)
                    return (string)(uint)Value;
                else if (Value is long)
                    return (string)(long)Value;
                else if (Value is ulong)
                    return (string)(ulong)Value;
                else if (Value is decimal)
                    return (string)(decimal)Value;
                else if (Value is float)
                    return (string)(float)Value;
                else if (Value is double)
                    return (string)(double)Value;
                else if (Value is char)
                    return (string)(char)Value;
                else if (Value is DateTime)
                    return (string)(DateTime)Value;
                else
                {
                    var CharArray = Value as char[];
                    if (CharArray != null)
                        return new string(CharArray);
                }
                throw new InvalidCastException();
            }
            public static new string ToString(bool Value)
            {
                if (Value)
                    return bool.TrueString;
                else
                    return bool.FalseString;
            }
            public static new string ToString(byte Value)
            {
                return Value.ToString();
            }
            public static new string ToString(char Value)
            {
                return Value.ToString();
            }
            internal static System.Globalization.CultureInfo GetCultureInfo()
            {
                return System.Globalization.CultureInfo.CurrentCulture;
            }
            internal static string ToHalfwidthNumbers(string s, System.Globalization.CultureInfo culture)
            {
                return s;
            }
            internal static bool IsHexOrOctValue(string Value, ref long i64Value)
            {
                var ch = default(var);
                var Length = default(var);
                var FirstNonspace = default(var);
                var TmpValue = default(var);
                Length = Value.Length;
                while (FirstNonspace < Length)
                {
                    ch = Value.Chars(FirstNonspace);
                    if (ch == '&' && FirstNonspace + 2 < Length)
                        goto GetSpecialValue;
                    if (ch != Strings.ChrW(32) && ch != Strings.ChrW(0x3000))
                        return false;
                    FirstNonspace += 1;
                }
                return false;
            GetSpecialValue:
                ;
                ch = char.ToLowerInvariant(Value.Chars(FirstNonspace + 1));
                TmpValue = ToHalfwidthNumbers(Value.Substring(FirstNonspace + 2), GetCultureInfo());
                if (ch == 'h')
                    i64Value = Convert.ToInt64(TmpValue, 16);
                else if (ch == 'o')
                    i64Value = Convert.ToInt64(TmpValue, 8);
                else
                    throw new FormatException();
                return true;
            }
            internal static bool IsHexOrOctValue(string Value, ref ulong ui64Value)
            {
                var ch = default(var);
                var Length = default(var);
                var FirstNonspace = default(var);
                var TmpValue = default(var);
                Length = Value.Length;
                while (FirstNonspace < Length)
                {
                    ch = Value.Chars(FirstNonspace);
                    if (ch == '&' && FirstNonspace + 2 < Length)
                        goto GetSpecialValue;
                    if (ch != Strings.ChrW(32) && ch != Strings.ChrW(0x3000))
                        return false;
                    FirstNonspace += 1;
                }
                return false;
            GetSpecialValue:
                ;
                ch = char.ToLowerInvariant(Value.Chars(FirstNonspace + 1));
                TmpValue = ToHalfwidthNumbers(Value.Substring(FirstNonspace + 2), GetCultureInfo());
                if (ch == 'h')
                    ui64Value = Convert.ToUInt64(TmpValue, 16);
                else if (ch == 'o')
                    ui64Value = Convert.ToUInt64(TmpValue, 8);
                else
                    throw new FormatException();
                return true;
            }
            public static T ToGenericParameter<T>(object Value)
            {
                if (Value == null)
                    return null;
                var reflectedType = typeof(T);
                if (Equals(reflectedType, typeof(bool)))
                    return (T)(object)(bool)Value;
                else if (Equals(reflectedType, typeof(sbyte)))
                    return (T)(object)(sbyte)Value;
                else if (Equals(reflectedType, typeof(byte)))
                    return (T)(object)(byte)Value;
                else if (Equals(reflectedType, typeof(short)))
                    return (T)(object)(short)Value;
                else if (Equals(reflectedType, typeof(ushort)))
                    return (T)(object)(ushort)Value;
                else if (Equals(reflectedType, typeof(int)))
                    return (T)(object)(int)Value;
                else if (Equals(reflectedType, typeof(uint)))
                    return (T)(object)(uint)Value;
                else if (Equals(reflectedType, typeof(long)))
                    return (T)(object)(long)Value;
                else if (Equals(reflectedType, typeof(ulong)))
                    return (T)(object)(ulong)Value;
                else if (Equals(reflectedType, typeof(decimal)))
                    return (T)(object)(decimal)Value;
                else if (Equals(reflectedType, typeof(float)))
                    return (T)(object)(float)Value;
                else if (Equals(reflectedType, typeof(double)))
                    return (T)(object)(double)Value;
                else if (Equals(reflectedType, typeof(DateTime)))
                    return (T)(object)ToDate(Value);
                else if (Equals(reflectedType, typeof(char)))
                    return (T)(object)(char)Value;
                else if (Equals(reflectedType, typeof(string)))
                    return (T)(object)(string)Value;
                else
                    return (T)Value;
            }
        }
        [Embedded()]
        [DebuggerNonUserCode()]
        [System.Runtime.CompilerServices.CompilerGenerated()]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        internal sealed class ProjectData
        {
            private ProjectData()
            {
            }
            public new static void SetProjectError(Exception ex)
            {
            }
            public new static void SetProjectError(Exception ex, int lErl)
            {
            }
            public static void ClearProjectError()
            {
            }
        }
        [Embedded()]
        [DebuggerNonUserCode()]
        [System.Runtime.CompilerServices.CompilerGenerated()]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        internal sealed class Utils
        {
            private Utils()
            {
            }
            public static Array CopyArray(Array arySrc, Array aryDest)
            {
                if (arySrc == null)
                    return aryDest;
                var lLength = default(var);
                lLength = arySrc.Length;
                if (lLength == 0)
                    return aryDest;
                if (aryDest.Rank() != arySrc.Rank())
                    throw new InvalidCastException();
                var iDim = default(var);
                var loopTo = aryDest.Rank() - 2;
                for (iDim = 0; iDim <= loopTo; iDim++)
                {
                    if (aryDest.GetUpperBound(iDim) != arySrc.GetUpperBound(iDim))
                        throw new ArrayTypeMismatchException();
                }
                if (lLength > aryDest.Length)
                    lLength = aryDest.Length;
                if (arySrc.Rank > 1)
                {
                    var LastRank = arySrc.Rank;
                    var lenSrcLastRank = arySrc.GetLength(LastRank - 1);
                    var lenDestLastRank = aryDest.GetLength(LastRank - 1);
                    if (lenDestLastRank == 0)
                        return aryDest;
                    var lenCopy = lenSrcLastRank > lenDestLastRank ? lenDestLastRank : lenSrcLastRank;
                    var i = default(var);
                    var loopTo1 = arySrc.Length / lenSrcLastRank - 1;
                    for (i = 0; i <= loopTo1; i++)
                        Array.Copy(arySrc, i * lenSrcLastRank, aryDest, i * lenDestLastRank, lenCopy);
                }
                else
                    Array.Copy(arySrc, aryDest, lLength);
                return aryDest;
            }
        }
        [Embedded()]
        [DebuggerNonUserCode()]
        [System.Runtime.CompilerServices.CompilerGenerated()]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        internal sealed class ObjectFlowControl
        {
            internal sealed class ForLoopControl
            {
                public static bool ForNextCheckR4(float count, float limit, float StepValue)
                {
                    if (StepValue >= 0)
                        return count <= limit;
                    else
                        return count >= limit;
                }
                public static bool ForNextCheckR8(double count, double limit, double StepValue)
                {
                    if (StepValue >= 0)
                        return count <= limit;
                    else
                        return count >= limit;
                }
                public static bool ForNextCheckDec(decimal count, decimal limit, decimal StepValue)
                {
                    if (StepValue >= 0)
                        return count <= limit;
                    else
                        return count >= limit;
                }
            }
        }
        [Embedded()]
        [DebuggerNonUserCode()]
        [System.Runtime.CompilerServices.CompilerGenerated()]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        internal sealed class StaticLocalInitFlag
        {
            public var State;
        }
        [Embedded()]
        [DebuggerNonUserCode()]
        [System.Runtime.CompilerServices.CompilerGenerated()]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        internal sealed class IncompleteInitialization : Exception
        {
            public IncompleteInitialization() : base()
            {
            }
        }
        [Embedded()]
        [AttributeUsage(AttributeTargets.Class, Inherited = false)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Runtime.CompilerServices.CompilerGenerated()]
        internal sealed class StandardModuleAttribute : Attribute
        {
        }
        [Embedded()]
        [AttributeUsage(AttributeTargets.Class, Inherited = false)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Runtime.CompilerServices.CompilerGenerated()]
        internal sealed class DesignerGeneratedAttribute : Attribute
        {
        }
        [Embedded()]
        [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Runtime.CompilerServices.CompilerGenerated()]
        internal sealed class OptionCompareAttribute : Attribute
        {
        }
        [Embedded()]
        [AttributeUsage(AttributeTargets.Class, Inherited = false)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Runtime.CompilerServices.CompilerGenerated()]
        internal sealed class OptionTextAttribute : Attribute
        {
        }
    }
    [Embedded()]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.CompilerServices.CompilerGenerated()]
    internal sealed class HideModuleNameAttribute : Attribute
    {
    }
    [Embedded()]
    [DebuggerNonUserCode()]
    [System.Runtime.CompilerServices.CompilerGenerated()]
    [StandardModule]
    internal static class Strings
    {
        public static char ChrW(int CharCode)
        {
            if (CharCode < -32768 || CharCode > 65535)
                throw new ArgumentException();
            return Convert.ToChar(CharCode & 0xFFFF);
        }
        public static int AscW(string String)
        {
            if (String == null || String.Length == 0)
                throw new ArgumentException();
            return AscW(String.Chars(0));
        }
        public static int AscW(char String)
        {
            return AscW(String);
        }
    }
    [Embedded()]
    [DebuggerNonUserCode()]
    [System.Runtime.CompilerServices.CompilerGenerated()]
    [StandardModule]
    internal static class Constants
    {
        class _failedMemberConversionMarker1
        {
        }
#error Cannot convert FieldDeclarationSyntax - see comment for details
        /* Cannot convert FieldDeclarationSyntax, System.NullReferenceException: Object reference not set to an instance of an object.
           at ICSharpCode.CodeConverter.CSharp.ExpressionNodeVisitor.<VisitBinaryExpression>d__62.MoveNext() in C:\Users\Graham\Documents\GitHub\CodeConverter\ICSharpCode.CodeConverter\CSharp\ExpressionNodeVisitor.cs:line 570
        --- End of stack trace from previous location where exception was thrown ---
           at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
           at System.Runtime.CompilerServices.TaskAwaiter`1.GetResult()
           at ICSharpCode.CodeConverter.CSharp.CommentConvertingVisitorWrapper`1.<Visit>d__5.MoveNext() in C:\Users\Graham\Documents\GitHub\CodeConverter\ICSharpCode.CodeConverter\CSharp\CommentConvertingVisitorWrapper.cs:line 22
        --- End of stack trace from previous location where exception was thrown ---
           at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
           at System.Runtime.CompilerServices.TaskAwaiter`1.GetResult()
           at ICSharpCode.CodeConverter.CSharp.SyntaxNodeVisitorExtensions.<AcceptAsync>d__0`1.MoveNext() in C:\Users\Graham\Documents\GitHub\CodeConverter\ICSharpCode.CodeConverter\CSharp\SyntaxNodeVisitorExtensions.cs:line 16
        --- End of stack trace from previous location where exception was thrown ---
           at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
           at System.Runtime.CompilerServices.TaskAwaiter`1.GetResult()
           at ICSharpCode.CodeConverter.CSharp.CommonConversions.<SplitVariableDeclarations>d__18.MoveNext() in C:\Users\Graham\Documents\GitHub\CodeConverter\ICSharpCode.CodeConverter\CSharp\CommonConversions.cs:line 63
        --- End of stack trace from previous location where exception was thrown ---
           at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
           at System.Runtime.CompilerServices.TaskAwaiter`1.GetResult()
           at ICSharpCode.CodeConverter.CSharp.DeclarationNodeVisitor.<GetMemberDeclarations>d__48.MoveNext() in C:\Users\Graham\Documents\GitHub\CodeConverter\ICSharpCode.CodeConverter\CSharp\DeclarationNodeVisitor.cs:line 412
        --- End of stack trace from previous location where exception was thrown ---
           at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
           at System.Runtime.CompilerServices.TaskAwaiter`1.GetResult()
           at ICSharpCode.CodeConverter.CSharp.DeclarationNodeVisitor.<VisitFieldDeclaration>d__47.MoveNext() in C:\Users\Graham\Documents\GitHub\CodeConverter\ICSharpCode.CodeConverter\CSharp\DeclarationNodeVisitor.cs:line 392
        --- End of stack trace from previous location where exception was thrown ---
           at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
           at System.Runtime.CompilerServices.TaskAwaiter`1.GetResult()
           at ICSharpCode.CodeConverter.CSharp.CommentConvertingNodesVisitor.<DefaultVisit>d__5.MoveNext() in C:\Users\Graham\Documents\GitHub\CodeConverter\ICSharpCode.CodeConverter\CSharp\CommentConvertingNodesVisitor.cs:line 30
        --- End of stack trace from previous location where exception was thrown ---
           at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
           at System.Runtime.CompilerServices.TaskAwaiter`1.GetResult()
           at ICSharpCode.CodeConverter.CSharp.DeclarationNodeVisitor.<ConvertMember>d__35.MoveNext() in C:\Users\Graham\Documents\GitHub\CodeConverter\ICSharpCode.CodeConverter\CSharp\DeclarationNodeVisitor.cs:line 204

        Input:
                Public Const vbCrLf As String = ChrW(13) & ChrW(10)

         */
        class _failedMemberConversionMarker2
        {
        }
#error Cannot convert FieldDeclarationSyntax - see comment for details
        /* Cannot convert FieldDeclarationSyntax, System.NullReferenceException: Object reference not set to an instance of an object.
           at ICSharpCode.CodeConverter.CSharp.ExpressionNodeVisitor.<VisitBinaryExpression>d__62.MoveNext() in C:\Users\Graham\Documents\GitHub\CodeConverter\ICSharpCode.CodeConverter\CSharp\ExpressionNodeVisitor.cs:line 570
        --- End of stack trace from previous location where exception was thrown ---
           at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
           at System.Runtime.CompilerServices.TaskAwaiter`1.GetResult()
           at ICSharpCode.CodeConverter.CSharp.CommentConvertingVisitorWrapper`1.<Visit>d__5.MoveNext() in C:\Users\Graham\Documents\GitHub\CodeConverter\ICSharpCode.CodeConverter\CSharp\CommentConvertingVisitorWrapper.cs:line 22
        --- End of stack trace from previous location where exception was thrown ---
           at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
           at System.Runtime.CompilerServices.TaskAwaiter`1.GetResult()
           at ICSharpCode.CodeConverter.CSharp.SyntaxNodeVisitorExtensions.<AcceptAsync>d__0`1.MoveNext() in C:\Users\Graham\Documents\GitHub\CodeConverter\ICSharpCode.CodeConverter\CSharp\SyntaxNodeVisitorExtensions.cs:line 16
        --- End of stack trace from previous location where exception was thrown ---
           at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
           at System.Runtime.CompilerServices.TaskAwaiter`1.GetResult()
           at ICSharpCode.CodeConverter.CSharp.CommonConversions.<SplitVariableDeclarations>d__18.MoveNext() in C:\Users\Graham\Documents\GitHub\CodeConverter\ICSharpCode.CodeConverter\CSharp\CommonConversions.cs:line 63
        --- End of stack trace from previous location where exception was thrown ---
           at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
           at System.Runtime.CompilerServices.TaskAwaiter`1.GetResult()
           at ICSharpCode.CodeConverter.CSharp.DeclarationNodeVisitor.<GetMemberDeclarations>d__48.MoveNext() in C:\Users\Graham\Documents\GitHub\CodeConverter\ICSharpCode.CodeConverter\CSharp\DeclarationNodeVisitor.cs:line 412
        --- End of stack trace from previous location where exception was thrown ---
           at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
           at System.Runtime.CompilerServices.TaskAwaiter`1.GetResult()
           at ICSharpCode.CodeConverter.CSharp.DeclarationNodeVisitor.<VisitFieldDeclaration>d__47.MoveNext() in C:\Users\Graham\Documents\GitHub\CodeConverter\ICSharpCode.CodeConverter\CSharp\DeclarationNodeVisitor.cs:line 392
        --- End of stack trace from previous location where exception was thrown ---
           at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
           at System.Runtime.CompilerServices.TaskAwaiter`1.GetResult()
           at ICSharpCode.CodeConverter.CSharp.CommentConvertingNodesVisitor.<DefaultVisit>d__5.MoveNext() in C:\Users\Graham\Documents\GitHub\CodeConverter\ICSharpCode.CodeConverter\CSharp\CommentConvertingNodesVisitor.cs:line 30
        --- End of stack trace from previous location where exception was thrown ---
           at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
           at System.Runtime.CompilerServices.TaskAwaiter`1.GetResult()
           at ICSharpCode.CodeConverter.CSharp.DeclarationNodeVisitor.<ConvertMember>d__35.MoveNext() in C:\Users\Graham\Documents\GitHub\CodeConverter\ICSharpCode.CodeConverter\CSharp\DeclarationNodeVisitor.cs:line 204

        Input:
                Public Const vbNewLine As String = ChrW(13) & ChrW(10)

         */
        public const var vbCr = ChrW(13);
        public const var vbLf = ChrW(10);
        public const var vbBack = ChrW(8);
        public const var vbFormFeed = ChrW(12);
        public const var vbTab = ChrW(9);
        public const var vbVerticalTab = ChrW(11);
        public const var vbNullChar = ChrW(0);
        public const var vbNullString = null;
    }
}

namespace VbNetStandardLib
{

    // Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

    // See Compiler::LoadXmlSolutionExtension
    namespace My
    {
        [Embedded()]
        [DebuggerNonUserCode()]
        [System.Runtime.CompilerServices.CompilerGenerated()]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        internal sealed class InternalXmlHelper
        {
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            private InternalXmlHelper()
            {
            }
            public static string get_Value(IEnumerable<XElement> source)
            {
                foreach (XElement item in source)
                    return item.Value;
                return null;
            }

            public static void set_Value(IEnumerable<XElement> source, string value)
            {
                foreach (XElement item in source)
                {
                    item.Value = value;
                    break;
                }
            }
            public static string get_AttributeValue(IEnumerable<XElement> source, XName name)
            {
                foreach (XElement item in source)
                    return Conversions.ToString(item.Attribute(name));
                return null;
            }

            public static void set_AttributeValue(IEnumerable<XElement> source, XName name, string value)
            {
                foreach (XElement item in source)
                {
                    item.SetAttributeValue(name, value);
                    break;
                }
            }
            public static string get_AttributeValue(XElement source, XName name)
            {
                return Conversions.ToString(source.Attribute(name));
            }

            public static void set_AttributeValue(XElement source, XName name, string value)
            {
                source.SetAttributeValue(name, value);
            }
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public static XAttribute CreateAttribute(XName name, object value)
            {
                if (value == null)
                    return null;
                return new XAttribute(name, value);
            }
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public static XAttribute CreateNamespaceAttribute(XName name, XNamespace ns)
            {
                var a = new XAttribute(name, ns.NamespaceName);
                a.AddAnnotation(ns);
                return a;
            }
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public static object RemoveNamespaceAttributes(string[] inScopePrefixes, XNamespace[] inScopeNs, List<XAttribute> attributes, object obj)
            {
                if (obj != null)
                {
                    var elem = obj as XElement;
                    if (!(elem == null))
                        return RemoveNamespaceAttributes(inScopePrefixes, inScopeNs, attributes, elem);
                    else
                    {
                        var elems = obj as IEnumerable;
                        if (elems != null)
                            return RemoveNamespaceAttributes(inScopePrefixes, inScopeNs, attributes, elems);
                    }
                }
                return obj;
            }
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public static IEnumerable RemoveNamespaceAttributes(string[] inScopePrefixes, XNamespace[] inScopeNs, List<XAttribute> attributes, IEnumerable obj)
            {
                if (obj != null)
                {
                    var elems = obj as IEnumerable<XElement>;
                    if (elems != null)
                        return elems.Select(new RemoveNamespaceAttributesClosure(inScopePrefixes, inScopeNs, attributes).ProcessXElement);
                    else
                        return obj.Cast<object>().Select(new RemoveNamespaceAttributesClosure(inScopePrefixes, inScopeNs, attributes).ProcessObject);
                }
                return obj;
            }
            [DebuggerNonUserCode()]
            [System.Runtime.CompilerServices.CompilerGenerated()]
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            private sealed class RemoveNamespaceAttributesClosure
            {
                private readonly string[] m_inScopePrefixes;
                private readonly XNamespace[] m_inScopeNs;
                private readonly List<XAttribute> m_attributes;
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                internal RemoveNamespaceAttributesClosure(string[] inScopePrefixes, XNamespace[] inScopeNs, List<XAttribute> attributes)
                {
                    m_inScopePrefixes = inScopePrefixes;
                    m_inScopeNs = inScopeNs;
                    m_attributes = attributes;
                }
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                internal XElement ProcessXElement(XElement elem)
                {
                    return RemoveNamespaceAttributes(m_inScopePrefixes, m_inScopeNs, m_attributes, elem);
                }
                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                internal object ProcessObject(object obj)
                {
                    var elem = obj as XElement;
                    if (elem != null)
                        return RemoveNamespaceAttributes(m_inScopePrefixes, m_inScopeNs, m_attributes, elem);
                    else
                        return obj;
                }
            }
            [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
            public static XElement RemoveNamespaceAttributes(string[] inScopePrefixes, XNamespace[] inScopeNs, List<XAttribute> attributes, XElement e)
            {
                if (e != null)
                {
                    var a = e.FirstAttribute;

                    while (a != null)
                    {
                        var nextA = a.NextAttribute;

                        if (a.IsNamespaceDeclaration)
                        {
                            var ns = a.Annotation<XNamespace>();
                            string prefix = a.Name.LocalName;

                            if (ns != null)
                            {
                                if (inScopePrefixes != null && inScopeNs != null)
                                {
                                    int lastIndex = inScopePrefixes.Length - 1;

                                    for (int i = 0, loopTo = lastIndex; i <= loopTo; i++)
                                    {
                                        string currentInScopePrefix = inScopePrefixes[i];
                                        var currentInScopeNs = inScopeNs[i];
                                        if (prefix.Equals(currentInScopePrefix))
                                        {
                                            if (ns == currentInScopeNs)
                                                // prefix and namespace match.  Remove the unneeded ns attribute 
                                                a.Remove();

                                            // prefix is in scope but refers to something else.  Leave the ns attribute. 
                                            a = null;
                                            break;
                                        }
                                    }
                                }

                                if (a != null)
                                {
                                    // Prefix is not in scope 
                                    // Now check whether it's going to be in scope because it is in the attributes list 

                                    if (attributes != null)
                                    {
                                        int lastIndex = attributes.Count - 1;
                                        for (int i = 0, loopTo1 = lastIndex; i <= loopTo1; i++)
                                        {
                                            var currentA = attributes[i];
                                            string currentInScopePrefix = currentA.Name.LocalName;
                                            var currentInScopeNs = currentA.Annotation<XNamespace>();
                                            if (currentInScopeNs != null)
                                            {
                                                if (prefix.Equals(currentInScopePrefix))
                                                {
                                                    if (ns == currentInScopeNs)
                                                        // prefix and namespace match.  Remove the unneeded ns attribute 
                                                        a.Remove();

                                                    // prefix is in scope but refers to something else.  Leave the ns attribute. 
                                                    a = null;
                                                    break;
                                                }
                                            }
                                        }
                                    }

                                    if (a != null)
                                    {
                                        // Prefix is definitely not in scope  
                                        a.Remove();
                                        // namespace is not defined either.  Add this attributes list 
                                        attributes.Add(a);
                                    }
                                }
                            }
                        }

                        a = nextA;
                    }
                }
                return e;
            }
        }
    }
}
