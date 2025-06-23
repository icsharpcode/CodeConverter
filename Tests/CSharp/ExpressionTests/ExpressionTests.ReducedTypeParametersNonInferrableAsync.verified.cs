using System.Linq;

public partial class Class1
{
    public void Foo()
    {
        var y = "".Split(',').Select<string, object>(x => x);
    }
}