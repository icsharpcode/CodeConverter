using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class Class1
{
    private int? Test1(int a)
    {
        return a;
    }
    private int? Test2(int? a)
    {
        return a;
    }
    private int Test3(int? a)
    {
        return (int)a;
    }
    private int? Test4(float a)
    {
        return (int?)Math.Round(a);
    }
    private int? Test5(float? a)
    {
        return a.HasValue ? (int?)Math.Round(a.Value) : null;
    }

    private float? Test6(float a)
    {
        return a;
    }
    private float? Test7(float? a)
    {
        return a;
    }
    private float Test8(float? a)
    {
        return (float)a;
    }
    private float? Test9(int a)
    {
        return a;
    }
    private float? Test10(int? a)
    {
        return a;
    }

    private string Test11(int? a)
    {
        return Conversions.ToString(a.Value);
    }
    private string Test12(int? a)
    {
        return Conversions.ToString(a.Value);
    }

    private int? Test13(string a)
    {
        return Conversions.ToInteger(a);
    }
    private int Test14(string a)
    {
        return Conversions.ToInteger(a);
    }
}