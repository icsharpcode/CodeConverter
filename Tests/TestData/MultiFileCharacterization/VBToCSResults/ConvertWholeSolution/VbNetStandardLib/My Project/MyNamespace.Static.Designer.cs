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
                    if (Right.Length == 0)
                        return 0;
                    return -1;
                }
                if (Right == null)
                {
                    if (Left.Length == 0)
                        return 0;
                    return 1;
                }
                int Result;
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
                    return ToSByte(Value);
                else if (underlyingType.Equals(typeof(byte)))
                    return ToByte(Value);
                else if (underlyingType.Equals(typeof(short)))
                    return ToShort(Value);
                else if (underlyingType.Equals(typeof(ushort)))
                    return ToUShort(Value);
                else if (underlyingType.Equals(typeof(int)))
                    return ToInteger(Value);
                else if (underlyingType.Equals(typeof(uint)))
                    return ToUInteger(Value);
                else if (underlyingType.Equals(typeof(long)))
                    return ToLong(Value);
                else if (underlyingType.Equals(typeof(ulong)))
                    return ToULong(Value);
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
                    var i64Value = default(long);
                    if (IsHexOrOctValue(Value, ref i64Value))
                        return ToBoolean(i64Value);
                    return ToBoolean(ParseDouble(Value));
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
                    return ToBoolean(Value);
                else if (Value is sbyte)
                    return ToBoolean(ToSByte(Value));
                else if (Value is byte)
                    return ToBoolean(ToByte(Value));
                else if (Value is short)
                    return ToBoolean(ToShort(Value));
                else if (Value is ushort)
                    return ToBoolean(ToUShort(Value));
                else if (Value is int)
                    return ToBoolean(ToInteger(Value));
                else if (Value is uint)
                    return ToBoolean(ToUInteger(Value));
                else if (Value is long)
                    return ToBoolean(ToLong(Value));
                else if (Value is ulong)
                    return ToBoolean(ToULong(Value));
                else if (Value is decimal)
                    return ToBoolean(ToDecimal(Value));
                else if (Value is float)
                    return ToBoolean(ToSingle(Value));
                else if (Value is double)
                    return ToBoolean(ToDouble(Value));
                else if (Value is string)
                    return ToBoolean(ToString(Value));
                throw new InvalidCastException();
            }
            public static byte ToByte(string Value)
            {
                if (Value == null)
                    return 0;
                try
                {
                    var i64Value = default(long);
                    if (IsHexOrOctValue(Value, ref i64Value))
                        return ToByte(i64Value);
                    return ToByte(ParseDouble(Value));
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
                    return ToByte(ToBoolean(Value));
                else if (Value is sbyte)
                    return ToByte(ToSByte(Value));
                else if (Value is byte)
                    return ToByte(Value);
                else if (Value is short)
                    return ToByte(ToShort(Value));
                else if (Value is ushort)
                    return ToByte(ToUShort(Value));
                else if (Value is int)
                    return ToByte(ToInteger(Value));
                else if (Value is uint)
                    return ToByte(ToUInteger(Value));
                else if (Value is long)
                    return ToByte(ToLong(Value));
                else if (Value is ulong)
                    return ToByte(ToULong(Value));
                else if (Value is decimal)
                    return ToByte(ToDecimal(Value));
                else if (Value is float)
                    return ToByte(ToSingle(Value));
                else if (Value is double)
                    return ToByte(ToDouble(Value));
                else if (Value is string)
                    return ToByte(ToString(Value));
                throw new InvalidCastException();
            }
            [CLSCompliant(false)]
            public static sbyte ToSByte(string Value)
            {
                if (Value == null)
                    return 0;
                try
                {
                    var i64Value = default(long);
                    if (IsHexOrOctValue(Value, ref i64Value))
                        return ToSByte(i64Value);
                    return ToSByte(ParseDouble(Value));
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
                    return ToSByte(ToBoolean(Value));
                else if (Value is sbyte)
                    return ToSByte(Value);
                else if (Value is byte)
                    return ToSByte(ToByte(Value));
                else if (Value is short)
                    return ToSByte(ToShort(Value));
                else if (Value is ushort)
                    return ToSByte(ToUShort(Value));
                else if (Value is int)
                    return ToSByte(ToInteger(Value));
                else if (Value is uint)
                    return ToSByte(ToUInteger(Value));
                else if (Value is long)
                    return ToSByte(ToLong(Value));
                else if (Value is ulong)
                    return ToSByte(ToULong(Value));
                else if (Value is decimal)
                    return ToSByte(ToDecimal(Value));
                else if (Value is float)
                    return ToSByte(ToSingle(Value));
                else if (Value is double)
                    return ToSByte(ToDouble(Value));
                else if (Value is string)
                    return ToSByte(ToString(Value));
                throw new InvalidCastException();
            }
            public static short ToShort(string Value)
            {
                if (Value == null)
                    return 0;
                try
                {
                    var i64Value = default(long);
                    if (IsHexOrOctValue(Value, ref i64Value))
                        return ToShort(i64Value);
                    return ToShort(ParseDouble(Value));
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
                    return ToShort(ToBoolean(Value));
                else if (Value is sbyte)
                    return ToSByte(Value);
                else if (Value is byte)
                    return ToByte(Value);
                else if (Value is short)
                    return ToShort(Value);
                else if (Value is ushort)
                    return ToShort(ToUShort(Value));
                else if (Value is int)
                    return ToShort(ToInteger(Value));
                else if (Value is uint)
                    return ToShort(ToUInteger(Value));
                else if (Value is long)
                    return ToShort(ToLong(Value));
                else if (Value is ulong)
                    return ToShort(ToULong(Value));
                else if (Value is decimal)
                    return ToShort(ToDecimal(Value));
                else if (Value is float)
                    return ToShort(ToSingle(Value));
                else if (Value is double)
                    return ToShort(ToDouble(Value));
                else if (Value is string)
                    return ToShort(ToString(Value));
                throw new InvalidCastException();
            }
            [CLSCompliant(false)]
            public static ushort ToUShort(string Value)
            {
                if (Value == null)
                    return 0;
                try
                {
                    var i64Value = default(long);
                    if (IsHexOrOctValue(Value, ref i64Value))
                        return ToUShort(i64Value);
                    return ToUShort(ParseDouble(Value));
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
                    return ToUShort(ToBoolean(Value));
                else if (Value is sbyte)
                    return ToUShort(ToSByte(Value));
                else if (Value is byte)
                    return ToByte(Value);
                else if (Value is short)
                    return ToUShort(ToShort(Value));
                else if (Value is ushort)
                    return ToUShort(Value);
                else if (Value is int)
                    return ToUShort(ToInteger(Value));
                else if (Value is uint)
                    return ToUShort(ToUInteger(Value));
                else if (Value is long)
                    return ToUShort(ToLong(Value));
                else if (Value is ulong)
                    return ToUShort(ToULong(Value));
                else if (Value is decimal)
                    return ToUShort(ToDecimal(Value));
                else if (Value is float)
                    return ToUShort(ToSingle(Value));
                else if (Value is double)
                    return ToUShort(ToDouble(Value));
                else if (Value is string)
                    return ToUShort(ToString(Value));
                throw new InvalidCastException();
            }
            public static int ToInteger(string Value)
            {
                if (Value == null)
                    return 0;
                try
                {
                    var i64Value = default(long);
                    if (IsHexOrOctValue(Value, ref i64Value))
                        return ToInteger(i64Value);
                    return ToInteger(ParseDouble(Value));
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
                    return ToInteger(ToBoolean(Value));
                else if (Value is sbyte)
                    return ToSByte(Value);
                else if (Value is byte)
                    return ToByte(Value);
                else if (Value is short)
                    return ToShort(Value);
                else if (Value is ushort)
                    return ToUShort(Value);
                else if (Value is int)
                    return ToInteger(Value);
                else if (Value is uint)
                    return ToInteger(ToUInteger(Value));
                else if (Value is long)
                    return ToInteger(ToLong(Value));
                else if (Value is ulong)
                    return ToInteger(ToULong(Value));
                else if (Value is decimal)
                    return ToInteger(ToDecimal(Value));
                else if (Value is float)
                    return ToInteger(ToSingle(Value));
                else if (Value is double)
                    return ToInteger(ToDouble(Value));
                else if (Value is string)
                    return ToInteger(ToString(Value));
                throw new InvalidCastException();
            }
            [CLSCompliant(false)]
            public static uint ToUInteger(string Value)
            {
                if (Value == null)
                    return 0;
                try
                {
                    var i64Value = default(long);
                    if (IsHexOrOctValue(Value, ref i64Value))
                        return ToUInteger(i64Value);
                    return ToUInteger(ParseDouble(Value));
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
                    return ToUInteger(ToBoolean(Value));
                else if (Value is sbyte)
                    return ToUInteger(ToSByte(Value));
                else if (Value is byte)
                    return ToByte(Value);
                else if (Value is short)
                    return ToUInteger(ToShort(Value));
                else if (Value is ushort)
                    return ToUShort(Value);
                else if (Value is int)
                    return ToUInteger(ToInteger(Value));
                else if (Value is uint)
                    return ToUInteger(Value);
                else if (Value is long)
                    return ToUInteger(ToLong(Value));
                else if (Value is ulong)
                    return ToUInteger(ToULong(Value));
                else if (Value is decimal)
                    return ToUInteger(ToDecimal(Value));
                else if (Value is float)
                    return ToUInteger(ToSingle(Value));
                else if (Value is double)
                    return ToUInteger(ToDouble(Value));
                else if (Value is string)
                    return ToUInteger(ToString(Value));
                throw new InvalidCastException();
            }
            public static long ToLong(string Value)
            {
                if (Value == null)
                    return 0;
                try
                {
                    var i64Value = default(long);
                    if (IsHexOrOctValue(Value, ref i64Value))
                        return i64Value;
                    return ToLong(ParseDecimal(Value, null));
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
                    return ToLong(ToBoolean(Value));
                else if (Value is sbyte)
                    return ToSByte(Value);
                else if (Value is byte)
                    return ToByte(Value);
                else if (Value is short)
                    return ToShort(Value);
                else if (Value is ushort)
                    return ToUShort(Value);
                else if (Value is int)
                    return ToInteger(Value);
                else if (Value is uint)
                    return ToUInteger(Value);
                else if (Value is long)
                    return ToLong(Value);
                else if (Value is ulong)
                    return ToLong(ToULong(Value));
                else if (Value is decimal)
                    return ToLong(ToDecimal(Value));
                else if (Value is float)
                    return ToLong(ToSingle(Value));
                else if (Value is double)
                    return ToLong(ToDouble(Value));
                else if (Value is string)
                    return ToLong(ToString(Value));
                throw new InvalidCastException();
            }
            [CLSCompliant(false)]
            public static ulong ToULong(string Value)
            {
                if (Value == null)
                    return 0;
                try
                {
                    var ui64Value = default(ulong);
                    if (IsHexOrOctValue(Value, ref ui64Value))
                        return ui64Value;
                    return ToULong(ParseDecimal(Value, null));
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
                    return ToULong(ToBoolean(Value));
                else if (Value is sbyte)
                    return ToULong(ToSByte(Value));
                else if (Value is byte)
                    return ToByte(Value);
                else if (Value is short)
                    return ToULong(ToShort(Value));
                else if (Value is ushort)
                    return ToUShort(Value);
                else if (Value is int)
                    return ToULong(ToInteger(Value));
                else if (Value is uint)
                    return ToUInteger(Value);
                else if (Value is long)
                    return ToULong(ToLong(Value));
                else if (Value is ulong)
                    return ToULong(Value);
                else if (Value is decimal)
                    return ToULong(ToDecimal(Value));
                else if (Value is float)
                    return ToULong(ToSingle(Value));
                else if (Value is double)
                    return ToULong(ToDouble(Value));
                else if (Value is string)
                    return ToULong(ToString(Value));
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
                    var i64Value = default(long);
                    if (IsHexOrOctValue(Value, ref i64Value))
                        return i64Value;
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
                    return ToDecimal(ToBoolean(Value));
                else if (Value is sbyte)
                    return ToSByte(Value);
                else if (Value is byte)
                    return ToByte(Value);
                else if (Value is short)
                    return ToShort(Value);
                else if (Value is ushort)
                    return ToUShort(Value);
                else if (Value is int)
                    return ToInteger(Value);
                else if (Value is uint)
                    return ToUInteger(Value);
                else if (Value is long)
                    return ToLong(Value);
                else if (Value is ulong)
                    return ToULong(Value);
                else if (Value is decimal)
                    return ToDecimal(Value);
                else if (Value is float)
                    return ToDecimal(ToSingle(Value));
                else if (Value is double)
                    return ToDecimal(ToDouble(Value));
                else if (Value is string)
                    return ToDecimal(ToString(Value));
                throw new InvalidCastException();
            }
            private static decimal ParseDecimal(string Value, System.Globalization.NumberFormatInfo NumberFormat)
            {
                System.Globalization.NumberFormatInfo NormalizedNumberFormat;
                var culture = GetCultureInfo();
                if (NumberFormat == null)
                    NumberFormat = culture.NumberFormat;
                NormalizedNumberFormat = GetNormalizedNumberFormat(NumberFormat);
                const System.Globalization.NumberStyles flags = System.Globalization.NumberStyles.AllowDecimalPoint | System.Globalization.NumberStyles.AllowExponent | System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowLeadingWhite | System.Globalization.NumberStyles.AllowThousands | System.Globalization.NumberStyles.AllowTrailingSign | System.Globalization.NumberStyles.AllowParentheses | System.Globalization.NumberStyles.AllowTrailingWhite | System.Globalization.NumberStyles.AllowCurrencySymbol;
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
                System.Globalization.NumberFormatInfo OutNumberFormat;
                {
                    var withBlock = InNumberFormat;
                    if (!(withBlock.CurrencyDecimalSeparator == null) && !(withBlock.NumberDecimalSeparator == null) && !(withBlock.CurrencyGroupSeparator == null) && !(withBlock.NumberGroupSeparator == null) && withBlock.CurrencyDecimalSeparator.Length == 1 && withBlock.NumberDecimalSeparator.Length == 1 && withBlock.CurrencyGroupSeparator.Length == 1 && withBlock.NumberGroupSeparator.Length == 1 && withBlock.CurrencyDecimalSeparator[0] == withBlock.NumberDecimalSeparator[0] && withBlock.CurrencyGroupSeparator[0] == withBlock.NumberGroupSeparator[0] && withBlock.CurrencyDecimalDigits == withBlock.NumberDecimalDigits)
                        return InNumberFormat;
                }
                {
                    var withBlock1 = InNumberFormat;
                    if (!(withBlock1.CurrencyDecimalSeparator == null) && !(withBlock1.NumberDecimalSeparator == null) && withBlock1.CurrencyDecimalSeparator.Length == withBlock1.NumberDecimalSeparator.Length && !(withBlock1.CurrencyGroupSeparator == null) && !(withBlock1.NumberGroupSeparator == null) && withBlock1.CurrencyGroupSeparator.Length == withBlock1.NumberGroupSeparator.Length)
                    {
                        int i;
                        var loopTo = withBlock1.CurrencyDecimalSeparator.Length - 1;
                        for (i = 0; i <= loopTo; i++)
                        {
                            if (withBlock1.CurrencyDecimalSeparator[i] != withBlock1.NumberDecimalSeparator[i])
                                goto MisMatch;
                        }

                        var loopTo1 = withBlock1.CurrencyGroupSeparator.Length - 1;
                        for (i = 0; i <= loopTo1; i++)
                        {
                            if (withBlock1.CurrencyGroupSeparator[i] != withBlock1.NumberGroupSeparator[i])
                                goto MisMatch;
                        }
                        return InNumberFormat;
                    }
                }

            MisMatch:
                ;
                OutNumberFormat = InNumberFormat.Clone();
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
                    var i64Value = default(long);
                    if (IsHexOrOctValue(Value, ref i64Value))
                        return i64Value;
                    double Result = ParseDouble(Value);
                    if ((Result < float.MinValue || Result > float.MaxValue) && !double.IsInfinity(Result))
                        throw new OverflowException();
                    return ToSingle(Result);
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
                    return ToSingle(ToBoolean(Value));
                else if (Value is sbyte)
                    return ToSByte(Value);
                else if (Value is byte)
                    return ToByte(Value);
                else if (Value is short)
                    return ToShort(Value);
                else if (Value is ushort)
                    return ToUShort(Value);
                else if (Value is int)
                    return ToInteger(Value);
                else if (Value is uint)
                    return ToUInteger(Value);
                else if (Value is long)
                    return ToLong(Value);
                else if (Value is ulong)
                    return ToULong(Value);
                else if (Value is decimal)
                    return ToSingle(ToDecimal(Value));
                else if (Value is float)
                    return ToSingle(Value);
                else if (Value is double)
                    return ToSingle(ToDouble(Value));
                else if (Value is string)
                    return ToSingle(ToString(Value));
                throw new InvalidCastException();
            }
            public static double ToDouble(string Value)
            {
                if (Value == null)
                    return 0;
                try
                {
                    var i64Value = default(long);
                    if (IsHexOrOctValue(Value, ref i64Value))
                        return i64Value;
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
                    return ToDouble(ToBoolean(Value));
                else if (Value is sbyte)
                    return ToSByte(Value);
                else if (Value is byte)
                    return ToByte(Value);
                else if (Value is short)
                    return ToShort(Value);
                else if (Value is ushort)
                    return ToUShort(Value);
                else if (Value is int)
                    return ToInteger(Value);
                else if (Value is uint)
                    return ToUInteger(Value);
                else if (Value is long)
                    return ToLong(Value);
                else if (Value is ulong)
                    return ToULong(Value);
                else if (Value is decimal)
                    return ToDouble(ToDecimal(Value));
                else if (Value is float)
                    return ToSingle(Value);
                else if (Value is double)
                    return ToDouble(Value);
                else if (Value is string)
                    return ToDouble(ToString(Value));
                throw new InvalidCastException();
            }
            private static double ParseDouble(string Value)
            {
                System.Globalization.NumberFormatInfo NormalizedNumberFormat;
                var culture = GetCultureInfo();
                var NumberFormat = culture.NumberFormat;
                NormalizedNumberFormat = GetNormalizedNumberFormat(NumberFormat);
                const System.Globalization.NumberStyles flags = System.Globalization.NumberStyles.AllowDecimalPoint | System.Globalization.NumberStyles.AllowExponent | System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowLeadingWhite | System.Globalization.NumberStyles.AllowThousands | System.Globalization.NumberStyles.AllowTrailingSign | System.Globalization.NumberStyles.AllowParentheses | System.Globalization.NumberStyles.AllowTrailingWhite | System.Globalization.NumberStyles.AllowCurrencySymbol;
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
                DateTime ParsedDate;
                const System.Globalization.DateTimeStyles ParseStyle = System.Globalization.DateTimeStyles.AllowWhiteSpaces | System.Globalization.DateTimeStyles.NoCurrentDateDefault;
                var Culture = GetCultureInfo();
                bool result = DateTime.TryParse(ToHalfwidthNumbers(Value, Culture), Culture, ParseStyle, out ParsedDate);
                if (result)
                    return ParsedDate;
                else
                    throw new InvalidCastException();
            }
            public static DateTime ToDate(object Value)
            {
                if (Value == null)
                    return default(DateTime);
                if (Value is DateTime)
                    return ToDate(Value);
                else if (Value is string)
                    return ToDate(ToString(Value));
                throw new InvalidCastException();
            }
            public static char ToChar(string Value)
            {
                if (Value == null || Value.Length == 0)
                    return Convert.ToChar(0 & 0xFFFF);
                return Value[0];
            }
            public static char ToChar(object Value)
            {
                if (Value == null)
                    return Convert.ToChar(0 & 0xFFFF);
                if (Value is char)
                    return ToChar(Value);
                else if (Value is string)
                    return ToChar(ToString(Value));
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
                    return ToString(Value).ToCharArray();
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
                long TimeTicks = Value.TimeOfDay.Ticks;
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
                    string StringValue = Value as string;
                    if (StringValue != null)
                        return StringValue;
                }
                if (Value is Enum)
                    Value = GetEnumValue(Value);
                if (Value is bool)
                    return ToString(ToBoolean(Value));
                else if (Value is sbyte)
                    return ToString(ToSByte(Value));
                else if (Value is byte)
                    return ToString(ToByte(Value));
                else if (Value is short)
                    return ToString(ToShort(Value));
                else if (Value is ushort)
                    return ToString(ToUShort(Value));
                else if (Value is int)
                    return ToString(ToInteger(Value));
                else if (Value is uint)
                    return ToString(ToUInteger(Value));
                else if (Value is long)
                    return ToString(ToLong(Value));
                else if (Value is ulong)
                    return ToString(ToULong(Value));
                else if (Value is decimal)
                    return ToString(ToDecimal(Value));
                else if (Value is float)
                    return ToString(ToSingle(Value));
                else if (Value is double)
                    return ToString(ToDouble(Value));
                else if (Value is char)
                    return ToString(ToChar(Value));
                else if (Value is DateTime)
                    return ToString(ToDate(Value));
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
                char ch;
                int Length;
                var FirstNonspace = default(int);
                string TmpValue;
                Length = Value.Length;
                while (FirstNonspace < Length)
                {
                    ch = Value[FirstNonspace];
                    if (ch == '&' && FirstNonspace + 2 < Length)
                        goto GetSpecialValue;
                    if (ch != Strings.ChrW(32) && ch != Strings.ChrW(0x3000))
                        return false;
                    FirstNonspace += 1;
                }
                return false;
            GetSpecialValue:
                ;
                ch = char.ToLowerInvariant(Value[FirstNonspace + 1]);
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
                char ch;
                int Length;
                var FirstNonspace = default(int);
                string TmpValue;
                Length = Value.Length;
                while (FirstNonspace < Length)
                {
                    ch = Value[FirstNonspace];
                    if (ch == '&' && FirstNonspace + 2 < Length)
                        goto GetSpecialValue;
                    if (ch != Strings.ChrW(32) && ch != Strings.ChrW(0x3000))
                        return false;
                    FirstNonspace += 1;
                }
                return false;
            GetSpecialValue:
                ;
                ch = char.ToLowerInvariant(Value[FirstNonspace + 1]);
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
                    return default(T);
                var reflectedType = typeof(T);
                if (Equals(reflectedType, typeof(bool)))
                    return ToBoolean(Value);
                else if (Equals(reflectedType, typeof(sbyte)))
                    return ToSByte(Value);
                else if (Equals(reflectedType, typeof(byte)))
                    return ToByte(Value);
                else if (Equals(reflectedType, typeof(short)))
                    return ToShort(Value);
                else if (Equals(reflectedType, typeof(ushort)))
                    return ToUShort(Value);
                else if (Equals(reflectedType, typeof(int)))
                    return ToInteger(Value);
                else if (Equals(reflectedType, typeof(uint)))
                    return ToUInteger(Value);
                else if (Equals(reflectedType, typeof(long)))
                    return ToLong(Value);
                else if (Equals(reflectedType, typeof(ulong)))
                    return ToULong(Value);
                else if (Equals(reflectedType, typeof(decimal)))
                    return ToDecimal(Value);
                else if (Equals(reflectedType, typeof(float)))
                    return ToSingle(Value);
                else if (Equals(reflectedType, typeof(double)))
                    return ToDouble(Value);
                else if (Equals(reflectedType, typeof(DateTime)))
                    return ToDate(Value);
                else if (Equals(reflectedType, typeof(char)))
                    return ToChar(Value);
                else if (Equals(reflectedType, typeof(string)))
                    return ToString(Value);
                else
                    return Value;
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
                int lLength;
                lLength = arySrc.Length;
                if (lLength == 0)
                    return aryDest;
                if (aryDest.Rank != arySrc.Rank)
                    throw new InvalidCastException();
                int iDim;
                var loopTo = aryDest.Rank - 2;
                for (iDim = 0; iDim <= loopTo; iDim++)
                {
                    if (aryDest.GetUpperBound(iDim) != arySrc.GetUpperBound(iDim))
                        throw new ArrayTypeMismatchException();
                }
                if (lLength > aryDest.Length)
                    lLength = aryDest.Length;
                if (arySrc.Rank > 1)
                {
                    int LastRank = arySrc.Rank;
                    int lenSrcLastRank = arySrc.GetLength(LastRank - 1);
                    int lenDestLastRank = aryDest.GetLength(LastRank - 1);
                    if (lenDestLastRank == 0)
                        return aryDest;
                    int lenCopy = lenSrcLastRank > lenDestLastRank ? lenDestLastRank : lenSrcLastRank;
                    int i;
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
            public short State;
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
            return AscW(String[0]);
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
        public const string vbCrLf = Conversions.ToString(Strings.ChrW(13)) + Conversions.ToString(Strings.ChrW(10));
        public const string vbNewLine = Conversions.ToString(Strings.ChrW(13)) + Conversions.ToString(Strings.ChrW(10));
        public const string vbCr = "\r";
        public const string vbLf = "\n";
        public const string vbBack = "\b";
        public const string vbFormFeed = "\f";
        public const string vbTab = "\t";
        public const string vbVerticalTab = "\v";
        public const string vbNullChar = "\0";
        public const string vbNullString = null;
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
                    return item.Attribute(name);
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
                return source.Attribute(name);
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
