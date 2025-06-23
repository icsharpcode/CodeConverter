using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        long TotalRead = 1L;
        long? ContentLength = 2; // (It is supposed that TotalRead < ContentLength)
        int percentage1 = Convert.ToInt32(TotalRead / (double?)ContentLength * 100.0d);
        int percentage2 = Convert.ToInt32(TotalRead / (double?)ContentLength * 100.0d);
    }
}