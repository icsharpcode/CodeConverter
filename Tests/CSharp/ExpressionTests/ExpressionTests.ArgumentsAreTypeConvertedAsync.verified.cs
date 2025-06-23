using System;
using System.Drawing;

public partial class Compound
{
    public void TypeCast(int someInt)
    {
        var col = Color.FromArgb((int)Math.Round(someInt * 255.0f), (int)Math.Round(someInt * 255.0f), (int)Math.Round(someInt * 255.0f));
        float[] arry = new float[(int)Math.Round(7d / someInt + 1)];
    }
}