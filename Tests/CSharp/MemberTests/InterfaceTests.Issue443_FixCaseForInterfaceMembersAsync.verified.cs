
public partial interface IFoo
{
    int FooDifferentCase(out string str2);
}

public partial class Foo : IFoo
{
    public int FooDifferentCase(out string str2)
    {
        str2 = 2.ToString();
        return 3;
    }
}
