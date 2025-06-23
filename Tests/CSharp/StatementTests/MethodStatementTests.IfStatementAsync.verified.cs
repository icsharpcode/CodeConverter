
internal partial class TestClass
{
    private void TestMethod(int a)
    {
        int b;

        if (a == 0)
        {
            b = 0;
        }
        else if (a == 1)
        {
            b = 1;
        }
        else if (a == 2 || a == 3)
        {
            b = 2;
        }
        else
        {
            b = 3;
        }
    }
}