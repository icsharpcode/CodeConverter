using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

internal partial class TestConversions
{
    public void Test(byte b)
    {
        char x = Strings.Chr(b);
        char y = Strings.ChrW(b);
    }
}