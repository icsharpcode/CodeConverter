using System;

internal partial class TestClass
{
    private void TestMethod(int[] values)
    {
        var val = default(int);
        foreach (var currentVal in values)
        {
            val = currentVal;
            if (val == 2)
                continue;
            if (val == 3)
                break;
        }

        Console.WriteLine(val);
    }
}