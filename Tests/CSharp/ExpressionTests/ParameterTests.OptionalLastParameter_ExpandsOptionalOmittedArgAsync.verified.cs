
public partial class TestClass
{
    public void M(string a)
    {
    }
    public void M(string a, string b = "smth")
    {
    }

    public void Test()
    {
        M("x", "smth");
    }
}