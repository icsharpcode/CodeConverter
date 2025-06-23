using System.Linq;

public partial class Class1
{
    public void Foo()
    {
        var y = "".Split(',').Select(x => x);
        string z = y.ElementAtOrDefault(0);
    }
}