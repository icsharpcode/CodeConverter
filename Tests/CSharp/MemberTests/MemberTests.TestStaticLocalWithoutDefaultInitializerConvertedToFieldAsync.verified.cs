using System;

internal partial class StaticLocalConvertedToField
{
    private int _OtherName_sPrevPosition;
    public int OtherName()
    {
        _OtherName_sPrevPosition = 23;
        Console.WriteLine(_OtherName_sPrevPosition);
        return _OtherName_sPrevPosition;
    }
}