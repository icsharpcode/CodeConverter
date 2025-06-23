using System;

public partial struct SomeStruct
{
    public int FieldA;
    public int FieldB;
}

internal static partial class Module1
{
    public static void Main()
    {
        var myArray = new SomeStruct[1];

        {
            ref var withBlock = ref myArray[0];
            withBlock.FieldA = 3;
            withBlock.FieldB = 4;
        }

        // Outputs: FieldA was changed to New FieldA value 
        Console.WriteLine($"FieldA was changed to {myArray[0].FieldA}");
        Console.WriteLine($"FieldB was changed to {myArray[0].FieldB}");
        Console.ReadLine();
    }
}