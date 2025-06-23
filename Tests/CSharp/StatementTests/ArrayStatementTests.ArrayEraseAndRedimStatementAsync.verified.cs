using System;

public partial class TestClass
{
    public static int[] TestMethod(int[] numArray, int[] numArray2)
    {
        numArray = new int[4];
        numArray = null;
        numArray2[1] = 1;
        Array.Resize(ref numArray, 6);
        Array.Resize(ref numArray2, 6);
        var y = new int[7, 6];
        y[2, 3] = 1;
        var oldY = y;
        y = new int[7, 9];
        if (oldY is not null)
            for (var i = 0; i <= oldY.Length / oldY.GetLength(1) - 1; ++i)
                Array.Copy(oldY, i * oldY.GetLength(1), y, i * y.GetLength(1), Math.Min(oldY.GetLength(1), y.GetLength(1)));
        return numArray2;
    }
}