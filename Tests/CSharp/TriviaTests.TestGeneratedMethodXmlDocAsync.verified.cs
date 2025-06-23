
internal partial class TestClass
{
    /// <summary>
    /// Returns the cached ResourceManager instance used by this class.
    /// </summary>
    public void TestMethod<T, T2, T3>(out T argument, ref T2 argument2, T3 argument3)
        where T : class, new()
        where T2 : struct
    {
        argument = null;
        argument2 = default;
        argument3 = default;
    }
}