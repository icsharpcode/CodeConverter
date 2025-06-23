using System;

internal partial class StaticLocalConvertedToField
{
    private int _OtherName_sPrevPosition = 3; // Comment moves with declaration
    public void OtherName(bool x)
    {
        Console.WriteLine(_OtherName_sPrevPosition);
    }

    private int _OtherName_sPrevPosition1 = default;
    public int OtherName(int x)
    {
        return _OtherName_sPrevPosition1;
    }
}