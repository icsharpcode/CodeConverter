using System.Collections.Generic;

internal partial class Class1
{
    private void Test()
    {
        object o = new List<int>();
        List<int> l = (List<int>)o;
    }
}