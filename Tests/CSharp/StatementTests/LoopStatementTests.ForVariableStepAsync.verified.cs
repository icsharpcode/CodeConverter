using System.Diagnostics;

internal partial class Issue453
{
    public void PrintLoop(int startIndex, int endIndex, int step)
    {
        for (int i = startIndex, loopTo = endIndex; step >= 0 ? i <= loopTo : i >= loopTo; i += step)
            Debug.WriteLine(i);
    }
}