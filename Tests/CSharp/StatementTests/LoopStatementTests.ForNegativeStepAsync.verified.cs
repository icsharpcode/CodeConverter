using System.Diagnostics;

internal partial class Issue453
{
    public void PrintLoop(int startIndex, int endIndex)
    {
        for (int i = startIndex, loopTo = endIndex; i >= loopTo; i -= 5)
            Debug.WriteLine(i);
    }
}