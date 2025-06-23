using System;

internal partial class GotoTest1
{
    private static void Main()
    {
        int x = 200;
        int y = 4;
        int count = 0;
        string[,] array = new string[x, y];

        for (int i = 0, loopTo = x - 1; i <= loopTo; i++)
        {

            for (int j = 0, loopTo1 = y - 1; j <= loopTo1; j++)
                array[i, j] = System.Threading.Interlocked.Increment(ref count).ToString();
        }

        Console.Write("Enter the number to search for: ");
        string myNumber = Console.ReadLine();

        for (int i = 0, loopTo2 = x - 1; i <= loopTo2; i++)
        {

            for (int j = 0, loopTo3 = y - 1; j <= loopTo3; j++)
            {

                if (array[i, j].Equals(myNumber))
                {
                    goto Found;
                }
            }
        }

        Console.WriteLine("The number {0} was not found.", myNumber);
        goto Finish;
    Found:
        ;

        Console.WriteLine("The number {0} is found.", myNumber);
    Finish:
        ;

        Console.WriteLine("End of search.");
        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();
    }
}