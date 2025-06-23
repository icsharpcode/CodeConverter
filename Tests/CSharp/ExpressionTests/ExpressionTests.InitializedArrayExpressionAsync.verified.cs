using System;
using System.Collections.Generic;

public partial class Issue713
{
    public int[] Empty()
    {
        IEnumerable<int> initializedSingle = new[] { 1 };
        int[,] initialized2d = new[,] { { 1 } };
        return Array.Empty<int>();
    }
}