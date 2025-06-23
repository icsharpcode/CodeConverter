
public partial interface IFoo
{
    int get_Prop(int x = 1, int y = 2, int z = 3);
    void set_Prop(int x = 1, int y = 2, int z = 3, int value = default);
}
public partial class SomeClass : IFoo
{
    internal int get_Prop2(int x = 1, int y = 2, int z = 3)
    {
        return default;
    }
    internal void set_Prop2(int x = 1, int y = 2, int z = 3, int value = default)
    {
    }

    int IFoo.get_Prop(int x = 1, int y = 2, int z = 3) => get_Prop2(x, y, z);
    void IFoo.set_Prop(int x = 1, int y = 2, int z = 3, int value = default) => set_Prop2(x, y, z, value);

    public void TestGet()
    {
        IFoo foo = this;
        int a = get_Prop2() + get_Prop2(y: 20) + get_Prop2(10) + get_Prop2(y: 20) + get_Prop2(z: 30) + get_Prop2(10) + get_Prop2();
        int b = foo.get_Prop() + foo.get_Prop(y: 20) + foo.get_Prop(10) + foo.get_Prop(y: 20) + foo.get_Prop(z: 30) + foo.get_Prop(10) + foo.get_Prop();
    }

    public void TestSet()
    {
        set_Prop2(value: 1);
        set_Prop2(y: 20, value: 1);
        set_Prop2(10, value: 1);
        set_Prop2(y: 20, value: 1);
        set_Prop2(z: 30, value: 1);
        set_Prop2(10, value: 1);
        set_Prop2(value: 1);

        IFoo foo = this;
        foo.set_Prop(value: 1);
        foo.set_Prop(y: 20, value: 1);
        foo.set_Prop(10, value: 1);
        foo.set_Prop(y: 20, value: 1);
        foo.set_Prop(z: 30, value: 1);
        foo.set_Prop(10, value: 1);
        foo.set_Prop(value: 1);
    }
}