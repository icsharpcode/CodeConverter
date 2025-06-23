
internal static partial class AppBuilderUseExtensions
{
    public static object Use<T>(this string app, params object[] args)
    {
        return null;
    }
}

internal partial class TestClass
{
    private void TestMethod(string str)
    {
        str.Use<object>();
    }
}