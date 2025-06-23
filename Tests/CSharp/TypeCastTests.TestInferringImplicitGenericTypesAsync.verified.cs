
using System;
using System.Linq;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class TestClass
{
    public void GenerateFromConstants()
    {
        float[] floatArr = Enumerable.Repeat(1.0f, 5).ToArray();
        double[] doubleArr = Enumerable.Repeat(2.0d, 5).ToArray();
        decimal[] decimalArr = Enumerable.Repeat(3.0m, 5).ToArray();
        bool[] boolArr = Enumerable.Repeat(true, 5).ToArray();
        int[] intArr = Enumerable.Repeat(1, 5).ToArray();
        uint[] uintArr = Enumerable.Repeat(1U, 5).ToArray();
        long[] longArr = Enumerable.Repeat(1L, 5).ToArray();
        ulong[] ulongArr = Enumerable.Repeat(1UL, 5).ToArray();
        char[] charArr = Enumerable.Repeat('a', 5).ToArray();
        string[] strArr = Enumerable.Repeat("a", 5).ToArray();
        object[] objArr = Enumerable.Repeat(new object(), 5).ToArray();
    }

    public void GenerateFromCasts()
    {
        float[] floatArr = Enumerable.Repeat(1f, 5).ToArray();
        double[] doubleArr = Enumerable.Repeat(2d, 5).ToArray();
        decimal[] decimalArr = Enumerable.Repeat(3m, 5).ToArray();
        bool[] boolArr = Enumerable.Repeat(Conversions.ToBoolean(1), 5).ToArray();
        int[] intArr = Enumerable.Repeat((int)Math.Round(1.0d), 5).ToArray();
        uint[] uintArr = Enumerable.Repeat((uint)Math.Round(1.0d), 5).ToArray();
        long[] longArr = Enumerable.Repeat((long)Math.Round(1.0d), 5).ToArray();
        ulong[] ulongArr = Enumerable.Repeat((ulong)Math.Round(1.0d), 5).ToArray();
        char[] charArr = Enumerable.Repeat('a', 5).ToArray();
        string[] strArr = Enumerable.Repeat("a", 5).ToArray();
        object[] objArr1 = Enumerable.Repeat((object)"a", 5).ToArray();
        object[] objArr2 = Enumerable.Repeat((object)"a", 5).ToArray();
    }
}