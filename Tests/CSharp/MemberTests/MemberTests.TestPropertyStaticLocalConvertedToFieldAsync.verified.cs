using System;

internal partial class StaticLocalConvertedToField
{
    private int _OtherName_sPrevPosition = 3; // Comment moves with declaration
    public int OtherName
    {
        get
        {
            Console.WriteLine(_OtherName_sPrevPosition);
            return _OtherName_sPrevPosition;
        }
    }

    private int _OtherName_sPrevPosition1 = default;
    public int get_OtherName(int x)
    {
        _OtherName_sPrevPosition1 += 1;
        return _OtherName_sPrevPosition1;
    }
}