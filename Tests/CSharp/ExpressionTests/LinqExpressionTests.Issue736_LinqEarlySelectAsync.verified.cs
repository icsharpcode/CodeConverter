
using System.Collections.Generic;
using System.Linq;

public partial class Issue635
{
    private object foo;
    private List<Issue635> l;
    private object listSelectWhere;

    public Issue635()
    {
        listSelectWhere = from t in
                              from t in l
                              select t.foo
                          where 1 == 2
                          select t;
    }
}