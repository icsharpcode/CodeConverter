using System;

internal static partial class StaticLocalConvertedToField
{
    private static int _OtherName_sPrevPosition = 3; // Comment moves with declaration
    public static void OtherName(bool x)
    {
        Console.WriteLine(_OtherName_sPrevPosition);
    }

    private static int _OtherName_sPrevPosition1 = default;
    public static int OtherName(int x) // Comment also moves with declaration
    {
        return _OtherName_sPrevPosition1;
    }
}