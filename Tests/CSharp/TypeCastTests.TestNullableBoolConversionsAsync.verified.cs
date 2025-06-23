using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class Class1
{
    private bool Test1(bool? a)
    {
        return (bool)a;
    }
    private bool? Test2(bool? a)
    {
        return a;
    }
    private bool? Test3(bool a)
    {
        return a;
    }

    private bool Test4(int? a)
    {
        return Conversions.ToBoolean(a.Value);
    }
    private bool? Test5(int? a)
    {
        return a.HasValue ? Conversions.ToBoolean(a.Value) : null;
    }
    private bool? Test6(int a)
    {
        return Conversions.ToBoolean(a);
    }

    private int Test4(bool? a)
    {
        return Conversions.ToInteger(a.Value);
    }
    private int? Test5(bool? a)
    {
        return a.HasValue ? Conversions.ToInteger(a.Value) : null;
    }
    private int? Test6(bool a)
    {
        return Conversions.ToInteger(a);
    }

    private string Test7(bool? a)
    {
        return Conversions.ToString(a.Value);
    }
    private string Test8(bool? a)
    {
        return Conversions.ToString(a.Value);
    }

    private bool? Test9(string a)
    {
        return Conversions.ToBoolean(a);
    }
    private bool Test10(string a)
    {
        return Conversions.ToBoolean(a);
    }
}