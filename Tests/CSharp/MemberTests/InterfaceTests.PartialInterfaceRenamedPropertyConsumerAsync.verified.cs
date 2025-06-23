
public partial interface IFoo
{
    int FooProp { get; set; }
}

public partial class Foo : IFoo
{

    public int FooPropRenamed { get; set; }
    int IFoo.FooProp { get => FooPropRenamed; set => FooPropRenamed = value; }

}

public partial class FooConsumer
{
    public int GetFooRenamed()
    {
        var foo = new Foo();
        IFoo bar = foo;
        return foo.FooPropRenamed + bar.FooProp;
    }
}