using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    public enum TestEnum : int
    {
        Test1
    }

    private object EnumVariable = TestEnum.Test1;
    public void AMethod()
    {
        int t1 = Conversions.ToInteger(EnumVariable);
    }
}