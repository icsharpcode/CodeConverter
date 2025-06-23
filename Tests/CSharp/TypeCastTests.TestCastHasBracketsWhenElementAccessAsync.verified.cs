using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestCastHasBracketsWhenElementAccess
{
    private int Casting(object sender)
    {
        return Conversions.ToInteger(((object[])sender)[0]);
    }
}