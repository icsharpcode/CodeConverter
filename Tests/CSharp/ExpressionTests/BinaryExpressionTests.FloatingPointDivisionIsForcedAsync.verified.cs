using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        double x = 10d / 3d;
        x /= 2d;
        double y = 10.0d / 3d;
        y /= 2d;
        int z = 8;
        z = (int)Math.Round(z / 3d);
    }
}