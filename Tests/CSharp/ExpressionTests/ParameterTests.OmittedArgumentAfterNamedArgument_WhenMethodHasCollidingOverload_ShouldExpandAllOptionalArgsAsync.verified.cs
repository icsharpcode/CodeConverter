
public partial class TestClass
{
    public void M(string a, string b)
    {
    }
    public void M(string a = "1", string b = "2", string c = "3")
    {
    }

    public void Test()
    {
        M(a: "4", "2", c: "3");
    }
}