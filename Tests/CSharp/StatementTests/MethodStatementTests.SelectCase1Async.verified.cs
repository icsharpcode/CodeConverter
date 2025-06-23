using System;

internal partial class TestClass
{
    private void TestMethod(int number)
    {
        switch (number)
        {
            case 0:
            case 1:
            case 2:
                {
                    Console.Write("number is 0, 1, 2");
                    break;
                }
            case 5:
                {
                    Console.Write("section 5");
                    break;
                }

            default:
                {
                    Console.Write("default section");
                    break;
                }
        }
    }
}