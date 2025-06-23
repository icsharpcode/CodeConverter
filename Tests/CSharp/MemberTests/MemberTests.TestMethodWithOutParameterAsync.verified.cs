using System.Collections.Generic;

internal partial class TestClass
{
    public bool TryGet(out List<string> strs)
    {
        strs = new List<string>();
        return false;
    }
}