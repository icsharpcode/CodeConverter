using System.Runtime.InteropServices;

internal partial class OmittedArguments
{
    public void M([Optional, DefaultParameterValue("a")] string a, [Optional, DefaultParameterValue("b")] ref string b)
    {
        string s = "";

        string argb = "b";
        M(b: ref argb); // omitted implicitly
        string argb1 = "b";
        M(b: ref argb1); // omitted explicitly

        string argb2 = "b";
        M(s, b: ref argb2); // omitted implicitly
        string argb3 = "b";
        M(s, b: ref argb3); // omitted explicitly

        string argb4 = "b";
        M(a: s, b: ref argb4); // omitted implicitly
        string argb5 = "b";
        M(a: s, b: ref argb5); // omitted explicitly
    }
}