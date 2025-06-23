using System.Collections.Generic;
using System.Linq;

public partial class Class1
{
    public void Foo()
    {
        var xs = new List<string>();
        var y = from x in xs
                group x by new { x.Length, Count = x.Count() };
    }
}