
internal partial class TestClass
{
    private int val;
    private void TestMethod(int[] values)
    {
        foreach (var currentVal in values)
        {
            val = currentVal;
            if (val == 2)
                continue;
            if (val == 3)
                break;
        }
    }
}