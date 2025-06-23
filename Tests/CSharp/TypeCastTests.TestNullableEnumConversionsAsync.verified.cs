using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal enum TestEnum
{
    None = 1
}

internal partial class Class1
{
    private TestEnum? Test1(int a)
    {
        return (TestEnum?)a;
    }
    private TestEnum? Test2(int? a)
    {
        return (TestEnum?)a;
    }
    private TestEnum Test3(int? a)
    {
        return (TestEnum)a;
    }

    private int? Test4(TestEnum a)
    {
        return Conversions.ToInteger(a);
    }
    private int? Test5(TestEnum? a)
    {
        return (int?)a;
    }
    private TestEnum? Test6(TestEnum? a)
    {
        return a;
    }
    private int Test7(TestEnum? a)
    {
        return (int)a;
    }

    private string Test8(TestEnum? a)
    {
        return Conversions.ToString(a.Value);
    }
    private string Test9(TestEnum? a)
    {
        return Conversions.ToString(a.Value);
    }

    private TestEnum? Test10(string a)
    {
        return (TestEnum?)Conversions.ToInteger(a);
    }
    private TestEnum Test11(string a)
    {
        return (TestEnum)Conversions.ToInteger(a);
    }
}