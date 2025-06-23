using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class Issue567
{
    public void DoSomething(ref string str)
    {
        Other.lst = new List<string>(new[] { 4.ToString(), 5.ToString(), 6.ToString() });
        Other.lst2 = new List<object>(new[] { 4.ToString(), 5.ToString(), 6.ToString() });
        str = 999.ToString();
    }

    public void Main()
    {
        var tmp = Other.lst;
        string argstr = tmp[1];
        DoSomething(ref argstr);
        tmp[1] = argstr;
        Debug.Assert((Other.lst[1] ?? "") == (4.ToString() ?? ""));
        var tmp1 = Other.lst2;
        string argstr1 = Conversions.ToString(tmp1[1]);
        DoSomething(ref argstr1);
        tmp1[1] = argstr1;
        Debug.Assert(Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(Other.lst2[1], 5.ToString(), false)));
    }

}

internal static partial class Other
{
    public static List<string> lst = new List<string>(new[] { 1.ToString(), 2.ToString(), 3.ToString() });
    public static List<object> lst2 = new List<object>(new[] { 1.ToString(), 2.ToString(), 3.ToString() });
}