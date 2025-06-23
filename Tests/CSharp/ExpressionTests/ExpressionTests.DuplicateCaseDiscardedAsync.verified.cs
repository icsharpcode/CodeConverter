using System;

internal static partial class Module1
{
    public static void Main()
    {
        switch (1)
        {
            case 1:
                {
                    Console.WriteLine("a");
                    break;
                }

            case var @case when @case == 1:
                {
                    Console.WriteLine("b");
                    break;
                }

        }

    }
}
1 target compilation errors:
CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code