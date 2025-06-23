
public partial interface IFoo
{
    int get_Prop(int x = 1, int y = 2);
    void set_Prop(int x = 1, int y = 2, int value = default);
}
public partial class SomeClass : IFoo
{
    internal int get_Prop2(int x = 1, int y = 2)
    {
        return default;
    }
    internal void set_Prop2(int x = 1, int y = 2, int value = default)
    {
    }

    int IFoo.get_Prop(int x = 1, int y = 2) => get_Prop2(x, y);
    void IFoo.set_Prop(int x = 1, int y = 2, int value = default) => set_Prop2(x, y, value);

    public void TestGet()
    {
        IFoo foo = this;
        int a = get_Prop2() + get_Prop2(y: 20) + get_Prop2(x: 10) + get_Prop2(y: -2, x: -1) + get_Prop2(x: -1, y: -2);
        int b = foo.get_Prop() + foo.get_Prop(y: 20) + foo.get_Prop(x: 10) + foo.get_Prop(y: -2, x: -1) + foo.get_Prop(x: -1, y: -2);
    }

    public void TestSet()
    {
        set_Prop2(value: 1);
        set_Prop2(-1, -2, 1);
        set_Prop2(-1, value: 1);
        set_Prop2(y: 20, value: 1);
        set_Prop2(x: 10, value: 1);
        set_Prop2(y: -2, x: -1, value: 1);
        set_Prop2(x: -1, y: -2, value: 1);

        IFoo foo = this;
        foo.set_Prop(value: 1);
        foo.set_Prop(-1, -2, 1);
        foo.set_Prop(-1, value: 1);
        foo.set_Prop(y: 20, value: 1);
        foo.set_Prop(x: 10, value: 1);
        foo.set_Prop(y: -2, x: -1, value: 1);
        foo.set_Prop(x: -1, y: -2, value: 1);
    }
}