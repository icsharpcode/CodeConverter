using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        int i = 1;
        var b = default(int);
        var c = default(int);
        var c1 = default(int);
        var c2 = default(int);
        do
        {
            b += 1;
            Console.WriteLine("b={0}", b);
            for (int j = 1; j <= 3; j++)
            {
                c += 1;
                Console.WriteLine("c={0}", c);
            }
            for (int j = 1; j <= 3; j++)
            {
                c1 += 1;
                Console.WriteLine("c1={0}", c1);
            }
            int k = 1;
            while (k <= 3)
            {
                c2 += 1;
                Console.WriteLine("c2={0}", c2);
                k += 1;
            }
            i += 1;
        }
        while (i <= 3);
    }
}