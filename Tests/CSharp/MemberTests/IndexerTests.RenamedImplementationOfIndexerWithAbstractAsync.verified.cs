
public partial interface IFoo
{
    int this[string str] { get; set; }
}

public abstract partial class Foo : IFoo
{

    public abstract int this[string str] { get; set; }
}

public partial class FooChild : Foo
{

    public override int this[string str]
    {
        get
        {
            return 1;
        }
        set
        {
        }
    }
}
