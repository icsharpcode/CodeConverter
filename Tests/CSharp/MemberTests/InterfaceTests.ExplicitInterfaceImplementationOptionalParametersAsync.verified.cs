
public partial interface IFoo
{
    int get_ExplicitProp(string str = "");
    void set_ExplicitProp(string str = "", int value = default);
    int ExplicitFunc(string str2 = "", int i2 = 1);
}

public partial class Foo : IFoo
{

    private int ExplicitFunc(string str = "", int i2 = 1)
    {
        return 5;
    }

    int IFoo.ExplicitFunc(string str = "", int i2 = 1) => ExplicitFunc(str, i2);

    private int get_ExplicitProp(string str = "")
    {
        return 5;
    }
    private void set_ExplicitProp(string str = "", int value = default)
    {
    }

    int IFoo.get_ExplicitProp(string str = "") => get_ExplicitProp(str);
    void IFoo.set_ExplicitProp(string str = "", int value = default) => set_ExplicitProp(str, value);
}
