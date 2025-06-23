
internal partial class TestClass
{
    private void TestMethod(int end)
    {
        int[] b = default, s = default;
        for (int i = 0, loopTo = end; i <= loopTo; i++)
            b[i] = s[i];
    }
}
1 source compilation errors:
BC30183: Keyword is not valid as an identifier.