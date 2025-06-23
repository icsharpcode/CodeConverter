using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class Class1
{
    private void Test()
    {
        object o = 5;
        int i = Conversions.ToInteger(o);
    }
}
