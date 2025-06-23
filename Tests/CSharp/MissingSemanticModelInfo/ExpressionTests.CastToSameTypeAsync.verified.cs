using System;

public partial class CastToSameTypeTest
{

    public void PositionEnumFromString(char c)
    {
        switch (c)
        {
            case '.':
                {
                    Console.WriteLine(1);
                    break;
                }

            case ',':
                {
                    Console.WriteLine(2);
                    break;
                }
        }
    }
}