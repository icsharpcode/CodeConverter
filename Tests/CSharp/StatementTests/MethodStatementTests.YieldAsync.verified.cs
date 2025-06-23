using System.Collections.Generic;

internal partial class TestClass
{
    private IEnumerable<int> TestMethod(int number)
    {
        if (number < 0)
            yield break;
        if (number < 1)
            yield break;
        for (int i = 0, loopTo = number - 1; i <= loopTo; i++)
            yield return i;
        yield break;
    }
}