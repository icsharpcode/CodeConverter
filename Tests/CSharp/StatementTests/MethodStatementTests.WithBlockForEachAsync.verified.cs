using System;
using System.Collections.Generic;

public partial class TestWithForEachClass
{
    private int _x;

    public static void Main()
    {
        var x = new List<TestWithForEachClass>();
        foreach (var y in x)
        {
            y._x = 1;
            Console.Write(y._x);
            y = (TestWithForEachClass)null;
        }
    }
}
1 target compilation errors:
CS1656: Cannot assign to 'y' because it is a 'foreach iteration variable'