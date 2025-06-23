using System;

internal partial class TestClass
{
    public event EventHandler MyEvent;

    private void TestMethod(EventHandler e)
    {
        MyEvent += e;
        MyEvent += MyHandler;
    }

    private void TestMethod2(EventHandler e)
    {
        MyEvent -= e;
        MyEvent -= MyHandler;
    }

    private void MyHandler(object sender, EventArgs e)
    {
    }
}