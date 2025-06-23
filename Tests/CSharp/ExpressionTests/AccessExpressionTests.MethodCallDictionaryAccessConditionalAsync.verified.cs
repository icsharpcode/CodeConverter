using System.Collections.Generic;

public partial class A
{
    public void Test()
    {
        var dict = new Dictionary<string, string>() { { "a", "AAA" }, { "b", "bbb" } };
        string v = dict?["a"];
    }
}