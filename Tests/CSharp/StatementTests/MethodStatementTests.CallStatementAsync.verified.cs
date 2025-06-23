using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        (() => Console.Write("Hello"))();
        (() => Console.Write("Hello"))();
        TestMethod();
        TestMethod();
    }
}
1 target compilation errors:
CS0149: Method name expected