using System;

internal partial class StaticLocalConvertedToField
{
    private int _sPrevPosition = 7; // Comment moves with declaration
    public StaticLocalConvertedToField(bool x)
    {
        Console.WriteLine(_sPrevPosition);
    }

    private int _sPrevPosition1 = default;
    public StaticLocalConvertedToField(int x)
    {
        Console.WriteLine(_sPrevPosition1);
    }
}