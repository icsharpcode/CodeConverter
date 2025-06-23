using System;
using System.Collections.Generic;

public partial class Issue495AndIssue713
{
    public int[] Empty()
    {
        IEnumerable<int> emptySingle = Array.Empty<int>();
        IEnumerable<int> initializedSingle = new[] { 1 };
        int[][] emptyNested = Array.Empty<int[]>();
        var initializedNested = new int[2][];
        int[,] empty2d = new int[,] { { } };
        int[,] initialized2d = new[,] { { 1 } };
        return Array.Empty<int>();
    }
}