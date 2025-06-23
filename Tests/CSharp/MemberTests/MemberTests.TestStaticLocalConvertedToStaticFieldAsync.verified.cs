using System;

internal partial class StaticLocalConvertedToField
{
    private static int _OtherName_sPrevPosition = default;
    public static void OtherName(bool x) // Comment moves with declaration
    {
        Console.WriteLine(_OtherName_sPrevPosition);
    }
    private int _OtherName_sPrevPosition1 = 5; // Comment also moves with declaration
    public void OtherName(int x)
    {
        Console.WriteLine(_OtherName_sPrevPosition1);
    }
    private static int _StaticTestProperty_sPrevPosition = 5; // Comment also moves with declaration
    public static int StaticTestProperty
    {
        get
        {
            return _StaticTestProperty_sPrevPosition + 1;
        }
    }
}