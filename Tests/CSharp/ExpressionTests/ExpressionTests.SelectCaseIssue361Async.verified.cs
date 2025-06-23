using System;

internal static partial class Module1
{
    public enum E
    {
        A = 1
    }

    public static void Main()
    {
        int x = 1;
        switch (x)
        {
            case (int)E.A:
                {
                    Console.WriteLine("z");
                    break;
                }
        }
    }
}