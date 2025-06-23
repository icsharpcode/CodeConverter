
namespace TestNamespace
{
    public partial interface IFoo
    {
        int DoFoo(ref string str, int i);
    }
}

public partial class Foo : TestNamespace.IFoo
{

    public int DoFooRenamed(ref string str, int i)
    {
        return 4;
    }

    int TestNamespace.IFoo.DoFoo(ref string str, int i) => DoFooRenamed(ref str, i);
}