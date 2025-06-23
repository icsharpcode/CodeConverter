
public partial interface IFoo
{
    int this[string str] { get; }
}

public partial class Foo : IFoo
{

    public virtual int this[string str]
    {
        get
        {
            return 2;
        }
    }
}
