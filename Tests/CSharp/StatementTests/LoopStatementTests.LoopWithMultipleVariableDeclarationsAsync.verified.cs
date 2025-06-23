using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        int b = default;
        int c = default;
        long e = default;
        for (int i = 1; i <= 2; i++)
        {
            int a;
            int d;
            long f;
            void g() => Console.WriteLine(1);
            a = 1;
            b += 1;

            c += 1;
            d = 1;

            e += 1L;
            f = 1L;

            g();
        }
    }
}