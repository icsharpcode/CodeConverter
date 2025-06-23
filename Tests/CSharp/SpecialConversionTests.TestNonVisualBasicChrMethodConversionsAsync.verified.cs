using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestConversions
{
    public void Test()
    {
        string a;
        a = Conversions.ToString(Chr(2));
        a = Conversions.ToString(Chr(2));
        a = "\u0002";
        a = "\u0002";
        a = "\u0002";
    }

    public void TestW()
    {
        string a;
        a = Conversions.ToString(ChrW(2));
        a = Conversions.ToString(ChrW(2));
        a = "\u0002";
        a = "\u0002";
        a = "\u0002";
    }

    public char Chr(object o)
    {
        return Strings.Chr(Conversions.ToInteger(o));
    }

    public char ChrW(object o)
    {
        return Strings.ChrW(Conversions.ToInteger(o));
    }
}