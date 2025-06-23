
public partial interface IFoo
{
    int FooBarProp { get; set; }
}

public partial interface IBar
{
    int FooBarProp { get; set; }
}

public partial class FooBar : IFoo, IBar
{

    public int Foo { get; set; }
    int IFoo.FooBarProp { get => Foo; set => Foo = value; }

    public int Bar { get; set; }
    int IBar.FooBarProp { get => Bar; set => Bar = value; }

}