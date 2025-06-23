using System;

internal partial class TestClass
{
    private void TestMethod(string str)
    {
        int length;
        length = str.Length;
        Console.WriteLine("Test" + length);
        Console.ReadKey();
    }
}