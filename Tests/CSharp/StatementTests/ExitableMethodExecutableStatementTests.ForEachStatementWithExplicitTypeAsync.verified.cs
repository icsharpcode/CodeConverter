
internal partial class TestClass
{
    private void TestMethod(int[] values)
    {
        foreach (int v in values)
        {
            if (v == 2)
                continue;
            if (v == 3)
                break;
        }
    }
}