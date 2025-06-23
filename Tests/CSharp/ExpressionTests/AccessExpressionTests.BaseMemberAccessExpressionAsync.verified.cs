
internal partial class BaseTestClass
{
    public int member;
}

internal partial class TestClass : BaseTestClass
{

    private void TestMethod()
    {
        member = 0;
    }
}