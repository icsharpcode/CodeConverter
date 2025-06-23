using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        int x = 1;
        Action simpleAssignmentAction = () => x = 1;
        Action nonBlockAction = () => Console.WriteLine("Statement");
        Action ifAction = () => { if (true) return; };
    }
}