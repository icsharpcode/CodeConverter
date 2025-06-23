using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal enum TestEnum
{
    None = 1
}

internal partial class Class1
{
    private void Test(bool b, float f, double d, decimal m)
    {
        int i = Conversions.ToInteger(b);
        i = (int)Math.Round(f);
        i = (int)Math.Round(d);
        i = (int)Math.Round(m);

        uint ui = Conversions.ToUInteger(b);
        ui = (uint)Math.Round(f);
        ui = (uint)Math.Round(d);
        ui = (uint)Math.Round(m);

        short s = Conversions.ToShort(b);
        s = (short)Math.Round(f);
        s = (short)Math.Round(d);
        s = (short)Math.Round(m);

        long l = Conversions.ToLong(b);
        l = (long)Math.Round(f);
        l = (long)Math.Round(d);
        l = (long)Math.Round(m);

        byte byt = Conversions.ToByte(b);
        byt = (byte)Math.Round(f);
        byt = (byte)Math.Round(d);
        byt = (byte)Math.Round(m);

        TestEnum e = (TestEnum)Conversions.ToInteger(b);
        e = (TestEnum)Math.Round(f);
        e = (TestEnum)Math.Round(d);
        e = (TestEnum)Math.Round(m);
    }

    private void TestNullable(bool? b, float? f, double? d, decimal? m)
    {
        int i = Conversions.ToInteger(b.Value);
        i = (int)Math.Round(f.Value);
        i = (int)Math.Round(d.Value);
        i = (int)Math.Round(m.Value);

        uint ui = Conversions.ToUInteger(b.Value);
        ui = (uint)Math.Round(f.Value);
        ui = (uint)Math.Round(d.Value);
        ui = (uint)Math.Round(m.Value);

        short s = Conversions.ToShort(b.Value);
        s = (short)Math.Round(f.Value);
        s = (short)Math.Round(d.Value);
        s = (short)Math.Round(m.Value);

        long l = Conversions.ToLong(b.Value);
        l = (long)Math.Round(f.Value);
        l = (long)Math.Round(d.Value);
        l = (long)Math.Round(m.Value);

        byte byt = Conversions.ToByte(b.Value);
        byt = (byte)Math.Round(f.Value);
        byt = (byte)Math.Round(d.Value);
        byt = (byte)Math.Round(m.Value);

        TestEnum e = (TestEnum)Conversions.ToInteger(b.Value);
        e = (TestEnum)Math.Round(f.Value);
        e = (TestEnum)Math.Round(d.Value);
        e = (TestEnum)Math.Round(m.Value);
    }
}
