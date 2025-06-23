using System.Runtime.InteropServices;

public partial interface IFoo
{
    int ExplicitFunc([Optional, DefaultParameterValue("")] ref string str2);
}

public partial class Foo : IFoo
{

    private int ExplicitFunc([Optional, DefaultParameterValue("")] ref string str)
    {
        return 5;
    }

    int IFoo.ExplicitFunc([Optional, DefaultParameterValue("")] ref string str) => ExplicitFunc(ref str);
}