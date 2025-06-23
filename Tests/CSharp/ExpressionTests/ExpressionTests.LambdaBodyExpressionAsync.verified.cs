using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        Func<int, int> test = a => a * 2;
        Func<int, int, double> test2 = (a, b) =>
        {
            if (b > 0)
                return a / (double)b;
            return 0d;
        };

        Func<int, int, int> test3 = (a, b) => a % b;
        test(3);
    }
}