
internal partial class TestClass
{
    private void TestMethod(string str)
    {
        int result = 5 - (string.IsNullOrEmpty(str) ? 1 : 2);
    }
}