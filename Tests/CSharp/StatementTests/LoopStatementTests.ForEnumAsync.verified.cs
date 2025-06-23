using System.Diagnostics;

internal enum MyEnum
{
    Zero,
    One
}

internal partial class ForEnumAsync
{
    public void PrintLoop(MyEnum startIndex, MyEnum endIndex, MyEnum step)
    {
        for (MyEnum i = startIndex, loopTo = endIndex; (int)step >= 0 ? i <= loopTo : i >= loopTo; i += (int)step)
            Debug.WriteLine(i);
        for (MyEnum i2 = startIndex, loopTo1 = endIndex; (int)step >= 0 ? i2 <= loopTo1 : i2 >= loopTo1; i2 += (int)step)
            Debug.WriteLine(i2);
        for (MyEnum i3 = startIndex, loopTo2 = endIndex; i3 <= loopTo2; i3 += 3)
            Debug.WriteLine(i3);
        for (MyEnum i4 = startIndex; i4 <= (MyEnum)4; i4++)
            Debug.WriteLine(i4);
    }
}