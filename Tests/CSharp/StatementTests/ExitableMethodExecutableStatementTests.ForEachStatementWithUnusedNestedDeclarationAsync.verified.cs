using System;

internal partial class TestClass
{
    private void TestMethod(int[] values)
    {
        int keep1 = default, keep2;
        foreach (var inline1 in values)
        {
            foreach (var currentKeep1 in values)
            {
                keep1 = currentKeep1;
                foreach (var inline2 in values)
                {
                    if (inline2 == 2)
                        continue;
                    if (inline2 == 3)
                        break;
                }
            }
            Console.WriteLine(keep1);
        }
    }
}