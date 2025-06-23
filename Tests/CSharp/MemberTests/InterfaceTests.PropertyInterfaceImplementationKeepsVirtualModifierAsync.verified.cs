
public partial interface IFoo
{
    int get_PropParams(string str);
    void set_PropParams(string str, int value);
    int Prop { get; set; }
}

public partial class Foo : IFoo
{

    public virtual int get_PropParams(string str)
    {
        return 5;
    }
    public virtual void set_PropParams(string str, int value)
    {
    }

    public virtual int Prop
    {
        get
        {
            return 5;
        }
        set
        {
        }
    }
}
