
public partial interface IFoo
{
    int get_ExplicitProp(string str);
    void set_ExplicitProp(string str, int value);
    int ExplicitFunc(ref string str2, int i2);
}

public partial interface IBar
{
    int get_ExplicitProp(string str);
    void set_ExplicitProp(string str, int value);
    int ExplicitFunc(ref string str2, int i2);
}

public partial class Foo : IFoo, IBar
{

    private int ExplicitFunc(ref string str, int i)
    {
        return 5;
    }

    int IFoo.ExplicitFunc(ref string str, int i) => ExplicitFunc(ref str, i);
    int IBar.ExplicitFunc(ref string str, int i) => ExplicitFunc(ref str, i);

    private int get_ExplicitProp(string str)
    {
        return 5;
    }
    private void set_ExplicitProp(string str, int value)
    {
    }

    int IFoo.get_ExplicitProp(string str) => get_ExplicitProp(str);
    int IBar.get_ExplicitProp(string str) => get_ExplicitProp(str);
    void IFoo.set_ExplicitProp(string str, int value) => set_ExplicitProp(str, value);
    void IBar.set_ExplicitProp(string str, int value) => set_ExplicitProp(str, value);
}