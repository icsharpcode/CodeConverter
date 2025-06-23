using System;
using System.Linq;

public partial class Class717
{
    public void Main()
    {
        var arr = new int[2];
        arr[0] = 0;
        arr[1] = 1;

        var r = from e in arr
                let p = $"value: {e}"
                let l = p.Substring(1)
                select l;

        foreach (var m in r)
            Console.WriteLine(m);
    }
}