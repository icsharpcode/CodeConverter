using System.Collections.Generic;
using System.Linq;

public partial class VisualBasicClass
{
    public void Main()
    {
        var list1 = new List<int>() { 1, 2, 3 };
        var list2 = new List<int>() { 2, 4, 5 };

        var qs = from n in list1
                 from x in list2
                 where x == n
                 select new { x, n };
    }
}