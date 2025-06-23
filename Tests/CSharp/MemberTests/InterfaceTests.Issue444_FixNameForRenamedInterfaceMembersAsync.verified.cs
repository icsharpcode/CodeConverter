
public partial interface IFoo
{
    int FooDifferentName(ref string str, int i);
}

public partial class Foo : IFoo
{

    public int BarDifferentName(ref string str, int i)
    {
        return 4;
    }

    int IFoo.FooDifferentName(ref string str, int i) => BarDifferentName(ref str, i);
}
