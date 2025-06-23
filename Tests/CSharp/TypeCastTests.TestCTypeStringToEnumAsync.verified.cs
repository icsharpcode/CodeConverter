using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public enum TestEnum
{
    A,
    B
}

public partial class VisualBasicClass
{
    public void Test(string s)
    {
        bool x = (TestEnum)Conversions.ToInteger(s) == TestEnum.A;
        string y = TestCast((TestEnum)Conversions.ToInteger(s));
    }

    public string TestCast(Enum s)
    {
        return s.ToString();
    }
}