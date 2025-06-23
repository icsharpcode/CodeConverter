
public partial interface IFoo
{
    int ExplicitProp { get; set; }
}

public partial interface IBar
{
    int ExplicitProp { get; set; }
}
public abstract partial class Foo : IFoo, IBar
{

    protected abstract int ExplicitPropRenamed1 { get; set; }
    int IFoo.ExplicitProp { get => ExplicitPropRenamed1; set => ExplicitPropRenamed1 = value; }
    protected abstract int ExplicitPropRenamed2 { get; set; }
    int IBar.ExplicitProp { get => ExplicitPropRenamed2; set => ExplicitPropRenamed2 = value; }
}