
public partial interface IFoo
{
    int DoFoo();
    int Prop { get; set; }
}

public partial class Foo : IFoo
{

    private int doFoo()
    {
        return 4;
    }

    int IFoo.DoFoo() => doFoo();

    private int prop { get; set; }
    int IFoo.Prop { get => prop; set => prop = value; }

    private int Consumer()
    {
        var foo = new Foo();
        IFoo interfaceInstance = foo;
        return foo.doFoo() + foo.doFoo() + interfaceInstance.DoFoo() + interfaceInstance.DoFoo() + foo.prop + foo.prop + interfaceInstance.Prop + interfaceInstance.Prop;
    }

}
