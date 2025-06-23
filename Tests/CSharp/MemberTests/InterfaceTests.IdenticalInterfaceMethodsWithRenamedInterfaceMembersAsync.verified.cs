
public partial interface IFoo
{
    int DoFooBar(ref string str, int i);
}

public partial interface IBar
{
    int DoFooBar(ref string str, int i);
}

public partial class FooBar : IFoo, IBar
{

    public int Foo(ref string str, int i)
    {
        return 4;
    }

    int IFoo.DoFooBar(ref string str, int i) => Foo(ref str, i);

    public int Bar(ref string str, int i)
    {
        return 2;
    }

    int IBar.DoFooBar(ref string str, int i) => Bar(ref str, i);

}
