using System.Collections.Generic;
using System.Linq;

public partial class C
{
    private static void LinqWithNullable()
    {
        var a = new List<int?>() { 1, 2, 3, default };
        var result = from x in a
                     where x == 1
                     select x;
    }
}