using System.Collections.Generic;
using System.Linq;

public partial class VisualBasicClass
{
    public static void X(List<object> objs)
    {
        int MaxObj = objs.Max(o => o.GetHashCode());
        int CountWhereObj = (from o in objs
                             where o.GetHashCode() > 3
                             select o).Count();
    }
}