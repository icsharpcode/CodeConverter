
public partial interface IFoo
{
    int this[string str] { get; set; }
}

public partial class Foo : IFoo
{

    public virtual int this[string str]
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
