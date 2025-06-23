using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal enum MyEnum
{
    AMember
}

internal partial class TestClass
{
    private void TestMethod(string v)
    {
        MyEnum b = (MyEnum)Conversions.ToInteger(Enum.Parse(typeof(MyEnum), v));
        b = (MyEnum)Conversions.ToInteger(Enum.Parse(typeof(MyEnum), v));
    }
}