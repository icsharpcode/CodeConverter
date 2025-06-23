using System;

internal partial class TestClass
{
    private void TestMethod(string str)
    {
        Console.WriteLine(str ?? "<null>");
    }
}