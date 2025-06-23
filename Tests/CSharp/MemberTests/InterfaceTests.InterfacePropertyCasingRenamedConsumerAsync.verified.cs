
public partial interface IFoo
{
    int FooProp { get; set; }
}

public partial class Foo : IFoo
{

    public int FooProp { get; set; }

}

public partial class FooConsumer
{
    public int GetFooRenamed()
    {
        var foo = new Foo();
        IFoo bar = foo;
        return foo.FooProp + bar.FooProp;
    }
}