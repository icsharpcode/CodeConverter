using System;

internal partial class TestClass
{
    private void TestMethod(object nullObject)
    {
        if (nullObject is null)
            throw new ArgumentNullException(nameof(nullObject));

        lock (nullObject)
            Console.WriteLine(nullObject);
    }
}