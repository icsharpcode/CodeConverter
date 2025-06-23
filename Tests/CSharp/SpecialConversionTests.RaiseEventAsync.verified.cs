using System;

internal partial class TestClass
{
    private event EventHandler MyEvent;

    private void TestMethod()
    {
        MyEvent?.Invoke(this, EventArgs.Empty);
    }
}