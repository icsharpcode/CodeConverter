
internal partial class TestClassBase
{
    public void DoStuff()
    {
    }
}
internal partial class TestClass : TestClassBase
{
    private void TestMethod()
    {
        DoStuff();
    }
}