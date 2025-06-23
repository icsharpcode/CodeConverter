using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class Issue655
{
    private object s1 = Strings.InStr(1, "obj", "object '");
    private object s2 = Strings.InStrRev(1.ToString(), "obj", Conversions.ToInteger("object '"));
    private object s3 = Strings.Replace(1.ToString(), "obj", "object '");
    private object s4 = Strings.Split(1.ToString(), "obj", Conversions.ToInteger("object '"));
    private object s5 = Strings.Filter(new string[] { 1.ToString(), 2.ToString() }, "obj");
    private object s6 = Strings.StrComp(1.ToString(), "obj");
    private object s7;

    public Issue655()
    {
        s7 = OtherFunction();
    }

    public bool OtherFunction(CompareMethod c = CompareMethod.Binary)
    {
        return c == CompareMethod.Binary;
    }
}