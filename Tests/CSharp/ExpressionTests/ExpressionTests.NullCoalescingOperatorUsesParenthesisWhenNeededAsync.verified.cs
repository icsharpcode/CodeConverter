using System;

public partial class VisualBasicClass
{
    public void TestMethod(string x, Func<int> y)
    {
        string a = x ?? "x";
        string b = (x ?? "x").ToUpper();
        string c = $"{x ?? "x"}";
        string d = $"{(x ?? "x").ToUpper()}";
        var e = y ?? (() => 5);
        var f = y ?? (() => 6);
    }
}