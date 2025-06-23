using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class Class1
{
    public void Foo()
    {
        Bar(Conversions.ToInteger(true));
        Bar(Conversions.ToInteger("4"));
        var ss = new string[2];
        string y = ss[Conversions.ToInteger("0")];
    }

    public void Bar(int x)
    {
    }
}