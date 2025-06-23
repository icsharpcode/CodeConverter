using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        string str = new ThreadStaticAttribute().ToString();
    }
}