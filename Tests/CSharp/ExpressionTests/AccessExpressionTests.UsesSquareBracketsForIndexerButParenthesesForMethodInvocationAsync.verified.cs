
internal partial class TestClass
{
    private string[] TestMethod()
    {
        string s = "1,2";
        return s.Split(s[1]);
    }
}