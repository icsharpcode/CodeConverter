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
        int? i = Conversions.ToInteger(b);
        i = (int?)Math.Round(f);
        i = (int?)Math.Round(d);
        i = (int?)Math.Round(m);

        uint? ui = Conversions.ToUInteger(b);
        ui = (uint?)Math.Round(f);
        ui = (uint?)Math.Round(d);
        ui = (uint?)Math.Round(m);

        short? s = Conversions.ToShort(b);
        s = (short?)Math.Round(f);
        s = (short?)Math.Round(d);
        s = (short?)Math.Round(m);

        long? l = Conversions.ToLong(b);
        l = (long?)Math.Round(f);
        l = (long?)Math.Round(d);
        l = (long?)Math.Round(m);

        byte? byt = Conversions.ToByte(b);
        byt = (byte?)Math.Round(f);
        byt = (byte?)Math.Round(d);
        byt = (byte?)Math.Round(m);

        TestEnum? e = (TestEnum?)Conversions.ToInteger(b);
        e = (TestEnum?)Math.Round(f);
        e = (TestEnum?)Math.Round(d);
        e = (TestEnum?)Math.Round(m);
    }

    private void TestNullable(bool? b, float? f, double? d, decimal? m)
    {
        int? i = b.HasValue ? Conversions.ToInteger(b.Value) : null;
        i = f.HasValue ? (int?)Math.Round(f.Value) : null;
        i = d.HasValue ? (int?)Math.Round(d.Value) : null;
        i = m.HasValue ? (int?)Math.Round(m.Value) : null;

        uint? ui = b.HasValue ? Conversions.ToUInteger(b.Value) : null;
        ui = f.HasValue ? (uint?)Math.Round(f.Value) : null;
        ui = d.HasValue ? (uint?)Math.Round(d.Value) : null;
        ui = m.HasValue ? (uint?)Math.Round(m.Value) : null;

        short? s = b.HasValue ? Conversions.ToShort(b.Value) : null;
        s = f.HasValue ? (short?)Math.Round(f.Value) : null;
        s = d.HasValue ? (short?)Math.Round(d.Value) : null;
        s = m.HasValue ? (short?)Math.Round(m.Value) : null;

        long? l = b.HasValue ? Conversions.ToLong(b.Value) : null;
        l = f.HasValue ? (long?)Math.Round(f.Value) : null;
        l = d.HasValue ? (long?)Math.Round(d.Value) : null;
        l = m.HasValue ? (long?)Math.Round(m.Value) : null;

        byte? byt = b.HasValue ? Conversions.ToByte(b.Value) : null;
        byt = f.HasValue ? (byte?)Math.Round(f.Value) : null;
        byt = d.HasValue ? (byte?)Math.Round(d.Value) : null;
        byt = m.HasValue ? (byte?)Math.Round(m.Value) : null;

        TestEnum? e = b.HasValue ? (TestEnum?)Conversions.ToInteger(b.Value) : null;
        e = f.HasValue ? (TestEnum?)Math.Round(f.Value) : null;
        e = d.HasValue ? (TestEnum?)Math.Round(d.Value) : null;
        e = m.HasValue ? (TestEnum?)Math.Round(m.Value) : null;
    }
}
