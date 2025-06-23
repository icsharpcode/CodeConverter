using System;

internal partial class TestClass
{
    public static void MultiStatement(int a)
    {
        if (a == 0)
        {
            Console.WriteLine(1);
            Console.WriteLine(2);
            return;
        }
        Console.WriteLine(3);
    }
}