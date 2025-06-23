using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        for (int i = 1; i <= 2; i++)
        {
            bool? a = default;
            Console.WriteLine(a);
        }
    }
}