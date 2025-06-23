
using System.Collections.Generic;
using System.Linq;

public partial class Issue635
{
    private List<int> l;
    private object listSortedDistinct;

    public Issue635()
    {
        listSortedDistinct = (from x in l
                              orderby x
                              select x).Distinct();
    }
}