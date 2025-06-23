
namespace TestNamespace
{
    public partial interface IFoo
    {
        int FooProp { get; set; }
    }
}

public partial class Foo : TestNamespace.IFoo
{

    public int FooPropRenamed { get; set; }
    int TestNamespace.IFoo.FooProp { get => FooPropRenamed; set => FooPropRenamed = value; }

}