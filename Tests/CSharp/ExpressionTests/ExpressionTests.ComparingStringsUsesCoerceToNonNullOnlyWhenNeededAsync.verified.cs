
internal partial class TestClass
{
    private void TestMethod(string a)
    {
        bool result = string.IsNullOrEmpty(a);
        result = string.IsNullOrEmpty(a);
        result = string.IsNullOrEmpty(a);
        result = string.IsNullOrEmpty(a);
        result = string.IsNullOrEmpty(a);
        result = string.IsNullOrEmpty(a);
        result = a is null;
        result = a is not null;
        result = a is null;
        result = (a ?? "") == (a ?? "");
        result = a == "test";
        result = "test" == a;
    }
}