
internal partial class TestClass
{
    private void TestMethod()
    {
        bool? var1 = default;
        var a = var1.HasValue ? var1.Value == false : (bool?)null;
        var b = var1.HasValue ? var1.Value == true : (bool?)null;
    }
}