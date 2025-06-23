
internal partial class TestClass712
{
    private object TestMethod()
    {
        bool? var1 = default;
        bool? var2 = default;
        if (var1.GetValueOrDefault() || (!var2).GetValueOrDefault())
            return true;
        else
            return false;
    }
}