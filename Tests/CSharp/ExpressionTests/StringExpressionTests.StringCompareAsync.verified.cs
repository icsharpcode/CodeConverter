using System;

public partial class Class1
{
    public void Foo()
    {
        string s1 = null;
        string s2 = "";
        if ((s1 ?? "") != (s2 ?? ""))
        {
            throw new Exception();
        }
        if (s1 == "something")
        {
            throw new Exception();
        }
        if ("something" == s1)
        {
            throw new Exception();
        }
        if (string.IsNullOrEmpty(s1))
        {
            // 
        }
        if (string.IsNullOrEmpty(s1))
        {
            // 
        }
    }
}