
public partial interface IFoo
{
    int ExplicitProp { get; }
}

public partial interface IBar
{
    int ExplicitProp { get; }
}

public partial class Foo : IFoo, IBar
{

    public int ExplicitPropRenamed { get; private set; }
    int IFoo.ExplicitProp { get => ExplicitPropRenamed; }
    int IBar.ExplicitProp { get => ExplicitPropRenamed; }
}