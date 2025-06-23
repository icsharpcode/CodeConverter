
public partial class TestClass
{
    public void M(string a)
    {
    }
    public void M(string a = "ss", string b = "smth")
    {
    }

    public void Test()
    {
        M("ss", "x");
    }
}