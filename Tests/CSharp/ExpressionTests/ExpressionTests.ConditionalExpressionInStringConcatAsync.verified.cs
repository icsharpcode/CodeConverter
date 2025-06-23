using System;

internal partial class ConditionalExpressionInStringConcat
{
    private void TestMethod(string str)
    {
        int appleCount = 42;
        Console.WriteLine("I have " + appleCount + (appleCount == 1 ? " apple" : " apples"));
    }
}