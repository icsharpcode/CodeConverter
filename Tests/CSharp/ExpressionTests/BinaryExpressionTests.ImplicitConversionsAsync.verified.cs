
internal partial class TestClass
{
    private void TestMethod()
    {
        double x = 1d;
        decimal y = 2m;
        int i1 = 1;
        int i2 = 2;
        double d1 = i1 / (double)i2;
        double z = x + (double)y;
        double z2 = (double)y + x;
    }
}
