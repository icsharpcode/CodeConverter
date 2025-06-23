using System;

internal partial class TestClass
{
    public void TestMethod()
    {
    }

    public void TestMethod(int i)
    {
    }
}

internal partial class TestSubclass : TestClass
{

    public new void TestMethod()
    {
        // Not possible: TestMethod(3)
        Console.WriteLine("New implementation");
    }
}