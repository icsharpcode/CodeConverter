using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        if (MyEvent is not null)
            MyEvent(this, EventArgs.Empty);
    }
}
1 source compilation errors:
BC30451: 'MyEvent' is not declared. It may be inaccessible due to its protection level.
1 target compilation errors:
CS0103: The name 'MyEvent' does not exist in the current context