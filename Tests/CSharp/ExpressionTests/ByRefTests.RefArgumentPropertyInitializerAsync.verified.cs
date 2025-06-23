
public partial class Class1
{
    static Class1 Foo__p1()
    {
        var argc1 = new Class1();
        return Foo(ref argc1);
    }

    private Class1 _p1 = Foo__p1();
    public static Class1 Foo(ref Class1 c1)
    {
        return c1;
    }
}