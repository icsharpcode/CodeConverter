
public partial interface IFoo
{
    int ExplicitProp { get; set; }
}

public partial interface IBar
{
    int ExplicitProp { get; set; }
}

public partial class Foo : IFoo, IBar
{

    private int ExplicitProp
    {
        get
        {
            return 5;
        }
        set
        {
        }
    }

    int IFoo.ExplicitProp { get => ExplicitProp; set => ExplicitProp = value; }
    int IBar.ExplicitProp { get => ExplicitProp; set => ExplicitProp = value; } // Comment moves because this line gets split
}