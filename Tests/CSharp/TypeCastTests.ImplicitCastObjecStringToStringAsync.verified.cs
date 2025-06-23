using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class Class1
{
    private void Test()
    {
        object o = "Test";
        string s = Conversions.ToString(o);
    }
}
