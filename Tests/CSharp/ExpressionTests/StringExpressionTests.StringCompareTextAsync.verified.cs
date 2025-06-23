using System;
using System.Globalization;

public partial class Class1
{
    public void Foo()
    {
        string s1 = null;
        string s2 = "";
        if (CultureInfo.CurrentCulture.CompareInfo.Compare(s1 ?? "", s2 ?? "", CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) != 0)
        {
            throw new Exception();
        }
        if (CultureInfo.CurrentCulture.CompareInfo.Compare(s1, "something", CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) == 0)
        {
            throw new Exception();
        }
        if (CultureInfo.CurrentCulture.CompareInfo.Compare("something", s1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) == 0)
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