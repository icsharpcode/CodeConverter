
public partial interface IFoo
{
    int this[string str] { set; }
}

public partial class Foo : IFoo
{

    public virtual int this[string str]
    {
        set
        {
        }
    }
}
