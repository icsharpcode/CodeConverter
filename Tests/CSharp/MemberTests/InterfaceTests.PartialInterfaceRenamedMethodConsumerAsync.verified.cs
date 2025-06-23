
public partial interface IFoo
{
    int DoFoo(ref string str, int i);
}

public partial class Foo : IFoo
{

    public int DoFooRenamed(ref string str, int i)
    {
        return 4;
    }

    int IFoo.DoFoo(ref string str, int i) => DoFooRenamed(ref str, i);
}

public partial class FooConsumer
{
    public int DoFooRenamedConsumer(ref string str, int i)
    {
        var foo = new Foo();
        IFoo bar = foo;
        return foo.DoFooRenamed(ref str, i) + bar.DoFoo(ref str, i);
    }
}