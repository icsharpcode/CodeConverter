using System;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

public partial class Issue479
{
    public int this[string s]
    {
        get
        {
            return 32768 + Strings.AscW(s);
        }
    }
}

public partial class TestIssue479
{
    public void compareAccess()
    {
        var hD = new Issue479();
        Console.WriteLine("Traditional access returns " + hD["X"] + Constants.vbCrLf + "Default property access returns " + hD["X"] + Constants.vbCrLf + "Dictionary access returns " + hD["X"]);
    }
}