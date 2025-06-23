using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        double x = Math.Pow(7d, 6d) % (5 / 4) + 3 * 2;
        x += 1d;
        x -= 2d;
        x *= 3d;
        x = (double)(x / 4L);
        x = Math.Pow(x, 5d);
    }
}