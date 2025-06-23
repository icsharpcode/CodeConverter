using System;

internal partial class TestClass
{
    private void TestMethod(bool someCondition)
    {
        var b = default(bool);
        for (int j = 1; j <= 2; j++)
        {
            if (someCondition)
            {
                Console.WriteLine(b);
                b = true;
            }
        }
    }
}