using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal enum TestEnum
{
    None = 0
}

internal partial class Class1
{
    public void TestEnumCast(string str)
    {
        TestEnum enm = (TestEnum)Conversions.ToInteger(str);
    }
}
