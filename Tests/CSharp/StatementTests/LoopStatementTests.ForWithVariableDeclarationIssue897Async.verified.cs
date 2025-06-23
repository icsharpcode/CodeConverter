using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        var b = default(bool);
        for (int i = 1; i <= 2; i++)
        {
            Console.WriteLine(b);
            b = true;
        }
    }
}