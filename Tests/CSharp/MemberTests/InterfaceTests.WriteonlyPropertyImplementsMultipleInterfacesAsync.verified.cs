
public partial interface IFoo
{
    int ExplicitProp { set; }
}

public partial interface IBar
{
    int ExplicitProp { set; }
}

public partial class Foo : IFoo, IBar
{

    public int ExplicitPropRenamed
    {
        set
        {
        }
    }

    int IFoo.ExplicitProp { set => ExplicitPropRenamed = value; }
    int IBar.ExplicitProp { set => ExplicitPropRenamed = value; } // Comment moves because this line gets split
}