
public partial interface IFoo
{
    int DoFoo(string str, int i);
}

public partial class Foo : IFoo
{

    public int DoFoo(string str, int i)
    {
        return 4;
    }
}

public partial class FooConsumer
{
    public int DoFooRenamedConsumer(string str, int i)
    {
        var foo = new Foo();
        IFoo bar = foo;
        return foo.DoFoo(str, i) + bar.DoFoo(str, i);
    }
}