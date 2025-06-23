using System;

internal partial class TestClass
{
    public bool SelfDivisionPossible(int x)
    {
        try
        {
            int i = (int)Math.Round(x / (double)x);
        }
        catch
        {
        }

        return i != 0;
    }
}
1 target compilation errors:
CS0103: The name 'i' does not exist in the current context