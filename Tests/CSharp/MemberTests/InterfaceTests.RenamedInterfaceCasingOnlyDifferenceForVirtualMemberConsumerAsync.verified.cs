
public partial interface IFoo
{
    int DoFoo();
    int Prop { get; set; }
}

public abstract partial class BaseFoo : IFoo
{

    protected internal virtual int doFoo()
    {
        return 4;
    }

    int IFoo.DoFoo() => doFoo();

    protected internal virtual int prop { get; set; }
    int IFoo.Prop { get => prop; set => prop = value; }

}

public partial class Foo : BaseFoo
{

    protected internal override int doFoo()
    {
        return 5;
    }

    protected internal override int prop { get; set; }

    private int Consumer()
    {
        var foo = new Foo();
        IFoo interfaceInstance = foo;
        BaseFoo baseClass = foo;
        return foo.doFoo() + foo.doFoo() + interfaceInstance.DoFoo() + interfaceInstance.DoFoo() + baseClass.doFoo() + baseClass.doFoo() + foo.prop + foo.prop + interfaceInstance.Prop + interfaceInstance.Prop + baseClass.prop + baseClass.prop;
    }
}
