
public partial interface IFoo
{
    int DoFooBar(ref string str, int i = 4);
}

public partial interface IBar
{
    int DoFooBar(ref string str, int i = 8);
}

public partial class FooBar : IFoo, IBar
{

    public int Foo(ref string str, int i = 4)
    {
        return 4;
    }

    int IFoo.DoFooBar(ref string str, int i = 4) => Foo(ref str, i);

    public int Bar(ref string str, int i = 8)
    {
        return 2;
    }

    int IBar.DoFooBar(ref string str, int i = 8) => Bar(ref str, i);

}
