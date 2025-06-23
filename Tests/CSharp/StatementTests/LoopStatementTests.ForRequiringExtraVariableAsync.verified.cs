using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        string stringValue = "42";
        for (int i = 1, loopTo = 10 - stringValue.Length; i <= loopTo; i++)
        {
            stringValue = stringValue + " " + i.ToString();
            Console.WriteLine(stringValue);
        }
    }
}