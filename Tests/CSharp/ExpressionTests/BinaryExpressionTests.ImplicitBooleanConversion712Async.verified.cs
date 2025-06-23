
internal partial class TestClass712
{
    private object TestMethod()
    {
        bool? var1 = default;
        bool? var2 = default;
        return var1.GetValueOrDefault() ? true : !var2 is not { } arg1 ? null : arg1 ? true : var1;
    }
}